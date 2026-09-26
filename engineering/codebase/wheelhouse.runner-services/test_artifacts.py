import hashlib
import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch
from urllib.error import HTTPError
from urllib.request import Request
import artifacts
import transport


class ArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = artifacts.SOURCES[0]
        images = {service: image + '@sha256:' + 'a' * 64 for service, image in self.source.images}
        compose = {'services': {name: {'image': image, 'platform': 'linux/amd64',
                    'healthcheck': {'test': ['CMD', 'true']}} for name, image in images.items()}}
        self.compose_bytes = json.dumps(compose).encode()
        self.manifest = {'schemaVersion': 1, 'product': 'foreverpin', 'release': 'v0.9.0',
                         'sourceCommit': 'b' * 40, 'platform': 'linux/amd64', 'rollbackCompatible': False,
                         'composeSha256': hashlib.sha256(self.compose_bytes).hexdigest(),
                         'images': images, 'requiredConfiguration': {name: [] for name in images}}
        self.release = {'tag_name': 'v0.9.0', 'draft': False, 'published_at': '2026-09-19T00:00:00Z',
                        'assets': [{'name': self.source.asset_name, 'id': 123, 'state': 'uploaded',
                                    'digest': 'sha256:' + 'a' * 64, 'size': 500}]}
        self.pack()

    def tearDown(self):
        self.temp.cleanup()

    def pack(self):
        data = io.BytesIO()
        with tarfile.open(fileobj=data, mode='w:gz') as package:
            for name, payload in [('release.json', json.dumps(self.manifest).encode()), ('compose.json', self.compose_bytes)]:
                member = tarfile.TarInfo(name)
                member.size = len(payload)
                package.addfile(member, io.BytesIO(payload))
        self.archive = data.getvalue()
        self.release['assets'][0]['digest'] = 'sha256:' + hashlib.sha256(self.archive).hexdigest()

    def fetch(self, url):
        if '/releases?' in url:
            return json.dumps([self.release]).encode()
        if '/commits/' in url:
            return json.dumps({'sha': 'b' * 40}).encode()
        return self.archive

    def test_catalog_hides_drafts_and_incomplete_uploads(self):
        for change in ({'draft': True}, {'assets': []}, {'published_at': None}, {'tag_name': 'main'}):
            with self.subTest(change=change):
                release = {**self.release, **change}
                with patch.object(artifacts, 'fetch', return_value=json.dumps([release]).encode()):
                    self.assertEqual([], artifacts.available())
        self.release['assets'][0]['state'] = 'new'
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            self.assertEqual([], artifacts.available())

    def test_publication_is_listed_without_build_status_queries(self):
        with patch.object(artifacts, 'fetch', side_effect=self.fetch) as fetch:
            result = artifacts.available()
            self.assertEqual('foreverpin-gh-123', result[0]['id'])
            self.assertEqual(1, fetch.call_count)
            self.assertNotIn('actions', fetch.call_args.args[0])

    def test_selected_bundle_is_checked_and_cached(self):
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            bundle = artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)
            self.assertTrue((bundle / 'source.json').is_file())
            self.assertEqual(bundle, artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle))

    def test_changed_archive_never_enters_the_cache(self):
        self.release['assets'][0]['digest'] = 'sha256:' + '0' * 64
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            with self.assertRaisesRegex(ValueError, 'asset changed'):
                artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)
        self.assertFalse((self.root / 'bundles/foreverpin-gh-123').exists())

    def test_source_commit_must_match_the_tag(self):
        self.manifest['sourceCommit'] = 'c' * 40
        self.pack()
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            with self.assertRaisesRegex(ValueError, 'source commit mismatch'):
                artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)

    def test_removed_release_cannot_be_submitted_from_cache(self):
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)
            self.release['draft'] = True
            with self.assertRaisesRegex(ValueError, 'no longer available'):
                artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)

    def test_wrong_service_registry_is_rejected(self):
        self.manifest['images']['management'] = 'ghcr.io/other/api@sha256:' + 'a' * 64
        compose = json.loads(self.compose_bytes)
        compose['services']['management']['image'] = self.manifest['images']['management']
        self.compose_bytes = json.dumps(compose).encode()
        self.manifest['composeSha256'] = hashlib.sha256(self.compose_bytes).hexdigest()
        self.pack()
        with patch.object(artifacts, 'fetch', side_effect=self.fetch):
            with self.assertRaisesRegex(ValueError, 'approved repository'):
                artifacts.prepare(self.root, 'foreverpin-gh-123', transport.import_bundle)

    def test_imported_bundles_deploy_only_in_rehearsal(self):
        archive = self.root / 'release.tar.gz'
        archive.write_bytes(self.archive)
        transport.import_bundle(self.root, archive, 'foreverpin-local')
        offline = artifacts.CommandFailed('GitHub request failed (network)')
        with patch.object(artifacts, 'fetch', side_effect=offline):
            with patch.dict('os.environ', {'WHEELHOUSE_REHEARSAL': '1'}):
                listed = artifacts.available(self.root)
                self.assertEqual([('foreverpin-local', 'LocalImport')], [(item['id'], item['provider']) for item in listed])
                self.assertEqual(self.root / 'bundles/foreverpin-local',
                                 artifacts.prepare(self.root, 'foreverpin-local', transport.import_bundle))
            with patch.dict('os.environ', {'WHEELHOUSE_REHEARSAL': '0'}):
                with self.assertRaises(artifacts.CommandFailed):
                    artifacts.available(self.root)

    def test_github_errors_report_only_the_status(self):
        error = HTTPError('https://api.github.com/repos', 403, 'rate limit exceeded', {}, io.BytesIO(b'PRIVATE'))
        with patch.object(artifacts, 'build_opener') as opener:
            opener.return_value.open.side_effect = error
            with self.assertRaisesRegex(artifacts.CommandFailed, r'^GitHub request failed \(HTTP 403\)$'):
                artifacts.fetch('https://api.github.com/repos/owner/repo/releases')

    def test_api_token_cannot_follow_a_cdn_redirect(self):
        request = Request('https://api.github.com/asset', headers={'Authorization': 'Bearer PRIVATE'})
        redirected = artifacts.ApiRedirect().redirect_request(request, None, 302, 'Found', {},
                                                               'https://release-assets.githubusercontent.com/asset')
        self.assertFalse(redirected.has_header('Authorization'))
        with self.assertRaisesRegex(ValueError, 'Insecure'):
            artifacts.ApiRedirect().redirect_request(request, None, 302, 'Found', {}, 'http://example.net/asset')
