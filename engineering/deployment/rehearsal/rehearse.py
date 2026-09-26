#!/usr/bin/env python3
"""Rehearses Wheelhouse deployments against a local SSH target. Local only; never points at a real host."""
import argparse
import base64
import io
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tarfile
import time

HERE = Path(__file__).resolve().parent
STATE = HERE / "state"
INVENTORY = STATE / "inventory"
SECRETS = STATE / "secrets"
KEYS = STATE / "keys"
RUNNERS = HERE.parents[1] / "codebase" / "wheelhouse.runner-services"
TRANSPORT = RUNNERS / "transport.py"
WORKBENCH = HERE.parents[4]
GENERATOR = WORKBENCH / "ventures" / "10x-venture-forever-pin" / "engineering" / "deployment" / "create-release.py"
REGISTRY = "localhost:15000"
VAULT_IMAGE = "secrets-vault:local"
TARGET = "foreverpin-rehearsal"
CONSOLE = STATE / "console"
CONSOLE_URL = "http://localhost:18210"
# The GitHub login that may sign in to the local console; change it in the generated settings if needed.
CONSOLE_ADMIN = os.environ.get("WHEELHOUSE_ADMIN", "sulton-max")
TERMINAL = {"succeeded", "failed", "rolled_back", "rollback_failed", "interrupted", "rejected"}


def run(*arguments, env=None, capture=True):
    result = subprocess.run(arguments, env=env, capture_output=capture, text=True)
    if result.returncode:
        detail = (result.stderr or result.stdout or "").strip().splitlines()[-1:] if capture else []
        sys.exit("Failed: " + " ".join(map(str, arguments[:3])) + (" — " + detail[0] if detail else ""))
    return result.stdout if capture else ""


def private_dir(path):
    path.mkdir(parents=True, exist_ok=True)
    path.chmod(0o700)
    return path


def private_file(path, text):
    path.write_text(text)
    path.chmod(0o600)


def environment():
    return {**os.environ, "REHEARSAL_STATE": str(STATE), "WHEELHOUSE_REHEARSAL": "1"}


def transport(*arguments):
    return json.loads(run(sys.executable, str(TRANSPORT), *arguments, "--root", str(INVENTORY), env=environment()))


def vault_files():
    """Bootstraps the rehearsal vault once, like its own bootstrap script, with a generated password."""
    folder = private_dir(STATE / "vault")
    credential = private_dir(INVENTORY / "vaults" / "rehearsal-vault") / "password"
    if (folder / "master_key").exists():
        return
    password = secrets.token_urlsafe(24)
    hashed = subprocess.run(["docker", "run", "--rm", "-i", "--network", "none", VAULT_IMAGE, "--hash-password"],
                            input=password + "\n", capture_output=True, text=True)
    if hashed.returncode or not hashed.stdout.strip().startswith("$argon2id$"):
        sys.exit("Vault password hashing failed; build " + VAULT_IMAGE + " from the secrets-vault repository")
    db_password = secrets.token_hex(32)
    files = {
        "master_key": base64.b64encode(secrets.token_bytes(32)).decode(),
        "admin_password_hash": hashed.stdout.strip(),
        "jwt_key": base64.b64encode(secrets.token_bytes(32)).decode(),
        "db_admin_password": secrets.token_hex(32),
        "db_init.sql": "CREATE ROLE vault LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '" + db_password
                       + "';\nCREATE DATABASE secrets_vault OWNER vault;",
        "db_connection": "Host=vault-db;Port=5432;Database=secrets_vault;Username=vault;Password=" + db_password,
    }
    for name, value in files.items():
        private_file(folder / name, value + "\n")
    private_file(credential, password + "\n")


def console_files():
    """Settings for the containerized console. The developer adds the GitHub OAuth app's id and secret."""
    private_dir(CONSOLE)
    password = CONSOLE / "database-password"
    if not password.exists():
        private_file(password, secrets.token_hex(24))
    settings = CONSOLE / "appsettings.Local.json"
    if not settings.exists():
        private_file(settings, json.dumps({
            "ConnectionStrings": {"Wheelhouse": "Host=console-db;Database=wheelhouse;Username=wheelhouse;Password="
                                                + password.read_text().strip()},
            "Identity": {"GitHub": {"ClientId": "", "ClientSecret": ""}, "AllowedGitHubLogins": [CONSOLE_ADMIN]},
        }, indent=2) + "\n")
    return bool(json.loads(settings.read_text()).get("Identity", {}).get("GitHub", {}).get("ClientId"))


