#!/usr/bin/env python3
"""SSH adapter used by DryDock and the operator CLI. Inventory and bundles are trusted local files."""
import argparse
import json
from pathlib import Path
import re
import shlex
import subprocess
import sys
import tempfile
import uuid
from runner import SLUG, require, read_json, write_json, validate_bundle, validate_target


def child(root, folder, identifier, suffix=""):
    require(SLUG.fullmatch(identifier), "Invalid identifier")
    path = (root / folder / (identifier + suffix)).resolve()
    require(path.is_relative_to((root / folder).resolve()), "Path escaped inventory")
    return path


class Ssh:
    def __init__(self, config):
        self.config = config
        require(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9.-]*", config.get("host", "")), "Invalid SSH host")
        require(re.fullmatch(r"[a-z_][a-z0-9_-]*", config.get("user", "")), "Invalid SSH user")
        require(type(config.get("port", 22)) is int and 1 <= config.get("port", 22) <= 65535, "Invalid SSH port")
        for key in ("keyFile", "knownHostsFile"):
            require(Path(config.get(key, "")).is_absolute() and Path(config[key]).is_file(), "Missing SSH identity file")
        self.destination = config["user"] + "@" + config["host"]

    def options(self):
        return ["-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes",
                "-o", "IdentitiesOnly=yes", "-o", "ConnectTimeout=10",
                "-o", "UserKnownHostsFile=" + self.config["knownHostsFile"], "-i", self.config["keyFile"]]

    def run(self, command):
        result = subprocess.run(["ssh", *self.options(), "-p", str(self.config.get("port", 22)),
                                 self.destination, command], capture_output=True, text=True, timeout=30)
        require(result.returncode == 0, "SSH operation failed; inspect the target privately")
        return result.stdout

    def copy(self, files, destination):
        result = subprocess.run(["scp", *self.options(), "-P", str(self.config.get("port", 22)),
                                 *map(str, files), self.destination + ":" + destination + "/"],
                                capture_output=True, text=True, timeout=30)
        require(result.returncode == 0, "SSH transfer failed")


def targets(root):
    result = []
    for path in sorted((root / "targets").glob("*.json")):
        config = read_json(path)
        validate_target(config["target"])
        result.append({"id": path.stem, "product": config["target"]["product"],
                       "environment": config["target"]["environment"],
                       "serverId": config.get("serverId"), "host": config["ssh"]["host"]})
    return result


def releases(root):
    result = []
    for path in sorted((root / "bundles").glob("*/release.json")):
        manifest = validate_bundle(path.parent)
        result.append({"id": path.parent.name, "product": manifest["product"], "release": manifest["release"],
                       "sourceCommit": manifest["sourceCommit"], "platform": manifest["platform"]})
    return result


def submit(root, target_id, bundle_id, actor):
    config = read_json(child(root, "targets", target_id, ".json"))
    bundle = child(root, "bundles", bundle_id)
    manifest = validate_bundle(bundle)
    target_root, _ = validate_target(config["target"], manifest)
    require(re.fullmatch(r"/[A-Za-z0-9/_-]+", str(target_root)), "SSH root requires a simple absolute path")
    require(isinstance(actor, str) and 0 < len(actor) <= 160 and "\n" not in actor, "Invalid actor")
    ssh = Ssh(config["ssh"])
    request_id = str(uuid.uuid4())
    remote = str(target_root / "incoming" / request_id)
    ssh.run("umask 077; mkdir -p " + shlex.quote(remote))
    runner_path = Path(__file__).with_name("runner.py")
    with tempfile.TemporaryDirectory() as temporary:
        target_path = Path(temporary) / "target.json"
        write_json(target_path, config["target"])
        ssh.copy([runner_path, bundle / "release.json", bundle / "compose.json", target_path], remote)
    # Record the submission before launching; remote state remains recoverable if the response is lost.
    record = {"id": request_id, "targetId": target_id, "bundleId": bundle_id, "remote": remote,
              "actor": actor, "status": "submitting"}
    write_json(root / "jobs" / (request_id + ".json"), record)
    command = shlex.join(["python3", remote + "/runner.py", "launch", "--bundle", remote,
                          "--target", remote + "/target.json", "--actor", actor])
    remote_job = json.loads(ssh.run(command))
    record.update(status=remote_job["status"], remoteJobId=remote_job["id"])
    write_json(root / "jobs" / (request_id + ".json"), record)
    return {"id": request_id, "targetId": target_id, "bundleId": bundle_id, "status": record["status"]}


def status(root, job_id):
    require(str(uuid.UUID(job_id)) == job_id, "Invalid deployment id")
    record = read_json(root / "jobs" / (job_id + ".json"))
    if "remoteJobId" not in record:
        return {"id": job_id, "status": "unknown", "targetId": record["targetId"]}
    config = read_json(child(root, "targets", record["targetId"], ".json"))
    remote = record["remote"]
    command = shlex.join(["python3", remote + "/runner.py", "status", "--target", remote + "/target.json",
                          "--job", record["remoteJobId"]])
    result = json.loads(Ssh(config["ssh"]).run(command))
    result["remoteJobId"] = result["id"]
    result["id"] = job_id
    result["targetId"] = record["targetId"]
    write_json(root / "observed" / (job_id + ".json"), result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["targets", "releases", "submit", "status"])
    parser.add_argument("--root", required=True)
    parser.add_argument("--target")
    parser.add_argument("--bundle")
    parser.add_argument("--actor", default="operator")
    parser.add_argument("--job")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    try:
        if args.action == "targets":
            result = targets(root)
        elif args.action == "releases":
            result = releases(root)
        elif args.action == "submit":
            result = submit(root, args.target, args.bundle, args.actor)
        else:
            result = status(root, args.job)
        print(json.dumps(result))
        return 0
    except Exception as error:
        print(json.dumps({"status": "rejected", "failure": type(error).__name__}), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
