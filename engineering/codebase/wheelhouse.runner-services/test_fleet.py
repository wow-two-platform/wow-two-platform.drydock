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

    def test_rehearsal_target_exists_only_behind_its_switch(self):
        with patch.dict('os.environ', {}, clear=False):
            import os
            os.environ.pop('WHEELHOUSE_REHEARSAL', None)
            self.assertNotIn('foreverpin-rehearsal', [target.id for target in fleet.active_targets()])
            with self.assertRaisesRegex(ValueError, 'not defined in code'):
                fleet.resolve_target(Path('/data/deployments'), 'foreverpin-rehearsal')
        with patch.dict('os.environ', {'WHEELHOUSE_REHEARSAL': '1'}):
            config = fleet.resolve_target(Path('/data/deployments'), 'foreverpin-rehearsal')
            self.assertEqual(('Local', 2222, 'rehearsal'),
                             (config['provider'], config['ssh']['port'], config['target']['environment']))

    def test_console_inside_the_rig_reaches_services_by_name_with_host_settings_paths(self):
        import importlib
        try:
            with patch.dict('os.environ', {'WHEELHOUSE_REHEARSAL': 'network', 'REHEARSAL_STATE': '/host/state'}):
                importlib.reload(fleet)
                config = fleet.resolve_target(Path('/data/deployments'), 'foreverpin-rehearsal')
                self.assertEqual(('target', 22), (config['ssh']['host'], config['ssh']['port']))
                self.assertEqual('http://vault:8080', fleet.vaults()[0]['url'])
                self.assertEqual('/host/state/secrets/management.json', config['target']['settings']['management'])
        finally:
            importlib.reload(fleet)  # later tests expect the host view

    def test_vaults_come_only_from_code_and_keep_urls_server_side(self):
        server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        vault = fleet.Vault('pilot-vault', 'Pilot vault', 'pilot', 'http://secrets-vault:8080')
        with patch.object(fleet, 'SERVERS', (server,)), patch.object(fleet, 'VAULTS', (vault,)):
            self.assertEqual([{'id': 'pilot-vault', 'name': 'Pilot vault', 'serverId': 'pilot',
                               'url': 'http://secrets-vault:8080'}], fleet.vaults())
        for url in ('http://vault:8080/path', 'file:///etc/passwd', 'http://vault:8080?x=1'):
            broken = fleet.Vault('pilot-vault', 'Pilot vault', 'pilot', url)
            with patch.object(fleet, 'SERVERS', (server,)), patch.object(fleet, 'VAULTS', (broken,)):
                with self.assertRaisesRegex(ValueError, 'Unsupported vault'):
                    fleet.vaults()

    def test_vault_needs_a_host_defined_in_code(self):
        orphan = fleet.Vault('pilot-vault', 'Pilot vault', 'missing', 'http://secrets-vault:8080')
        with patch.object(fleet, 'SERVERS', ()), patch.object(fleet, 'VAULTS', (orphan,)):
            with self.assertRaisesRegex(ValueError, 'not defined in code'):
                fleet.vaults()

    def test_duplicate_server_ids_are_rejected(self):
        server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        with patch.object(fleet, 'SERVERS', (server, server)):
            with self.assertRaisesRegex(ValueError, 'Duplicate server'):
                fleet.servers()
