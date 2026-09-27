#!/usr/bin/env python3
"""Builds a product's changed services from its deploy.yml and writes a Wheelhouse release bundle.

A service carries the product version in which it last changed. A release (a vX.Y.Z tag) rebuilds only the services
whose paths changed since the base release and tags them X.Y.Z; a candidate (any other commit) tags them
sha-<commit> and shows them as <base version>+<commit>. Unchanged services keep the base release's image and version.
Requires Python 3.9+, Git and, for `build`, Docker with Buildx; the descriptor parser needs PyYAML.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import tarfile
import tempfile

DESCRIPTOR = "engineering/deployment/deploy.yml"
ROOT = "engineering/"
SLUG = re.compile(r"[a-z][a-z0-9-]{0,47}")
TAG = re.compile(r"v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?")
SHA = re.compile(r"[a-f0-9]{40}")
REPOSITORY = re.compile(r"[a-z0-9][a-z0-9._/:-]*[a-z0-9]")
MEMORY = re.compile(r"[1-9][0-9]*[kmg]")
DURATION = re.compile(r"[1-9][0-9]*[smh]")
CAPABILITY = re.compile(r"[A-Z][A-Z_]{1,31}")
SITE_PATH = re.compile(r"/(?:[A-Za-z0-9._~-]+/)*(?:[A-Za-z0-9._~-]+)?")
PLATFORMS = ("linux/amd64", "linux/arm64")
NEEDS = ("postgres", "valkey", "broker")
EXPOSURES = ("public", "private")
SERVICE_KEYS = {"image", "build", "paths", "port", "health", "memory", "stopGrace", "settings", "volumes", "sites",
                "needs", "capabilities"}


class Invalid(ValueError):
    """The descriptor or an argument breaks a rule; the message names the rule."""


def check(condition, message):
    if not condition:
        raise Invalid(message)


# ------------------------------------------------------------------ descriptor

def parse(text):
    try:
        import yaml
    except ImportError:
        raise Invalid("Reading deploy.yml needs PyYAML (pip install pyyaml)") from None
    try:
        return yaml.safe_load(text)
    except yaml.YAMLError:
        raise Invalid("deploy.yml is not valid YAML") from None


def relative(value, what):
    check(isinstance(value, str) and value and not value.startswith("/") and ".." not in PurePosixPath(value).parts,
          what + " must be a path relative to engineering/")
    return value


def validate(descriptor):
    """Checks a parsed descriptor and returns it with every default filled in."""
    check(isinstance(descriptor, dict), "deploy.yml must be a mapping")
    unknown = set(descriptor) - {"descriptor", "product", "platform", "shared", "services"}
    check(not unknown, "Unknown top-level key: " + ", ".join(sorted(map(str, unknown))))
    check(descriptor.get("descriptor") == 1, "descriptor must be 1")
    check(isinstance(descriptor.get("product"), str) and SLUG.fullmatch(descriptor["product"]),
          "product must be a lowercase slug")
    platform = descriptor.get("platform", "linux/amd64")
    check(platform in PLATFORMS, "platform must be linux/amd64 or linux/arm64")
    shared = descriptor.get("shared") or []
    check(isinstance(shared, list), "shared must be a list of paths")
    services = descriptor.get("services")
    check(isinstance(services, dict) and services, "services must name at least one service")
    result = {"descriptor": 1, "product": descriptor["product"], "platform": platform,
              "shared": [relative(path, "shared path") for path in shared], "services": {}}
    claimed = set()
    for name, service in services.items():
        check(isinstance(name, str) and SLUG.fullmatch(name), "Service names must be lowercase slugs")
        check(isinstance(service, dict), name + ": a service must be a mapping")
        unknown = set(service) - SERVICE_KEYS
        check(not unknown, name + ": unknown key " + ", ".join(sorted(map(str, unknown))))
        result["services"][name] = service_of(name, service, claimed)
    return result


def service_of(name, service, claimed):
    build = service.get("build")
    check(isinstance(build, dict) and set(build) <= {"dockerfile", "context", "target", "args", "contexts"},
          name + ": build takes dockerfile, context, target, args and contexts")
    args, contexts = build.get("args") or {}, build.get("contexts") or {}
    check(isinstance(args, dict) and all(isinstance(key, str) and isinstance(value, (str, int)) for key, value in args.items()),
          name + ": build args must map names to values")
    check(isinstance(contexts, dict) and all(isinstance(key, str) for key in contexts),
          name + ": build contexts must map names to paths")
    check(build.get("target") is None or isinstance(build["target"], str), name + ": build target must be a stage name")
    paths = service.get("paths")
    check(isinstance(paths, list) and paths, name + ": paths must list what changes the service")
    port = service.get("port", 8080)
    check(type(port) is int and 1 <= port <= 65535, name + ": port must be 1-65535")
    health = service.get("health")
    if isinstance(health, dict):
        command = health.get("command")
        check(set(health) == {"command"} and isinstance(command, list) and command
              and all(isinstance(part, str) for part in command), name + ": health command must be a list")
        test = ["CMD", *command]
    else:
        check(isinstance(health, str) and SITE_PATH.fullmatch(health), name + ": health must be an HTTP path or a command")
        test = ["CMD", "curl", "--fail", "--silent", "http://localhost:" + str(port) + health]
    memory, grace = str(service.get("memory", "512m")), str(service.get("stopGrace", "30s"))
    check(MEMORY.fullmatch(memory), name + ": memory must look like 512m or 1g")
    check(DURATION.fullmatch(grace), name + ": stopGrace must look like 30s")
    settings = service.get("settings")
    if settings is not None:
        check(isinstance(settings, dict) and set(settings) <= {"file", "required"}, name + ": settings takes file and required")
        file = settings.get("file", "/app/appsettings.Local.json")
        required = settings.get("required") or []
        check(isinstance(file, str) and file.startswith("/"), name + ": settings file must be an absolute path")
        check(isinstance(required, list) and all(isinstance(key, str) and key for key in required),
              name + ": settings required must list keys")
        settings = {"file": file, "required": required}
    volumes = service.get("volumes") or {}
    check(isinstance(volumes, dict) and all(isinstance(key, str) and SLUG.fullmatch(key) and isinstance(path, str)
                                            and path.startswith("/") for key, path in volumes.items()),
          name + ": volumes must map slugs to absolute paths")
    sites = {}
    for site, options in (service.get("sites") or {}).items():
        options = options or {}
        check(isinstance(site, str) and SLUG.fullmatch(site) and isinstance(options, dict)
              and set(options) <= {"port", "path", "exposure"}, name + ": sites map slugs to port, path and exposure")
        entry = {"port": options.get("port", port), "path": options.get("path", "/"),
                 "exposure": options.get("exposure", "public")}
        check(type(entry["port"]) is int and 1 <= entry["port"] <= 65535, name + ": site port must be 1-65535")
        check(isinstance(entry["path"], str) and SITE_PATH.fullmatch(entry["path"]), name + ": site path must start with /")
        check(entry["exposure"] in EXPOSURES, name + ": site exposure must be public or private")
        check((site, entry["path"]) not in claimed, name + ": another service already serves site " + site + " at "
              + entry["path"])
        claimed.add((site, entry["path"]))
        sites[site] = entry
    needs = service.get("needs") or []
    check(isinstance(needs, list) and set(needs) <= set(NEEDS), name + ": needs takes postgres, valkey and broker")
    capabilities = service.get("capabilities") or []
    check(isinstance(capabilities, list) and all(isinstance(item, str) and CAPABILITY.fullmatch(item) for item in capabilities),
          name + ": capabilities must be Linux capability names")
    image = service.get("image")
    # A registry port may carry a colon; the last path segment may not, or it would be a tag.
    check(image is None or (isinstance(image, str) and REPOSITORY.fullmatch(image) and "@" not in image
                            and ":" not in image.rsplit("/", 1)[-1]), name + ": image must be a repository without a tag")
    return {"image": image,
            "build": {"dockerfile": relative(build.get("dockerfile"), name + ": build dockerfile"),
                      "context": relative(build.get("context", "codebase"), name + ": build context"),
                      "target": build.get("target"), "args": {key: str(value) for key, value in args.items()},
                      "contexts": {key: relative(path, name + ": build context " + key) for key, path in contexts.items()}},
            "paths": [relative(path, name + ": path") for path in paths], "port": port, "health": test,
            "memory": memory, "stopGrace": grace, "settings": settings, "volumes": volumes, "sites": sites,
            "needs": needs, "capabilities": capabilities}


# ---------------------------------------------------------------------- change

def glob(pattern):
    """A path glob relative to engineering/: ** crosses folders, * and ? stay inside one."""
    parts, index = [], 0
    while index < len(pattern):
        if pattern.startswith("**/", index):
            parts.append("(?:.*/)?")
            index += 3
        elif pattern.startswith("**", index):
            parts.append(".*")
            index += 2
        elif pattern[index] == "*":
            parts.append("[^/]*")
            index += 1
        elif pattern[index] == "?":
            parts.append("[^/]")
            index += 1
        else:
            parts.append(re.escape(pattern[index]))
            index += 1
    return re.compile(re.escape(ROOT) + "".join(parts))


def triggers(descriptor, name):
    service = descriptor["services"][name]
    return [glob(path) for path in [*service["paths"], *descriptor["shared"], service["build"]["dockerfile"]]]


def changed_services(descriptor, changed_paths):
    return [name for name in descriptor["services"]
            if any(pattern.fullmatch(path) for pattern in triggers(descriptor, name) for path in changed_paths)]


def git(repo, *arguments):
    result = subprocess.run(["git", "-C", str(repo), *arguments], capture_output=True, text=True)
    check(result.returncode == 0, "git " + arguments[0] + " failed: " + (result.stderr.strip().splitlines() or ["?"])[-1])
    return result.stdout


def plan(descriptor, repo, commit, base=None, tag=None):
    """Which services to build and every service's version; `base` is the previous release's manifest."""
    check(SHA.fullmatch(commit), "Resolve the commit to its full SHA first")
    check(tag is None or TAG.fullmatch(tag), "A release tag looks like v1.2.3")
    short = commit[:7]
    if base is None:
        rebuild = list(descriptor["services"])
    else:
        check(base.get("product") == descriptor["product"], "The base release belongs to another product")
        paths = git(repo, "diff", "--name-only", base["sourceCommit"], commit).splitlines()
        rebuild = changed_services(descriptor, paths)
        # A service the base release never shipped has no image to keep.
        rebuild += [name for name in descriptor["services"] if name not in base["images"] and name not in rebuild]
    label = tag or "sha-" + short
    base_version = TAG.fullmatch(base["release"]) and base["release"][1:] if base else None
    services = {}
    for name in descriptor["services"]:
        if name in rebuild:
            version = tag[1:] if tag else (base_version or "0.0.0") + "+" + short
            services[name] = {"build": True, "version": version, "changedIn": label}
        else:
            kept = (base.get("versions") or {}).get(name) or {"version": base_version or base["release"],
                                                               "changedIn": base["release"]}
            services[name] = {"build": False, "version": kept["version"], "changedIn": kept["changedIn"],
                              "image": base["images"][name]}
    return {"product": descriptor["product"], "release": label, "kind": "release" if tag else "candidate",
            "sourceCommit": commit, "base": base["release"] if base else None, "services": services}


# ---------------------------------------------------------------------- bundle

def render(descriptor, planned, images, branch=None, rollback_compatible=False):
    """compose.json and release.json for the planned release, given every service's digest-pinned image."""
    product, platform = descriptor["product"], descriptor["platform"]
    compose_services, volumes = {}, {}
    for name, service in descriptor["services"].items():
        mounts = []
        if service["settings"]:
            variable = name.upper().replace("-", "_") + "_SETTINGS"
            mounts.append({"type": "bind", "source": "${" + variable + ":?Set " + variable + "}",
                           "target": service["settings"]["file"], "read_only": True})
        for volume, path in service["volumes"].items():
            mounts.append({"type": "volume", "source": volume, "target": path})
            volumes[volume] = {}
        definition = {"image": images[name], "platform": platform, "restart": "unless-stopped",
                      "mem_limit": service["memory"], "init": True, "stop_grace_period": service["stopGrace"],
                      "security_opt": ["no-new-privileges:true"], "cap_drop": ["ALL"],
                      "healthcheck": {"test": service["health"], "interval": "10s", "timeout": "5s",
                                      "start_period": "60s", "retries": 6},
                      # The ingress and sibling services reach it as <product>-<environment>-<service>.
                      "networks": {"platform": {"aliases": [product + "-${DEPLOY_ENVIRONMENT}-" + name]}},
                      "logging": {"driver": "json-file", "options": {"max-size": "10m", "max-file": "3"}}}
        if service["capabilities"]:
            definition["cap_add"] = service["capabilities"]
        if mounts:
            definition["volumes"] = mounts
        compose_services[name] = definition
    compose = {"services": compose_services,
               "networks": {"platform": {"external": True, "name": "${PLATFORM_NETWORK:?Set PLATFORM_NETWORK}"}}}
    if volumes:
        compose["volumes"] = volumes
    compose_bytes = (json.dumps(compose, indent=2) + "\n").encode()
    manifest = {"schemaVersion": 1, "product": product, "release": planned["release"], "kind": planned["kind"],
                "sourceCommit": planned["sourceCommit"], "platform": platform,
                "rollbackCompatible": rollback_compatible, "composeSha256": hashlib.sha256(compose_bytes).hexdigest(),
                "images": {name: images[name] for name in descriptor["services"]},
                "requiredConfiguration": {name: service["settings"]["required"]
                                          for name, service in descriptor["services"].items() if service["settings"]},
                "versions": {name: {"version": entry["version"], "changedIn": entry["changedIn"]}
                             for name, entry in planned["services"].items()},
                "sites": {name: service["sites"] for name, service in descriptor["services"].items() if service["sites"]},
                "needs": {name: service["needs"] for name, service in descriptor["services"].items() if service["needs"]}}
    if branch:
        manifest["branch"] = branch
    return compose_bytes, manifest


