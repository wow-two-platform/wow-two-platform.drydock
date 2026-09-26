#!/usr/bin/env python3
"""Provider-neutral, target-side release executor. Requires Python 3 and Docker Compose v2."""
import argparse
import base64
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
PROXIES = "Deployment:TrustedProxies"


class Rejected(ValueError):
    """A refused precondition. Messages hold only static text and contract key names."""


class CommandFailed(RuntimeError):
    """A failed external step. Messages hold only the step name and exit status."""


def require(condition, message):
    if not condition:
        raise Rejected(message)


def reason(error):
    # Any other message can echo inputs or credential values; only these carry operator-safe text.
    return str(error)[:300] if isinstance(error, (Rejected, CommandFailed)) else None


def rejection(error):
    result = {"status": "rejected", "failure": type(error).__name__}
    if reason(error):
        result["reason"] = reason(error)
    return result


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


def base_environment(target):
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
    return environment


def validate_settings(target, service, required):
    require(service in target.get("settings", {}), "Provide settings for every service")
    path = Path(target["settings"][service])
    require(path.is_absolute() and path.is_file() and not path.is_symlink(),
            "Settings must be an absolute regular file: " + service)
    require(path.stat().st_mode & 0o022 == 0 and
            (path.stat().st_mode & 0o077 == 0 or path.parent.stat().st_mode & 0o077 == 0),
            "Settings files must be private or inside a private directory: " + service)
    try:
        configuration = read_json(path)
    except ValueError:
        raise Rejected("Settings are not valid JSON: " + service) from None
    for key in required:
        require(configuration_value(configuration, key) not in (None, "", []), "Missing required setting: " + service + ":" + key)
    hosts = configuration.get("AllowedHosts") if isinstance(configuration, dict) else None
    if isinstance(hosts, str):
        # Health checks and smoke probes reach every service as http://localhost:8080.
        entries = {host.strip().lower() for host in hosts.split(";") if host.strip()}
        require(not entries or "*" in entries or "localhost" in entries,
                "AllowedHosts must include localhost for health probes: " + service)
    # ASP.NET binds only a JSON array here; a plain string silently trusts no proxy.
    proxies = configuration_value(configuration, PROXIES)
    require(proxies is None or isinstance(proxies, list), PROXIES + " must be a JSON array: " + service)
    return path


def environment_for(target, manifest):
    environment = base_environment(target)
    require(target.get("settings", {}).keys() == manifest["images"].keys(), "Provide settings for every service")
    for service, required in manifest["requiredConfiguration"].items():
        environment[service.upper().replace("-", "_") + "_SETTINGS"] = str(validate_settings(target, service, required))
    return environment


@contextlib.contextmanager
def deployment_lock(root):
    root.mkdir(parents=True, exist_ok=True, mode=0o700)
    with (root / "lock").open("a") as stream:
        try:
            fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise Rejected("Another deployment owns this target")
        yield


