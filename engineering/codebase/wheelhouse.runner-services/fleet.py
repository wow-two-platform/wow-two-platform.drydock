"""Reviewed fleet definitions. Adding a provider, VPS or binding requires a code change."""
from dataclasses import dataclass
from enum import Enum
import os
from pathlib import Path
import re
from runner import SLUG, require

VAULT_URL = re.compile(r"https?://[A-Za-z0-9.-]+(:[0-9]{1,5})?")


class VpsProvider(str, Enum):
    HETZNER = "Hetzner"
    LOCAL = "Local"


class DeploymentEnvironment(str, Enum):
    STAGING = "staging"
    PRODUCTION = "production"
    REHEARSAL = "rehearsal"


@dataclass(frozen=True)
class Server:
    id: str
    name: str
    provider: VpsProvider
    host: str
    region: str
    ssh_user: str = "deploy"
    ssh_port: int = 22


@dataclass(frozen=True)
class Target:
    id: str
    server_id: str
    product: str
    environment: DeploymentEnvironment
    settings: tuple[tuple[str, str], ...]
    network: str
    root: str = "/srv/wheelhouse"
    smoke: tuple[dict, ...] = ()


@dataclass(frozen=True)
class Vault:
    id: str
    name: str
    server_id: str
    # The private management endpoint Wheelhouse reaches; the browser never sees or chooses it.
    url: str


# Populate these only with verified host details during the VPS wiring session.
# SSH identities live under <inventory>/ssh/<server-id>/; runtime secrets stay on the target.
SERVERS: tuple[Server, ...] = ()
TARGETS: tuple[Target, ...] = ()
# Administrator credentials live under <inventory>/vaults/<vault-id>/password.
VAULTS: tuple[Vault, ...] = ()

# The local SSH target from engineering/deployment/rehearsal. Its settings paths are identical inside the
# target container and on the Docker host, so the daemon resolves the same bind-mount sources. The console
# container runs from /app, so the rig passes the host path in REHEARSAL_STATE.
REHEARSAL_STATE = Path(os.environ.get("REHEARSAL_STATE")
                       or Path(__file__).resolve().parents[2] / "deployment" / "rehearsal" / "state")
# `network`: Wheelhouse runs inside the rig and reaches the target and vault by service name, as it would a VPS.
IN_RIG = os.environ.get("WHEELHOUSE_REHEARSAL") == "network"
REHEARSAL_SERVERS = (Server("rehearsal", "Local rehearsal target", VpsProvider.LOCAL,
                            "target" if IN_RIG else "127.0.0.1", "local", ssh_port=22 if IN_RIG else 2222),)
REHEARSAL_TARGETS = (Target("foreverpin-rehearsal", "rehearsal", "foreverpin", DeploymentEnvironment.REHEARSAL,
                            (("management", str(REHEARSAL_STATE / "secrets" / "management.json")),
                             ("redirect", str(REHEARSAL_STATE / "secrets" / "redirect.json"))),
                            "wheelhouse-rehearsal",
                            smoke=({"service": "management", "path": "/api/runtime-config", "status": 200},
                                   {"service": "redirect", "path": "/health", "status": 200})),)
REHEARSAL_VAULTS = (Vault("rehearsal-vault", "Rehearsal vault", "rehearsal",
                          "http://vault:8080" if IN_RIG else "http://127.0.0.1:18201"),)


def rehearsal():
    # An explicit operator switch for local rehearsal; never set in a deployed control plane.
    return os.environ.get("WHEELHOUSE_REHEARSAL") in ("1", "network")


def active_servers():
    return SERVERS + (REHEARSAL_SERVERS if rehearsal() else ())


def active_targets():
    return TARGETS + (REHEARSAL_TARGETS if rehearsal() else ())


def active_vaults():
    return VAULTS + (REHEARSAL_VAULTS if rehearsal() else ())


def vaults():
    catalog = active_vaults()
    servers()
    require(len({vault.id for vault in catalog}) == len(catalog), "Duplicate vault ID")
    result = []
    for vault in catalog:
        require(SLUG.fullmatch(vault.id) and VAULT_URL.fullmatch(vault.url), "Unsupported vault")
        require(any(server.id == vault.server_id for server in active_servers()), "Server is not defined in code")
        result.append({"id": vault.id, "name": vault.name, "serverId": vault.server_id, "url": vault.url})
    return result


def servers():
    catalog = active_servers()
    require(len({server.id for server in catalog}) == len(catalog), "Duplicate server ID")
    result = []
    for server in catalog:
        require(SLUG.fullmatch(server.id) and isinstance(server.provider, VpsProvider), "Unsupported server")
        result.append({"id": server.id, "name": server.name, "provider": server.provider.value,
                       "host": server.host, "region": server.region, "sshUser": server.ssh_user})
    return result


def resolve_target(root, identifier):
    servers()
    catalog = active_targets()
    require(len({target.id for target in catalog}) == len(catalog), "Duplicate target ID")
    target = next((item for item in catalog if item.id == identifier), None)
    require(target is not None and SLUG.fullmatch(target.id), "Target is not defined in code")
    require(isinstance(target.environment, DeploymentEnvironment), "Unsupported environment")
    server = next((item for item in active_servers() if item.id == target.server_id), None)
    require(server is not None, "Server is not defined in code")
    identity = Path(root) / "ssh" / server.id
    return {"serverId": server.id, "provider": server.provider.value,
            "ssh": {"host": server.host, "user": server.ssh_user, "port": server.ssh_port,
                    "keyFile": str(identity / "identity"), "knownHostsFile": str(identity / "known_hosts")},
            "target": {"product": target.product, "environment": target.environment.value,
                       "root": target.root, "settings": dict(target.settings),
                       "variables": {"PLATFORM_NETWORK": target.network}, "smoke": list(target.smoke)}}
