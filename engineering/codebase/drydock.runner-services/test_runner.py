import copy
import hashlib
import json
import os
from pathlib import Path
import tempfile
import tarfile
import unittest
from unittest.mock import patch
import runner
import transport


class FakeDocker:
    events = []
    fail = None

    def __init__(self, target, environment):
        pass

    def preflight(self, manifest):
        self.events.append("preflight")

    def pull(self, bundle):
        self.events.append("pull")
        if self.fail == "pull":
            raise RuntimeError("password=DO_NOT_LOG")

    def up(self, bundle):
        release = runner.read_json(Path(bundle) / "release.json")["release"]
        self.events.append("up:" + release)
        if self.fail == release:
            raise RuntimeError("password=DO_NOT_LOG")

    def verify(self, bundle, manifest):
        self.events.append("verify:" + manifest["release"])


class RunnerTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        settings = self.root / "settings.json"
        settings.write_text(json.dumps({"Database": {"Connection": "DO_NOT_LOG"}}))
        settings.chmod(0o600)
        self.target = {"product": "pilot", "environment": "test", "root": str(self.root / "state"),
                       "settings": {"api": str(settings)}, "minimumFreeBytes": 0}
        self.bundle = self.root / "bundle"
        self.bundle.mkdir()
        self.manifest = {"schemaVersion": 1, "product": "pilot", "release": "v1",
                         "sourceCommit": "a" * 40, "platform": "linux/amd64", "rollbackCompatible": False,
                         "images": {"api": "ghcr.io/owner/api@sha256:" + "a" * 64},
                         "requiredConfiguration": {"api": ["Database:Connection"]}}
        self.compose = {"services": {"api": {"image": self.manifest["images"]["api"], "platform": "linux/amd64",
                        "healthcheck": {"test": ["CMD", "curl", "-f", "http://localhost/health"]}}}}
        self.save()
        FakeDocker.events = []
        FakeDocker.fail = None

    def tearDown(self):
        self.temporary.cleanup()

    def save(self):
        compose = json.dumps(self.compose).encode()
        (self.bundle / "compose.json").write_bytes(compose)
        self.manifest["composeSha256"] = hashlib.sha256(compose).hexdigest()
        (self.bundle / "release.json").write_text(json.dumps(self.manifest))

    def apply(self):
        return runner.apply(self.bundle, self.target, "test-operator", docker_factory=FakeDocker)

    def test_success_saves_auditable_current_and_redacts_settings(self):
        result = self.apply()
        self.assertEqual("succeeded", result["status"])
        self.assertEqual(["preflight", "pull", "up:v1", "verify:v1"], FakeDocker.events)
        current = runner.read_json(self.root / "state/pilot-test/current.json")
        self.assertEqual(result["id"], current["id"])
        self.assertNotIn("DO_NOT_LOG", json.dumps(result))

    def test_hash_mismatch_fails_before_docker(self):
        (self.bundle / "compose.json").write_text("{}")
        with self.assertRaisesRegex(ValueError, "hash"):
            self.apply()
        self.assertEqual([], FakeDocker.events)

    def test_mutable_image_rejected(self):
        self.manifest["images"]["api"] = "ghcr.io/owner/api:latest"
        self.compose["services"]["api"]["image"] = self.manifest["images"]["api"]
        self.save()
        with self.assertRaisesRegex(ValueError, "immutable"):
            self.apply()

    def test_wrong_product_rejected(self):
        self.target["product"] = "other"
        with self.assertRaisesRegex(ValueError, "product mismatch"):
            self.apply()

    def test_settings_permissions_rejected(self):
        self.root.chmod(0o755)
        Path(self.target["settings"]["api"]).chmod(0o644)
        with self.assertRaisesRegex(ValueError, "private"):
            self.apply()

    def test_missing_configuration_rejected(self):
        self.manifest["requiredConfiguration"]["api"] = ["Missing"]
        self.save()
        with self.assertRaisesRegex(ValueError, "Missing required"):
            self.apply()

    def test_failure_before_mutation_keeps_previous(self):
        prior = self.apply()
        FakeDocker.fail = "pull"
        self.manifest["release"] = "v2"
        self.save()
        result = self.apply()
        self.assertEqual("failed", result["status"])
        self.assertEqual(prior["id"], runner.read_json(self.root / "state/pilot-test/current.json")["id"])
        self.assertNotIn("DO_NOT_LOG", json.dumps(result))
        self.assertEqual(1, FakeDocker.events.count("up:v1"))

    def test_compatible_failure_restores_previous_images(self):
        prior = self.apply()
        self.manifest.update(release="v2", rollbackCompatible=True)
        self.save()
        FakeDocker.fail = "v2"
        result = self.apply()
        self.assertEqual("rolled_back", result["status"])
        self.assertEqual(prior["id"], runner.read_json(self.root / "state/pilot-test/current.json")["id"])
        self.assertEqual(["up:v2", "pull", "up:v1", "verify:v1"], FakeDocker.events[-4:])

    def test_incompatible_failure_never_rolls_back(self):
        self.apply()
        self.manifest["release"] = "v2"
        self.save()
        FakeDocker.fail = "v2"
        self.assertEqual("failed", self.apply()["status"])
        self.assertEqual("up:v2", FakeDocker.events[-1])

    def test_interrupted_rollout_requires_explicit_acknowledgement(self):
        job = "00000000-0000-0000-0000-000000000001"
        runner.write_json(self.root / "state/pilot-test/active.json", {"id": job, "status": "running"})
        with self.assertRaisesRegex(ValueError, "reconciliation"):
            self.apply()
        with self.assertRaisesRegex(ValueError, "matching"):
            runner.acknowledge(self.target, "wrong")
        runner.acknowledge(self.target, job)
        self.assertEqual("succeeded", self.apply()["status"])

    def test_concurrent_lock_rejects_second_operator(self):
        with runner.deployment_lock(self.root / "state/pilot-test"):
            with self.assertRaisesRegex(ValueError, "Another deployment"):
                self.apply()

    def test_missing_health_check_rejected(self):
        self.compose["services"]["api"].pop("healthcheck")
        self.save()
        with self.assertRaisesRegex(ValueError, "health gate"):
            self.apply()

    def test_environment_excludes_ambient_compose_and_application_secrets(self):
        with patch.dict(os.environ, {"COMPOSE_FILE": "/tmp/evil", "DB_CONNECTION": "DO_NOT_LOG"}):
            environment = runner.environment_for(self.target, self.manifest)
        self.assertNotIn("COMPOSE_FILE", environment)
        self.assertNotIn("DB_CONNECTION", environment)

    def test_target_cannot_override_compose_controls(self):
        self.target["variables"] = {"COMPOSE_FILE": "/tmp/evil"}
        with self.assertRaisesRegex(ValueError, "Invalid target variable"):
            self.apply()

    def test_source_copy_is_validated_again(self):
        original = runner.validate_bundle
        calls = []
        def validate(path):
            calls.append(Path(path))
            return original(path)
        with patch.object(runner, "validate_bundle", side_effect=validate):
            self.apply()
        self.assertEqual(2, len(calls))
        self.assertNotEqual(calls[0], calls[1])

    def test_unrecovered_mutation_blocks_future_rollouts(self):
        self.apply()
        self.manifest["release"] = "v2"
        self.save()
        FakeDocker.fail = "v2"
        failed = self.apply()
        with self.assertRaisesRegex(ValueError, "reconciliation"):
            self.apply()
        runner.acknowledge(self.target, failed["id"])
        self.assertFalse((self.root / "state/pilot-test/current.json").exists())

    def test_rollback_failure_never_reports_recovered(self):
        self.apply()
        self.manifest.update(release="v2", rollbackCompatible=True)
        self.save()
        with patch.object(FakeDocker, "up", side_effect=RuntimeError("failure")):
            self.assertEqual("rollback_failed", self.apply()["status"])

    def test_cli_rollback_is_a_failed_requested_deployment(self):
        target = self.root / "target.json"
        runner.write_json(target, self.target)
        with patch("sys.argv", ["runner.py", "apply", "--target", str(target), "--bundle", str(self.bundle)]):
            with patch.object(runner, "apply", return_value={"status": "rolled_back"}):
                self.assertEqual(1, runner.main())

    def test_archive_import_validates_and_never_overwrites_a_release(self):
        archive = self.root / "release.tar.gz"
        with tarfile.open(archive, "w:gz") as package:
            for name in ("release.json", "compose.json"):
                package.add(self.bundle / name, arcname=name)
        inventory = self.root / "inventory"
        result = transport.import_bundle(inventory, archive, "pilot-v1")
        self.assertEqual("pilot-v1", result["id"])
        self.assertEqual("v1", transport.releases(inventory)[0]["release"])
        with self.assertRaisesRegex(ValueError, "already exists"):
            transport.import_bundle(inventory, archive, "pilot-v1")

    def test_archive_import_rejects_links(self):
        archive = self.root / "release.tar.gz"
        with tarfile.open(archive, "w:gz") as package:
            member = tarfile.TarInfo("release.json")
            member.type = tarfile.SYMTYPE
            member.linkname = "/etc/passwd"
            package.addfile(member)
            package.add(self.bundle / "compose.json", arcname="compose.json")
        with self.assertRaisesRegex(ValueError, "Invalid archive"):
            transport.import_bundle(self.root / "inventory", archive, "pilot-v1")


if __name__ == "__main__":
    unittest.main()