class Docker:
    def __init__(self, target, environment):
        self.target = target
        self.environment = environment
        self.project = target["product"] + "-" + target["environment"]

    def execute(self, arguments, step, timeout=600):
        # Command output can contain runtime secrets. Persist only the step name and exit code.
        try:
            result = subprocess.run(arguments, env=self.environment, capture_output=True, text=True, timeout=timeout)
        except subprocess.TimeoutExpired:
            raise CommandFailed(step + " timed out after " + str(timeout) + "s") from None
        if result.returncode:
            raise CommandFailed(step + " failed (exit " + str(result.returncode) + "); inspect target containers privately")
        return result.stdout

    def preflight(self, manifest):
        architecture = self.execute(["docker", "info", "--format", "{{.OSType}}/{{.Architecture}}"], "docker info").strip()
        architecture = architecture.replace("x86_64", "amd64").replace("aarch64", "arm64")
        require(architecture == manifest["platform"], "Target architecture differs from release")
        self.execute(["docker", "compose", "version"], "docker compose version", timeout=30)

    def compose(self, bundle, *arguments):
        return self.execute(["docker", "compose", "--project-name", self.project,
                             "--env-file", "/dev/null", "-f", str(Path(bundle) / "compose.json"), *arguments],
                            "compose " + arguments[0])

    def unhealthy(self, bundle):
        # Diagnosis after a failed wait: service names and container states only, never logs.
        try:
            output = self.compose(bundle, "ps", "--all", "--format", "json").strip()
            rows = json.loads(output) if output.startswith("[") else [json.loads(line) for line in output.splitlines()]
        except (CommandFailed, ValueError):
            return []
        return sorted(row.get("Service", "unknown") + " (" + (row.get("Health") or row.get("State") or "unknown") + ")"
                      for row in rows if isinstance(row, dict) and row.get("Health") != "healthy")

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
            details = json.loads(self.execute(["docker", "inspect", container], "docker inspect"))[0]
            require(details["Config"]["Image"] == image, "Running image differs from release: " + service)
            health = details["State"].get("Health", {}).get("Status")
            require(health == "healthy", "Service failed readiness: " + service + " (" + str(health) + ")")
        # Target-owned smoke checks cannot be supplied by an uploaded release.
        for probe in self.target.get("smoke", []):
            require(probe.get("service") in manifest["images"], "Unknown smoke service")
            require(re.fullmatch(r"/[A-Za-z0-9/_?=&.%~-]*", probe.get("path", "")), "Invalid smoke path")
            expected = probe.get("status", 200)
            require(type(expected) is int and 200 <= expected <= 499, "Invalid smoke status")
            status = self.compose(bundle, "exec", "-T", probe["service"], "curl", "--silent",
                                  "--output", "/dev/null", "--write-out", "%{http_code}",
                                  "--max-time", "10", "http://localhost:8080" + probe["path"])
            require(status.strip() == str(expected), "Smoke check failed: " + probe["service"] + " " + probe["path"]
                    + " returned " + status.strip()[:3] + ", expected " + str(expected))


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
            if reason(error):
                record["reason"] = reason(error)
            if mutation_started and isinstance(error, CommandFailed):
                with contextlib.suppress(Exception):
                    states = docker.unhealthy(destination)
                    if states:
                        record["reason"] += "; not healthy: " + ", ".join(states)
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
        require(active["status"] == "running" or active.get("mutationStarted"),
                "The deployment changed no containers; nothing to reconcile")
        # Acknowledgement does not assert that previous images still match the observed schema.
        current = root / project / "current.json"
        if current.exists():
            current.unlink()
        active["status"] = "interrupted"
        active["completedAt"] = now()
        write_json(root / project / "active.json", active)
        write_json(root / project / "jobs" / (job_id + ".json"), active)
        return active


def lock_held(base):
    # A shared probe fails only while a rollout holds the exclusive lock; the file is never created here.
    try:
        with (base / "lock").open("r") as stream:
            fcntl.flock(stream, fcntl.LOCK_SH | fcntl.LOCK_NB)
            fcntl.flock(stream, fcntl.LOCK_UN)
            return False
    except FileNotFoundError:
        return False
    except BlockingIOError:
        return True


def summary(record):
    fields = ("id", "release", "sourceCommit", "status", "actor", "startedAt", "completedAt",
              "failure", "reason", "mutationStarted", "previous")
    return None if record is None else {key: record[key] for key in fields if key in record}


def state(target):
    root, project = validate_target(target)
    base = root / project
    current = read_json(base / "current.json") if (base / "current.json").is_file() else None
    active = read_json(base / "active.json") if (base / "active.json").is_file() else None
    condition = "ready"
    if active and active.get("status") == "running":
        # Only a status probe of a running record, so it can never refuse an otherwise valid rollout.
        condition = "running" if lock_held(base) else "needs_reconciliation"
    elif active and active.get("mutationStarted") and active.get("status") in ("failed", "rollback_failed"):
        condition = "needs_reconciliation"
    return {"project": project, "condition": condition, "current": summary(current), "active": summary(active)}


