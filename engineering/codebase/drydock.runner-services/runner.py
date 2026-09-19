#!/usr/bin/env python3
"""Provider-neutral, target-side release executor. Requires Python 3 and Docker Compose v2."""
import argparse
import contextlib
import datetime
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import uuid

SLUG = re.compile(r"[a-z][a-z0-9-]{0,47}")
IMAGE = re.compile(r"[a-z0-9][a-z0-9./:_-]*@sha256:[a-f0-9]{64}")
SHA = re.compile(r"[a-f0-9]{40}")
TERMINAL = {"succeeded", "failed", "rolled_back", "rollback_failed", "interrupted", "rejected"}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(Path(path).read_text())


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    temporary = path.with_suffix(".tmp")
    with temporary.open("w") as stream:
        os.chmod(temporary, 0o600)
        json.dump(value, stream, indent=2)
        stream.write("\n")
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)
    directory = os.open(path.parent, os.O_RDONLY)
    try:
        os.fsync(directory)
    finally:
        os.close(directory)


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def validate_bundle(bundle):
    bundle = Path(bundle).resolve()
    manifest = read_json(bundle / "release.json")
    compose_path = bundle / "compose.json"
    compose = read_json(compose_path)
    require(manifest.get("schemaVersion") == 1, "Unsupported release schema")
    require(SLUG.fullmatch(manifest.get("product", "")), "Invalid product")
    require(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,95}", manifest.get("release", "")), "Invalid release")
    require(SHA.fullmatch(manifest.get("sourceCommit", "")), "A full source commit is required")
    require(manifest.get("platform") in ("linux/amd64", "linux/arm64"), "Unsupported platform")
    require(type(manifest.get("rollbackCompatible")) is bool, "Declare rollback compatibility")
    require(hashlib.sha256(compose_path.read_bytes()).hexdigest() == manifest.get("composeSha256"), "Compose hash mismatch")
    images = manifest.get("images", {})
    services = compose.get("services", {})
    require(images and images.keys() == services.keys(), "Service image map mismatch")
    for name, image in images.items():
        require(SLUG.fullmatch(name) and IMAGE.fullmatch(image), "Services require immutable image digests")
        service = services[name]
        require(service.get("image") == image, "Compose image differs from release")
        require(service.get("platform") == manifest["platform"], "Compose platform differs from release")
        require("build" not in service, "Releases cannot build on the target")
        health = service.get("healthcheck", {})
        require(health.get("test") and not health.get("disable"), "Every service requires a health gate")
        require(health["test"][0] != "NONE", "Disabled health gate")
        require(not service.get("privileged") and service.get("network_mode") != "host", "Privileged workload denied")
        require(not service.get("container_name"), "Global container names prevent environment isolation")
    required = manifest.get("requiredConfiguration", {})
    require(required.keys() == services.keys(), "Configuration contract must cover every service")
    for fields in required.values():
        require(isinstance(fields, list) and all(isinstance(key, str) for key in fields), "Invalid configuration contract")
    return manifest


def validate_target(target, manifest=None):
    require(SLUG.fullmatch(target.get("product", "")), "Invalid target product")
    require(SLUG.fullmatch(target.get("environment", "")), "Invalid target environment")
    project = target["product"] + "-" + target["environment"]
    root = Path(target.get("root", ""))
    require(root.is_absolute() and str(root) not in ("/", "/tmp", "/srv"), "Use a dedicated absolute deployment root")
    require(not root.is_symlink(), "Deployment root cannot be a symlink")
    if manifest:
        require(target["product"] == manifest["product"], "Target product mismatch")
    return root, project


def configuration_value(value, field):
    for key in field.split(":"):
        value = value.get(key) if isinstance(value, dict) else None
    return value


def environment_for(target, manifest):
    # Host files are the sole secret source. Never inherit ambient Compose or application overrides.
    environment = {key: value for key, value in os.environ.items()
                   if key in ("PATH", "HOME", "DOCKER_HOST", "DOCKER_CONFIG", "XDG_RUNTIME_DIR")}
    environment["COMPOSE_DISABLE_ENV_FILE"] = "1"
    environment["DEPLOY_ENVIRONMENT"] = target["environment"]
    for name, value in target.get("variables", {}).items():
        require(re.fullmatch(r"[A-Z][A-Z0-9_]*", name) and not name.startswith(("COMPOSE_", "DOCKER_")),
                "Invalid target variable")
        require(isinstance(value, str) and "\n" not in value, "Invalid target variable value")
        environment[name] = value
    settings = target.get("settings", {})
    require(settings.keys() == manifest["images"].keys(), "Provide settings for every service")
    for service, required in manifest["requiredConfiguration"].items():
        path = Path(settings[service])
        require(path.is_absolute() and path.is_file() and not path.is_symlink(), "Settings must be an absolute regular file")
        require(path.stat().st_mode & 0o022 == 0 and
                (path.stat().st_mode & 0o077 == 0 or path.parent.stat().st_mode & 0o077 == 0),
                "Settings files must be private or inside a private directory")
        configuration = read_json(path)
        for key in required:
            require(configuration_value(configuration, key) not in (None, "", []), "Missing required setting: " + service + ":" + key)
        environment[service.upper().replace("-", "_") + "_SETTINGS"] = str(path)
    return environment


