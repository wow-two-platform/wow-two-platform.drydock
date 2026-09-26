"""Reviewed fleet definitions. Adding a provider, VPS or binding requires a code change."""
from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from runner import SLUG, require


class VpsProvider(str, Enum):
    HETZNER = "Hetzner"


class DeploymentEnvironment(str, Enum):
    STAGING = "staging"
    PRODUCTION = "production"


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


# Populate these only with verified host details during the VPS wiring session.
# SSH identities live under <inventory>/ssh/<server-id>/; runtime secrets stay on the target.
SERVERS: tuple[Server, ...] = ()
TARGETS: tuple[Target, ...] = ()


def servers():
    require(len({server.id for server in SERVERS}) == len(SERVERS), "Duplicate server ID")
    result = []
    for server in SERVERS:
        require(SLUG.fullmatch(server.id) and isinstance(server.provider, VpsProvider), "Unsupported server")
        result.append({"id": server.id, "name": server.name, "provider": server.provider.value,
                       "host": server.host, "region": server.region, "sshUser": server.ssh_user})
    return result


def resolve_target(root, identifier):
    servers()
    require(len({target.id for target in TARGETS}) == len(TARGETS), "Duplicate target ID")
    target = next((item for item in TARGETS if item.id == identifier), None)
    require(target is not None and SLUG.fullmatch(target.id), "Target is not defined in code")
    require(isinstance(target.environment, DeploymentEnvironment), "Unsupported environment")
    server = next((item for item in SERVERS if item.id == target.server_id), None)
    require(server is not None, "Server is not defined in code")
    identity = Path(root) / "ssh" / server.id
    return {"serverId": server.id, "provider": server.provider.value,
            "ssh": {"host": server.host, "user": server.ssh_user, "port": server.ssh_port,
                    "keyFile": str(identity / "identity"), "knownHostsFile": str(identity / "known_hosts")},
            "target": {"product": target.product, "environment": target.environment.value,
                       "root": target.root, "settings": dict(target.settings),
                       "variables": {"PLATFORM_NETWORK": target.network}, "smoke": list(target.smoke)}}
