import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import artifacts
import catalog
import fleet
import transport


class CatalogTests(unittest.TestCase):
    def test_products_refuse_a_duplicate_slug_or_an_invalid_repository(self):
        product = catalog.Product('pilot', 'Pilot', 'A pilot product.', 'owner/pilot')
        with patch.object(catalog, 'PRODUCTS', (product, product)):
            with self.assertRaisesRegex(ValueError, 'Duplicate product slug'):
                catalog.products()
        with patch.object(catalog, 'PRODUCTS', (catalog.Product('pilot', 'Pilot', 'A pilot.', 'not a repository'),)):
            with self.assertRaisesRegex(ValueError, 'Invalid product repository'):
                catalog.products()

    def test_every_target_names_a_catalog_product(self):
        with patch.dict(os.environ, {'WHEELHOUSE_REHEARSAL': '1'}):
            slugs = {product.slug for product in catalog.products()}
            for binding in fleet.active_targets():
                self.assertIn(binding.product, slugs, binding.id)

    def test_a_target_for_an_unknown_product_is_refused(self):
        server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        target = fleet.Target('ghost-dev', 'pilot', 'ghost', fleet.DeploymentEnvironment.DEV, (), 'platform')
        with patch.object(fleet, 'SERVERS', (server,)), patch.object(fleet, 'TARGETS', (target,)):
            with self.assertRaisesRegex(ValueError, 'Product is not defined in code'):
                fleet.resolve_target(Path('/data/deployments'), 'ghost-dev')

    def test_release_sources_come_from_catalog_products_that_publish(self):
        publishing = [product for product in catalog.products() if product.release is not None]
        self.assertEqual([product.slug for product in publishing], [source.product for source in artifacts.SOURCES])
        for product, source in zip(publishing, artifacts.SOURCES):
            self.assertEqual((product.repository, product.release.asset, product.release.images),
                             (source.repository, source.asset_name, source.images))


class ProductsResourceTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)
        (self.root / 'jobs').mkdir()
        (self.root / 'observed').mkdir()
        self.server = fleet.Server('pilot', 'Pilot', fleet.VpsProvider.HETZNER, 'vps.example.net', 'hel1')
        self.vault = fleet.Vault('pilot-vault', 'Pilot vault', 'pilot', 'http://vault:8080')
        self.targets = tuple(fleet.Target('foreverpin-' + environment.value, 'pilot', 'foreverpin', environment, (),
                                          'platform') for environment in reversed(fleet.DeploymentEnvironment))

    def tearDown(self):
        self.directory.cleanup()

    def record(self, job_id, target_id, status, submitted, sites):
        (self.root / 'jobs' / (job_id + '.json')).write_text(json.dumps(
            {'id': job_id, 'targetId': target_id, 'status': 'submitted', 'submittedAt': submitted}))
        (self.root / 'observed' / (job_id + '.json')).write_text(json.dumps({'status': status, 'sites': sites}))

    def read(self):
        with patch.object(fleet, 'SERVERS', (self.server,)), patch.object(fleet, 'TARGETS', self.targets), \
                patch.object(fleet, 'VAULTS', (self.vault,)), patch.dict(os.environ, {'WHEELHOUSE_REHEARSAL': ''}):
            return next(item for item in transport.products(self.root) if item['slug'] == 'foreverpin')

    def test_environments_follow_dev_test_prod_with_one_namespace_each(self):
        product = self.read()
        self.assertEqual(['dev', 'test', 'prod'], [environment['name'] for environment in product['environments']])
        self.assertEqual({'vaultId': 'pilot-vault', 'namespace': 'foreverpin-prod'}, product['environments'][2]['secrets'])
        self.assertEqual('sulton-max/10x-venture-forever-pin', product['repository'])
        self.assertTrue(product['hasReleaseSource'])

    def test_sites_come_from_the_newest_succeeded_rollout_and_drop_unsafe_addresses(self):
        self.record('00000000-0000-0000-0000-000000000001', 'foreverpin-dev', 'succeeded', '2026-09-28T00:00:00+00:00',
                    [{'name': 'app', 'url': 'http://app.old.example', 'exposure': 'public'}])
        self.record('00000000-0000-0000-0000-000000000002', 'foreverpin-dev', 'succeeded', '2026-09-29T00:00:00+00:00',
                    [{'name': 'app', 'url': 'https://app.example.com', 'exposure': 'public'},
                     {'name': 'admin', 'url': 'https://admin.example.com/ops', 'exposure': 'private'},
                     {'name': 'bad', 'url': 'javascript:alert(1)'}])
        self.record('00000000-0000-0000-0000-000000000003', 'foreverpin-dev', 'failed', '2026-09-30T00:00:00+00:00',
                    [{'name': 'app', 'url': 'https://broken.example.com'}])
        dev = self.read()['environments'][0]
        self.assertEqual([{'name': 'app', 'url': 'https://app.example.com', 'exposure': 'public'},
                          {'name': 'admin', 'url': 'https://admin.example.com/ops', 'exposure': 'private'}],
                         dev['sites'])

    def test_an_environment_without_a_rollout_or_a_vault_reports_neither(self):
        with patch.object(fleet, 'SERVERS', (self.server,)), patch.object(fleet, 'TARGETS', self.targets), \
                patch.object(fleet, 'VAULTS', ()), patch.dict(os.environ, {'WHEELHOUSE_REHEARSAL': ''}):
            product = next(item for item in transport.products(self.root) if item['slug'] == 'foreverpin')
        self.assertEqual([], product['environments'][0]['sites'])
        self.assertIsNone(product['environments'][0]['secrets'])


if __name__ == '__main__':
    unittest.main()