def write_bundle(output, compose_bytes, manifest, archive=None):
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    (output / "compose.json").write_bytes(compose_bytes)
    (output / "release.json").write_text(json.dumps(manifest, indent=2) + "\n")
    if archive:
        with tarfile.open(archive, "w:gz") as package:
            for name in ("release.json", "compose.json"):
                payload = (output / name).read_bytes()
                member = tarfile.TarInfo(name)
                member.size = len(payload)
                package.addfile(member, io.BytesIO(payload))


# ----------------------------------------------------------------------- build

def docker(*arguments):
    result = subprocess.run(["docker", *arguments], capture_output=True, text=True)
    check(result.returncode == 0, "docker " + arguments[0] + " failed: "
          + (result.stderr.strip().splitlines() or ["?"])[-1][:200])
    return result.stdout


def repository_of(descriptor, name, registry):
    return descriptor["services"][name]["image"] or registry.rstrip("/") + "/" + name


def push_local(reference, repository, tag):
    """Publishes an image that already exists locally (a local server, or a CI job that built it itself)."""
    remote = repository + ":" + tag
    docker("tag", reference, remote)
    docker("push", remote)
    digests = json.loads(docker("image", "inspect", "--format", "{{json .RepoDigests}}", remote))
    return next(item for item in digests if item.startswith(repository + "@"))


