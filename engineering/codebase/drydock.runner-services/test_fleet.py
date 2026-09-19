import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import fleet
import transport


class FleetTests(unittest.TestCase):
    def test_external_json_cannot_register_a_host(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'targets').mkdir()
            (root / 'targets/injected.json').write_text(json.dumps({'ssh': {'host': 'unreviewed'}}))
            with patch.object(fleet, 'SERVERS', ()), patch.object(fleet, 'TARGETS', ()):
                self.assertEqual([], transport.targets(root))
                with self.assertRaisesRegex(ValueError, 'not defined in code'):
                    fleet.resolve_target(root, 'injected')

    def test_provider_and_environment_require_supported_enums(self):
        server = fleet.Server('pilot', 'Pilot', 'CustomProvider', 'vps.example.net', 'hel1')
        with patch.object(fleet, 'SERVERS', (server,)):
            with self.assertRaisesRegex(ValueError, 'Unsupported server'):
                fleet.servers()

    def test_code_binding_selects_host_and_secret_references(self):
        server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        target = fleet.Target('foreverpin-staging', 'pilot', 'foreverpin', fleet.DeploymentEnvironment.STAGING,
                              (('management', '/srv/secrets/management.json'), ('redirect', '/srv/secrets/redirect.json')),
                              'platform')
        with patch.object(fleet, 'SERVERS', (server,)), patch.object(fleet, 'TARGETS', (target,)):
            config = fleet.resolve_target(Path('/data/deployments'), target.id)
            self.assertEqual('Hetzner', config['provider'])
            self.assertEqual('vps.example.net', config['ssh']['host'])
            self.assertEqual('/data/deployments/ssh/pilot/identity', config['ssh']['keyFile'])
            self.assertEqual('staging', config['target']['environment'])
            self.assertEqual('platform', config['target']['variables']['PLATFORM_NETWORK'])

    def test_duplicate_server_ids_are_rejected(self):
        server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        with patch.object(fleet, 'SERVERS', (server, server)):
            with self.assertRaisesRegex(ValueError, 'Duplicate server'):
                fleet.servers()