@contextlib.contextmanager
def deployment_lock(root):
    root.mkdir(parents=True, exist_ok=True, mode=0o700)
    with (root / "lock").open("a") as stream:
        try:
            fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError("Another deployment owns this target")
        yield


class Docker:
    def __init__(self, target, environment):
        self.target = target
        self.environment = environment
        self.project = target["product"] + "-" + target["environment"]

    def execute(self, arguments, timeout=600):
        # Command output can contain runtime secrets. Persist only the command category and exit code.
        result = subprocess.run(arguments, env=self.environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            raise RuntimeError("Deployment command failed (exit " + str(result.returncode) + "); inspect target containers privately")
        return result.stdout

    def preflight(self, manifest):
        architecture = self.execute(["docker", "info", "--format", "{{.OSType}}/{{.Architecture}}"]).strip()
        architecture = architecture.replace("x86_64", "amd64").replace("aarch64", "arm64")
        require(architecture == manifest["platform"], "Target architecture differs from release")
        self.execute(["docker", "compose", "version"], timeout=30)

    def compose(self, bundle, *arguments):
        return self.execute(["docker", "compose", "--project-name", self.project,
                             "--env-file", "/dev/null", "-f", str(Path(bundle) / "compose.json"), *arguments])

    def pull(self, bundle):
        self.compose(bundle, "config", "--quiet")
        self.compose(bundle, "pull", "--policy", "always")

    def up(self, bundle):
        timeout = self.target.get("healthTimeoutSeconds", 120)
        require(type(timeout) is int and 10 <= timeout <= 300, "Invalid health timeout")
        self.compose(bundle, "up", "--detach", "--wait", "--wait-timeout", str(timeout),
                     "--remove-orphans", "--pull", "never")

    def verify(self, bundle, manifest):
        for service, image in manifest["images"].items():
            container = self.compose(bundle, "ps", "--all", "--quiet", service).strip()
            require(container and "\n" not in container, "Expected exactly one container per service")
            details = json.loads(self.execute(["docker", "inspect", container]))[0]
            require(details["Config"]["Image"] == image, "Running image differs from release")
            require(details["State"].get("Health", {}).get("Status") == "healthy", "Service failed readiness")
        # Target-owned smoke checks cannot be supplied by an uploaded release.
        for probe in self.target.get("smoke", []):
            require(probe.get("service") in manifest["images"], "Unknown smoke service")
            require(re.fullmatch(r"/[A-Za-z0-9/_?=&.%~-]*", probe.get("path", "")), "Invalid smoke path")
            expected = probe.get("status", 200)
            require(type(expected) is int and 200 <= expected <= 499, "Invalid smoke status")
            status = self.compose(bundle, "exec", "-T", probe["service"], "curl", "--silent",
                                  "--output", "/dev/null", "--write-out", "%{http_code}",
                                  "--max-time", "10", "http://localhost:8080" + probe["path"])
            require(status.strip() == str(expected), "Application smoke check failed")


def apply(bundle, target, actor, job_id=None, docker_factory=Docker):
    manifest = validate_bundle(bundle)
    root, project = validate_target(target, manifest)
    root = root / project
    require(isinstance(actor, str) and 0 < len(actor) <= 160 and "\n" not in actor, "Invalid actor")
    job_id = job_id or str(uuid.uuid4())
    require(str(uuid.UUID(job_id)) == job_id, "Invalid deployment id")
    with deployment_lock(root):
        active_path = root / "active.json"
        active = read_json(active_path) if active_path.exists() else None
        require(not active or active["status"] in TERMINAL, "Interrupted deployment requires reconciliation")
        require(not active or not (active.get("mutationStarted") and active["status"] in ("failed", "rollback_failed")),
                "Failed mutation requires reconciliation")
        environment = environment_for(target, manifest)
        require(shutil.disk_usage(root).free >= target.get("minimumFreeBytes", 1024 ** 3), "Insufficient free disk")
        docker = docker_factory(target, environment)
        docker.preflight(manifest)
        destination = root / "releases" / job_id
        destination.mkdir(parents=True, mode=0o700)
        for filename in ("release.json", "compose.json"):
            shutil.copyfile(Path(bundle) / filename, destination / filename)
        # Revalidate the immutable snapshot, not only the source directory.
        manifest = validate_bundle(destination)
        previous_path = root / "current.json"
        previous = read_json(previous_path) if previous_path.exists() else None
        record = {"id": job_id, "project": project, "release": manifest["release"],
                  "sourceCommit": manifest["sourceCommit"], "actor": actor, "status": "running",
                  "startedAt": now(), "previous": previous.get("id") if previous else None}
        def save():
            write_json(root / "jobs" / (job_id + ".json"), record)
            write_json(active_path, record)
        save()
        mutation_started = False
        try:
            docker.pull(destination)
            mutation_started = True
            record["mutationStarted"] = True
            save()
            docker.up(destination)
            docker.verify(destination, manifest)
            record["status"] = "succeeded"
            write_json(previous_path, {"id": job_id, "release": manifest["release"]})
        except (Exception, KeyboardInterrupt) as error:
            record["status"] = "failed"
            # Only safe diagnostic categories; exception messages may include input/credential values.
            record["failure"] = type(error).__name__
            if mutation_started and previous and manifest["rollbackCompatible"]:
                try:
                    prior_bundle = root / "releases" / previous["id"]
                    prior_manifest = validate_bundle(prior_bundle)
                    prior_environment = environment_for(target, prior_manifest)
                    prior_docker = docker_factory(target, prior_environment)
                    prior_docker.pull(prior_bundle)
                    prior_docker.up(prior_bundle)
                    prior_docker.verify(prior_bundle, prior_manifest)
                    record["status"] = "rolled_back"
                except Exception:
                    record["status"] = "rollback_failed"
        record["completedAt"] = now()
        save()
        return record


def status(target, job_id):
    root, project = validate_target(target)
    require(str(uuid.UUID(job_id)) == job_id, "Invalid deployment id")
    return read_json(root / project / "jobs" / (job_id + ".json"))


def launch(bundle, target_path, actor):
    target = read_json(target_path)
    manifest = validate_bundle(bundle)
    root, project = validate_target(target, manifest)
    job_id = str(uuid.uuid4())
    jobs = root / project / "jobs"
    jobs.mkdir(parents=True, exist_ok=True, mode=0o700)
    write_json(jobs / (job_id + ".json"), {"id": job_id, "status": "queued", "actor": actor, "queuedAt": now()})
    # The target process owns the rollout across SSH/control-plane disconnects.
    subprocess.Popen([sys.executable, str(Path(__file__).resolve()), "apply", "--bundle", str(Path(bundle).resolve()),
                      "--target", str(Path(target_path).resolve()), "--actor", actor, "--job", job_id],
                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                     start_new_session=True, close_fds=True)
    return {"id": job_id, "status": "queued"}


def acknowledge(target, job_id):
    root, project = validate_target(target)
    with deployment_lock(root / project):
        active = read_json(root / project / "active.json")
        require(active["id"] == job_id and active["status"] in ("running", "failed", "rollback_failed"),
                "No matching interrupted deployment")
        # Acknowledgement does not assert that previous images still match the observed schema.
        current = root / project / "current.json"
        if current.exists():
            current.unlink()
        active["status"] = "interrupted"
        active["completedAt"] = now()
        write_json(root / project / "active.json", active)
        write_json(root / project / "jobs" / (job_id + ".json"), active)
        return active


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["validate", "apply", "launch", "status", "acknowledge"])
    parser.add_argument("--bundle")
    parser.add_argument("--target")
    parser.add_argument("--actor", default="operator")
    parser.add_argument("--job")
    args = parser.parse_args()
    try:
        if args.action == "validate":
            result = validate_bundle(args.bundle)
        elif args.action == "launch":
            result = launch(args.bundle, args.target, args.actor)
        elif args.action == "status":
            result = status(read_json(args.target), args.job)
        elif args.action == "acknowledge":
            result = acknowledge(read_json(args.target), args.job)
        else:
            result = apply(args.bundle, read_json(args.target), args.actor, args.job)
        print(json.dumps(result))
        return 1 if args.action == "apply" and result.get("status") != "succeeded" else 0
    except Exception as error:
        # A launched worker must not leave its own job queued if preflight rejected it.
        if args.action == "apply" and args.job:
            try:
                root, project = validate_target(read_json(args.target))
                write_json(root / project / "jobs" / (args.job + ".json"),
                           {"id": args.job, "status": "rejected", "failure": type(error).__name__, "completedAt": now()})
            except Exception:
                pass
        print(json.dumps({"status": "rejected", "failure": type(error).__name__}), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