def build_image(descriptor, name, checkout, repository, tag):
    service = descriptor["services"][name]["build"]
    engineering = Path(checkout) / "engineering"
    with tempfile.TemporaryDirectory() as directory:
        metadata = Path(directory) / "metadata.json"
        arguments = ["buildx", "build", "--platform", descriptor["platform"], "--push", "--metadata-file", str(metadata),
                     "-f", str(engineering / service["dockerfile"]), "-t", repository + ":" + tag]
        if service["target"]:
            arguments += ["--target", service["target"]]
        for key, value in sorted(service["args"].items()):
            arguments += ["--build-arg", key + "=" + value]
        for key, path in sorted(service["contexts"].items()):
            arguments += ["--build-context", key + "=" + str(engineering / path)]
        docker(*arguments, str(engineering / service["context"]))
        digest = json.loads(metadata.read_text()).get("containerimage.digest", "")
    check(re.fullmatch(r"sha256:[a-f0-9]{64}", digest), name + ": the build reported no image digest")
    return repository + "@" + digest


def build(descriptor, planned, repo, registry, local_images, checkout=None):
    """Builds and pushes every planned service; returns each service's digest-pinned image."""
    images = {name: entry["image"] for name, entry in planned["services"].items() if not entry["build"]}
    tag = planned["release"][1:] if planned["kind"] == "release" else planned["release"]
    pending = [name for name, entry in planned["services"].items() if entry["build"]]
    worktree = None
    try:
        for name in pending:
            repository = repository_of(descriptor, name, registry)
            if name in local_images:
                images[name] = push_local(local_images[name], repository, tag)
                continue
            if checkout is None and worktree is None:
                worktree = tempfile.mkdtemp(prefix="wheelhouse-build-")
                git(repo, "worktree", "add", "--detach", worktree, planned["sourceCommit"])
            images[name] = build_image(descriptor, name, checkout or worktree, repository, tag)
    finally:
        if worktree:
            subprocess.run(["git", "-C", str(repo), "worktree", "remove", "--force", worktree], capture_output=True)
    return images