def up(console=False, dev=False):
    """Creates keys, pins the target's host key and starts the rig, plus the console or the IDE database."""
    for folder in (STATE, KEYS, SECRETS, INVENTORY / "ssh" / "rehearsal"):
        private_dir(folder)
    identity = INVENTORY / "ssh" / "rehearsal" / "identity"
    if not identity.exists():
        run("ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-C", "wheelhouse-rehearsal", "-f", str(identity))
    host_key = KEYS / "ssh_host_ed25519_key"
    if not host_key.exists():
        run("ssh-keygen", "-q", "-t", "ed25519", "-N", "", "-C", "rehearsal-target", "-f", str(host_key))
    (KEYS / "authorized_keys").write_text(Path(str(identity) + ".pub").read_text())
    # The host key is generated here, so the pin needs no trust-on-first-use scan. The console reaches the
    # same target as `target` inside the rig network.
    key = " ".join(Path(str(host_key) + ".pub").read_text().split()[:2])
    known = "[127.0.0.1]:2222 " + key + "\ntarget " + key + "\n"
    private_file(INVENTORY / "ssh" / "rehearsal" / "known_hosts", known)
    password = SECRETS / "database-password"
    if not password.exists():
        private_file(password, secrets.token_urlsafe(24))
    profiles = []
    if subprocess.run(["docker", "image", "inspect", VAULT_IMAGE], capture_output=True).returncode == 0:
        vault_files()
        profiles = ["--profile", "vault"]
    signin = console_files() if console else False
    if console:
        profiles += ["--profile", "console"]
    if dev:
        profiles += ["--profile", "dev"]
    run("docker", "compose", "-f", str(HERE / "compose.yml"), *profiles, "up", "-d", "--build", "--wait",
        env=environment(), capture=False)
    if console:
        # The console reads its settings once at start; a restart applies an edited OAuth app or allowlist.
        run("docker", "compose", "-f", str(HERE / "compose.yml"), *profiles, "restart", "console",
            env=environment(), capture=False)
    print("Rig ready: ssh deploy@127.0.0.1 -p 2222, registry " + REGISTRY
          + (", vault http://127.0.0.1:18201" if "vault" in profiles else ", no vault (build " + VAULT_IMAGE
                                                                       + " to add one)"))
    if console:
        print("Console: " + CONSOLE_URL + (" — sign in with GitHub" if signin else
              " — sign-in is off until " + str(CONSOLE / "appsettings.Local.json") + " holds a GitHub OAuth app"
              " (callback " + CONSOLE_URL + "/api/identity/callback)"))
    if dev:
        print("IDE: run Wheelhouse.Api with the `https` launch profile, then open https://localhost:8210"
              " (database 127.0.0.1:15432; sign-in uses the API project's user-secrets)")


def bundle(release, broken=False):
    """Pushes the local product images and imports a digest-pinned bundle for them."""
    images = {}
    for service in ("management", "redirect"):
        # A broken release runs the redirect image as management, so its smoke probe fails after replacement.
        local = "foreverpin-" + ("redirect" if broken else service) + ":local"
        remote = REGISTRY + "/foreverpin-" + service + ":" + release
        run("docker", "tag", local, remote)
        run("docker", "push", remote)
        digests = json.loads(run("docker", "image", "inspect", "--format", "{{json .RepoDigests}}", remote))
        images[service] = next(item for item in digests if item.startswith(REGISTRY + "/"))
    platform = "linux/" + run("docker", "info", "--format", "{{.Architecture}}").strip() \
        .replace("x86_64", "amd64").replace("aarch64", "arm64")
    commit = run("git", "-C", str(GENERATOR.parents[2]), "rev-parse", "HEAD").strip()
    output = private_dir(STATE / "build") / release
    if not output.exists():
        run(sys.executable, str(GENERATOR), "--management-image", images["management"],
            "--redirect-image", images["redirect"], "--release", release, "--commit", commit,
            "--platform", platform, "--output", str(output))
    archive = STATE / "build" / (release + ".tar.gz")
    with tarfile.open(archive, "w:gz") as package:
        for name in ("release.json", "compose.json"):
            payload = (output / name).read_bytes()
            member = tarfile.TarInfo(name)
            member.size = len(payload)
            package.addfile(member, io.BytesIO(payload))
    bundle_id = "foreverpin-" + release
    if not (INVENTORY / "bundles" / bundle_id).exists():
        transport("import", "--archive", str(archive), "--bundle", bundle_id)
    print("Imported " + bundle_id)
    return bundle_id