def check(target, manifest=None, docker_factory=None):
    """Reads the target without changing it; every detail is operator-safe text."""
    checks = []

    def probe(name, action):
        try:
            detail = action()
            checks.append({"name": name, "ok": True, "detail": detail or "ok"})
        except (Rejected, CommandFailed) as error:
            checks.append({"name": name, "ok": False, "detail": reason(error)})
        except Exception as error:
            checks.append({"name": name, "ok": False, "detail": type(error).__name__})

    root, project = validate_target(target, manifest)
    docker = (docker_factory or Docker)(target, base_environment(target))

    def writable_root():
        existing = next(path for path in (root, *root.parents) if path.exists())
        require(os.access(existing, os.W_OK | os.X_OK), "Deployment root is not writable: " + str(existing))
        return str(root) + (" exists" if root.exists() else " will be created")

    def architecture():
        value = docker.execute(["docker", "info", "--format", "{{.OSType}}/{{.Architecture}}"], "docker info").strip()
        value = value.replace("x86_64", "amd64").replace("aarch64", "arm64")
        if manifest:
            require(value == manifest["platform"], "Target is " + value + "; release needs " + manifest["platform"])
        return value

    def compose():
        return docker.execute(["docker", "compose", "version", "--short"], "docker compose version", timeout=30).strip()

    def disk():
        existing = next(path for path in (root, *root.parents) if path.exists())
        free = shutil.disk_usage(existing).free
        require(free >= target.get("minimumFreeBytes", 1024 ** 3), "Low disk: " + str(free // 1024 ** 2) + " MiB free")
        return str(free // 1024 ** 3) + " GiB free"

    def network():
        name = target.get("variables", {}).get("PLATFORM_NETWORK")
        if not name:
            return "no shared network declared"
        docker.execute(["docker", "network", "inspect", "--format", "{{.Name}}", name], "docker network inspect")
        return name

    probe("Deployment root", writable_root)
    probe("Docker", architecture)
    probe("Compose", compose)
    probe("Disk", disk)
    probe("Network", network)
    services = manifest["requiredConfiguration"] if manifest else {name: [] for name in target.get("settings", {})}
    for service, required in services.items():
        probe("Settings: " + service, lambda service=service, required=required:
              str(validate_settings(target, service, required).name) + " valid")
    current = state(target)
    checks.append({"name": "State", "ok": current["condition"] != "needs_reconciliation",
                   "detail": current["condition"].replace("_", " ")})
    return {"project": project, "ok": all(item["ok"] for item in checks), "checks": checks, "state": current}


SIZE_UNITS = {"B": 1, "kB": 1000, "KB": 1000, "MB": 1000 ** 2, "GB": 1000 ** 3, "TB": 1000 ** 4,
              "KiB": 1024, "MiB": 1024 ** 2, "GiB": 1024 ** 3, "TiB": 1024 ** 4}


def size_bytes(text):
    match = re.fullmatch(r"\s*([0-9.]+)\s*([A-Za-z]+)\s*", text or "")
    return int(float(match.group(1)) * SIZE_UNITS[match.group(2)]) if match and match.group(2) in SIZE_UNITS else None


def percent(text):
    match = re.fullmatch(r"\s*([0-9.]+)%\s*", text or "")
    return float(match.group(1)) if match else None


def host_vitals(root):
    """Load, memory, disks and uptime from /proc and statvfs; a field stays None where the host hides it."""
    def read(path):
        try:
            return Path(path).read_text()
        except OSError:
            return ""

    memory = {}
    for line in read("/proc/meminfo").splitlines():
        name, _, value = line.partition(":")
        if name in ("MemTotal", "MemAvailable") and value.split():
            memory[name] = int(value.split()[0]) * 1024
    load = read("/proc/loadavg").split()[:3]
    uptime = read("/proc/uptime").split()[:1]
    disks, seen = [], set()
    # The deployment root and Docker's data root, once per filesystem. Container mounts give one disk several
    # device ids, so identical capacity counts as the same disk too.
    for path in (root, Path("/var/lib/docker")):
        existing = next((item for item in (path, *path.parents) if item.exists()), None)
        try:
            device, usage = existing.stat().st_dev, shutil.disk_usage(existing)
        except (AttributeError, OSError):
            continue
        if device not in seen and (usage.total, usage.free) not in seen:
            seen.update((device, (usage.total, usage.free)))
            disks.append({"path": str(existing), "totalBytes": usage.total, "freeBytes": usage.free})
    return {"cpus": os.cpu_count(), "load": [float(value) for value in load] if len(load) == 3 else None,
            "memoryTotalBytes": memory.get("MemTotal"), "memoryAvailableBytes": memory.get("MemAvailable"),
            "uptimeSeconds": int(float(uptime[0])) if uptime else None, "disks": disks}


def container_vitals(docker, project):
    """State, health, restarts and resource use per container; never labels, environment or logs."""
    ids = docker.execute(["docker", "ps", "--all", "--quiet", "--no-trunc", "--filter",
                          "label=com.docker.compose.project=" + project], "docker ps", timeout=30).split()
    if not ids:
        return []
    details = json.loads(docker.execute(["docker", "inspect", *ids], "docker inspect", timeout=30))
    running = [item["Id"] for item in details if item.get("State", {}).get("Running")]
    usage = []
    if running:
        output = docker.execute(["docker", "stats", "--no-stream", "--no-trunc", "--format", "{{json .}}", *running],
                                "docker stats", timeout=30)
        for line in output.splitlines():
            try:
                row = json.loads(line)
            except ValueError:
                continue
            if isinstance(row, dict) and row.get("ID"):
                usage.append(row)
    result = []
    for item in details:
        state = item.get("State") or {}
        labels = (item.get("Config") or {}).get("Labels") or {}
        row = next((row for row in usage if item["Id"].startswith(row["ID"]) or row["ID"].startswith(item["Id"])), {})
        used, _, limit = (row.get("MemUsage") or "").partition("/")
        result.append({"service": labels.get("com.docker.compose.service") or item.get("Name", "").lstrip("/"),
                       "state": state.get("Status"), "health": (state.get("Health") or {}).get("Status"),
                       "restarts": item.get("RestartCount", 0),
                       # Docker reports a never-started container as year 1.
                       "startedAt": None if str(state.get("StartedAt")).startswith("0001-") else state.get("StartedAt"),
                       "exitCode": None if state.get("Running") else state.get("ExitCode"),
                       "cpuPercent": percent(row.get("CPUPerc")), "memoryBytes": size_bytes(used),
                       "memoryLimitBytes": size_bytes(limit)})
    return sorted(result, key=lambda container: container["service"])


def vitals(target, docker_factory=None):
    """Reads host and container vitals without changing the target; every field is operator-safe."""
    root, project = validate_target(target)
    docker = (docker_factory or Docker)(target, base_environment(target))
    result = {"project": project, "host": None, "containers": None, "problems": []}
    try:
        result["host"] = host_vitals(root)
    except (OSError, ValueError):
        result["problems"].append("Host vitals were unreadable")
    try:
        result["containers"] = container_vitals(docker, project)
    except (Rejected, CommandFailed) as error:
        result["problems"].append(reason(error))
    except (ValueError, KeyError, TypeError):
        result["problems"].append("Container details were unreadable")
    current = state(target)
    result.update(condition=current["condition"], release=(current["current"] or {}).get("release"))
    return result


def decode(value):
    # Transport passes documents as base64 arguments so read-only calls leave no files on the target.
    return json.loads(base64.b64decode(value, validate=True))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["validate", "apply", "launch", "status", "acknowledge", "state", "check",
                                           "vitals"])
    parser.add_argument("--bundle")
    parser.add_argument("--target")
    parser.add_argument("--target-json")
    parser.add_argument("--release-json")
    parser.add_argument("--actor", default="operator")
    parser.add_argument("--job")
    args = parser.parse_args()
    try:
        target = decode(args.target_json) if args.target_json else None
        if args.action == "validate":
            result = validate_bundle(args.bundle)
        elif args.action == "launch":
            result = launch(args.bundle, args.target, args.actor)
        elif args.action == "status":
            result = status(target or read_json(args.target), args.job)
        elif args.action == "acknowledge":
            result = acknowledge(target or read_json(args.target), args.job)
        elif args.action == "state":
            result = state(target or read_json(args.target))
        elif args.action == "check":
            result = check(target or read_json(args.target), decode(args.release_json) if args.release_json else None)
        elif args.action == "vitals":
            result = vitals(target or read_json(args.target))
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
                           {"id": args.job, **rejection(error), "completedAt": now()})
            except Exception:
                pass
        print(json.dumps(rejection(error)), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
