import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import transport


class TransportTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        for name in ("key", "known_hosts"):
            (self.root / name).touch()
        self.config = {"host": "vps.example.net", "user": "deploy", "port": 2222,
                       "keyFile": str(self.root / "key"), "knownHostsFile": str(self.root / "known_hosts")}

    def tearDown(self):
        self.temporary.cleanup()

    def test_pinned_host_identity_is_required(self):
        options = transport.Ssh(self.config).options()
        self.assertIn("StrictHostKeyChecking=yes", options)
        self.assertIn("BatchMode=yes", options)
        self.assertIn("IdentitiesOnly=yes", options)
        self.assertIn("UserKnownHostsFile=" + self.config["knownHostsFile"], options)

    def test_host_cannot_inject_an_ssh_option(self):
        self.config["host"] = "-oProxyCommand=evil"
        with self.assertRaises(ValueError):
            transport.Ssh(self.config)

    def test_missing_identity_fails_closed(self):
        self.config["knownHostsFile"] = "/nonexistent/known-hosts"
        with self.assertRaises(ValueError):
            transport.Ssh(self.config)

    def test_inventory_ids_cannot_escape_root(self):
        with self.assertRaises(ValueError):
            transport.child(self.root, "bundles", "../outside")

    def test_inventory_symlink_cannot_escape_root(self):
        (self.root / "bundles").mkdir()
        (self.root / "bundles/escape").symlink_to("/tmp")
        with self.assertRaises(ValueError):
            transport.child(self.root, "bundles", "escape")

    def test_missing_remote_receipt_is_unknown_not_success(self):
        job = "00000000-0000-0000-0000-000000000001"
        transport.write_json(self.root / "jobs" / (job + ".json"),
                             {"id": job, "targetId": "pilot", "status": "submitting"})
        self.assertEqual("unknown", transport.status(self.root, job)["status"])

    def test_target_file_symlink_cannot_escape_root(self):
        (self.root / "targets").mkdir()
        (self.root / "targets/escape.json").symlink_to("/tmp/outside.json")
        with self.assertRaises(ValueError):
            transport.child(self.root, "targets", "escape", ".json")


if __name__ == "__main__":
    unittest.main()