# ------------------------------------------------------------------------- cli

def load(repo, commit, descriptor_path=None):
    text = Path(descriptor_path).read_text() if descriptor_path else git(repo, "show", commit + ":" + DESCRIPTOR)
    return validate(parse(text))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("action", choices=["validate", "plan", "build"])
    parser.add_argument("--repo", default=".", help="the product repository")
    parser.add_argument("--commit", default="HEAD")
    parser.add_argument("--descriptor", help="a deploy.yml on disk instead of the one at the commit")
    parser.add_argument("--base", help="the previous release's bundle folder; without it every service builds")
    parser.add_argument("--tag", help="the release tag (vX.Y.Z); without it the build is a candidate")
    parser.add_argument("--branch", help="the branch the commit was built from")
    parser.add_argument("--platform", choices=PLATFORMS, help="override the descriptor's platform (a local server)")
    parser.add_argument("--registry", help="image prefix, e.g. ghcr.io/owner/repo; each service is <prefix>/<name>")
    parser.add_argument("--image", action="append", default=[], metavar="SERVICE=REF",
                        help="publish an image that already exists locally instead of building the service")
    parser.add_argument("--checkout", action="store_true", help="build from --repo as checked out (CI)")
    parser.add_argument("--rollback-compatible", action="store_true")
    parser.add_argument("--output", help="bundle folder to write")
    parser.add_argument("--archive", help="also write a .tar.gz of the bundle for import")
    args = parser.parse_args()
    try:
        if args.action == "validate":
            descriptor = validate(parse(Path(args.descriptor or Path(args.repo) / DESCRIPTOR).read_text()))
            print(json.dumps({"product": descriptor["product"], "services": sorted(descriptor["services"])}))
            return 0
        commit = git(args.repo, "rev-parse", "--verify", args.commit + "^{commit}").strip()
        descriptor = load(args.repo, commit, args.descriptor)
        if args.platform:
            descriptor["platform"] = args.platform
        base = json.loads((Path(args.base) / "release.json").read_text()) if args.base else None
        planned = plan(descriptor, args.repo, commit, base, args.tag)
        if args.action == "plan":
            print(json.dumps(planned, indent=2))
            return 0
        check(args.registry and args.output, "build needs --registry and --output")
        local_images = dict(item.split("=", 1) for item in args.image)
        check(set(local_images) <= set(descriptor["services"]), "--image names an unknown service")
        images = build(descriptor, planned, args.repo, args.registry, local_images, args.repo if args.checkout else None)
        compose_bytes, manifest = render(descriptor, planned, images, args.branch, args.rollback_compatible)
        write_bundle(args.output, compose_bytes, manifest, args.archive)
        print(json.dumps({"release": manifest["release"], "kind": manifest["kind"], "output": args.output,
                          "built": [name for name, entry in planned["services"].items() if entry["build"]]}))
        return 0
    except Invalid as error:
        print(json.dumps({"status": "rejected", "reason": str(error)}), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