def settings(bundle_id):
    """Writes private settings files from the release contract with rehearsal-only values."""
    password = (SECRETS / "database-password").read_text().strip()
    values = {
        "DatabaseOptions": {"ConnectionString": "Host=rehearsal-database;Database=foreverpin;Username=foreverpin;Password=" + password},
        "ApiSettings": {"RedirectBaseUrl": "http://localhost:17123"},
        "Auth": {"Google": {"ClientId": "rehearsal.apps.googleusercontent.com"}},
        "Billing": {"SecretKey": "sk_test_rehearsal", "WebhookSecret": "whsec_rehearsal",
                    "Prices": {"Solo": "price_rehearsal_solo", "Pro": "price_rehearsal_pro", "Agency": "price_rehearsal_agency"},
                    "SuccessUrl": "http://localhost/billing/success", "CancelUrl": "http://localhost/billing/cancel"},
        "AllowedHosts": "localhost",
        "Deployment": {"TrustedProxies": ["127.0.0.1"]},
    }

    def fill(template, source):
        return {key: fill(value, source.get(key, {})) if isinstance(value, dict) else source.get(key, value)
                for key, value in template.items()}

    for service, template in transport("template", "--bundle", bundle_id).items():
        private_file(SECRETS / (service + ".json"), json.dumps(fill(template, values), indent=2) + "\n")
    print("Wrote settings for " + bundle_id)


def deploy(bundle_id, timeout=420):
    """Submits the bundle through the same transport the dashboard uses and waits for the outcome."""
    job = transport("submit", "--target", TARGET, "--bundle", bundle_id, "--actor", "rehearsal")
    print("Submitted " + job["id"])
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        time.sleep(5)
        outcome = transport("status", "--job", job["id"])
        if outcome.get("status") in TERMINAL:
            print(json.dumps({key: outcome.get(key) for key in ("status", "release", "reason", "failure")}))
            return outcome
    sys.exit("Timed out waiting for " + job["id"])


def down(volumes):
    vault = ["--profile", "vault", "--profile", "console", "--profile", "dev"]
    # The runner names the product project <product>-<environment>.
    run("docker", "compose", "-p", "foreverpin-rehearsal", "down", *(["--volumes"] if volumes else []),
        env=environment(), capture=False)
    run("docker", "compose", "-f", str(HERE / "compose.yml"), *vault, "down", *(["--volumes"] if volumes else []),
        env=environment(), capture=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["up", "console", "dev", "bundle", "settings", "check", "deploy", "state",
                                           "run", "down"])
    parser.add_argument("--release", default="rehearsal-1")
    parser.add_argument("--volumes", action="store_true")
    parser.add_argument("--broken", action="store_true", help="bundle: build a release that fails after replacement")
    args = parser.parse_args()
    bundle_id = "foreverpin-" + args.release
    if args.action == "up":
        up()
    elif args.action == "console":
        up(console=True)
    elif args.action == "dev":
        up(dev=True)
    elif args.action == "bundle":
        bundle(args.release, args.broken)
    elif args.action == "settings":
        settings(bundle_id)
    elif args.action == "check":
        print(json.dumps(transport("check", "--target", TARGET, "--bundle", bundle_id), indent=2))
    elif args.action == "deploy":
        deploy(bundle_id)
    elif args.action == "state":
        print(json.dumps(transport("state", "--target", TARGET), indent=2))
    elif args.action == "run":
        up()
        settings(bundle(args.release))
        print(json.dumps(transport("check", "--target", TARGET, "--bundle", bundle_id), indent=2))
        deploy(bundle_id)
    else:
        down(args.volumes)


if __name__ == "__main__":
    main()
