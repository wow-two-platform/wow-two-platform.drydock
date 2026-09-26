import base64
import contextlib
import hashlib
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import transport


class FakePool:
    """Runs the parallel collection in order so a test can script each target."""

    def __init__(self, max_workers):
        pass

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        return False

    def map(self, function, items):
        return [function(item) for item in items]


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

    def test_status_retains_the_submission_host_after_fleet_edits(self):
        job = "00000000-0000-0000-0000-000000000001"
        remote_job = "00000000-0000-0000-0000-000000000002"
        transport.write_json(self.root / "jobs" / (job + ".json"),
                             {"id": job, "targetId": "removed", "remoteJobId": remote_job,
                              "remote": "/srv/wheelhouse/incoming/pilot", "ssh": self.config})
        with patch.object(transport, "Ssh") as ssh:
            ssh.return_value.run.return_value = json.dumps({"id": remote_job, "status": "succeeded"})
            result = transport.status(self.root, job)
            self.assertEqual("succeeded", result["status"])
            ssh.assert_called_once_with(self.config)

    def failed(self, stderr, code=255):
        return subprocess.CompletedProcess([], code, stdout="", stderr=stderr)

    def test_ssh_failure_names_known_client_errors(self):
        error = transport.ssh_failure("SSH operation", self.failed("Host key verification failed.\r\n"))
        self.assertIsInstance(error, transport.CommandFailed)
        self.assertTrue(str(error).startswith("SSH host key verification failed (exit 255)"))

    def test_ssh_failure_relays_the_remote_runner_reason(self):
        remote = json.dumps({"status": "rejected", "failure": "Rejected", "reason": "Missing required setting: api:Key"})
        error = transport.ssh_failure("SSH operation", self.failed("banner\n" + remote + "\n", 1))
        self.assertIsInstance(error, transport.Rejected)
        self.assertEqual("Missing required setting: api:Key", str(error))

    def test_ssh_failure_never_echoes_unknown_output(self):
        error = transport.ssh_failure("SSH transfer", self.failed("password=DO_NOT_LOG", 1))
        self.assertNotIn("DO_NOT_LOG", str(error))
        self.assertTrue(str(error).startswith("SSH transfer failed (exit 1)"))

    def write_bundle(self, required):
        image = "ghcr.io/owner/api@sha256:" + "a" * 64
        compose = json.dumps({"services": {"api": {"image": image, "platform": "linux/amd64",
                              "healthcheck": {"test": ["CMD", "true"]}}}}).encode()
        bundle = self.root / "bundles/pilot-v1"
        bundle.mkdir(parents=True)
        (bundle / "compose.json").write_bytes(compose)
        transport.write_json(bundle / "release.json", {
            "schemaVersion": 1, "product": "pilot", "release": "v1", "sourceCommit": "a" * 40,
            "platform": "linux/amd64", "rollbackCompatible": False, "composeSha256": hashlib.sha256(compose).hexdigest(),
            "images": {"api": image}, "requiredConfiguration": {"api": required}})

    def test_template_nests_every_required_setting(self):
        self.write_bundle(["AllowedHosts", "Billing:Prices:Solo", "Billing:SecretKey", "Deployment:TrustedProxies"])
        expected = {"AllowedHosts": "", "Billing": {"Prices": {"Solo": ""}, "SecretKey": ""},
                    "Deployment": {"TrustedProxies": []}}
        self.assertEqual({"api": expected}, transport.template(self.root, "pilot-v1"))
        self.assertEqual(expected, transport.template(self.root, "pilot-v1", "api"))

    def test_template_rejects_a_key_that_is_also_a_section(self):
        self.write_bundle(["Billing", "Billing:SecretKey"])
        with self.assertRaisesRegex(transport.Rejected, "Conflicting"):
            transport.template(self.root, "pilot-v1")

    def test_jobs_merge_observed_outcomes_newest_first_without_ssh_details(self):
        first, second = "00000000-0000-0000-0000-00000000000a", "00000000-0000-0000-0000-00000000000b"
        for job, submitted in ((first, "2026-09-24T10:00:00+00:00"), (second, "2026-09-25T10:00:00+00:00")):
            transport.write_json(self.root / "jobs" / (job + ".json"),
                                 {"id": job, "targetId": "pilot", "bundleId": "pilot-v1", "release": "v1",
                                  "actor": "max", "status": "queued", "submittedAt": submitted,
                                  "ssh": self.config, "remote": "/srv/wheelhouse/incoming/x", "remoteJobId": job})
        transport.write_json(self.root / "observed" / (first + ".json"),
                             {"id": first, "status": "failed", "reason": "compose up failed (exit 1)",
                              "mutationStarted": True})
        (self.root / "jobs" / "not-a-job.json").write_text("{}")
        result = transport.jobs(self.root)
        self.assertEqual([second, first], [item["id"] for item in result])
        self.assertEqual(("failed", "compose up failed (exit 1)"), (result[1]["status"], result[1]["reason"]))
        self.assertNotIn("ssh", json.dumps(result))
        self.assertNotIn("/srv/wheelhouse", json.dumps(result))

    def test_remote_runner_streams_over_stdin_and_leaves_no_files(self):
        target = {"product": "pilot", "environment": "staging", "root": "/srv/wheelhouse"}
        with patch.object(transport.Ssh, "execute", return_value='{"condition": "ready"}') as execute:
            result = transport.run_remote(transport.Ssh(self.config), "state", target)
        arguments, step, stdin, timeout = execute.call_args.args
        command = arguments[-1].split()
        self.assertEqual({"condition": "ready"}, result)
        self.assertEqual(["python3", "-", "state", "--target-json"], command[:4])
        self.assertEqual(target, json.loads(base64.b64decode(command[4])))
        self.assertEqual(transport.RUNNER.read_text(), stdin)

    def test_check_reports_a_missing_identity_without_connecting(self):
        config = {"serverId": "pilot", "provider": "Hetzner", "ssh": dict(self.config, keyFile="/missing/key"),
                  "target": {"product": "pilot", "environment": "staging", "root": "/srv/wheelhouse"}}
        with patch.object(transport.fleet, "resolve_target", return_value=config), \
                patch.object(transport.subprocess, "run") as run:
            result = transport.check(self.root, "pilot-staging")
        self.assertFalse(result["ok"])
        self.assertEqual("SSH identity", result["checks"][0]["name"])
        run.assert_not_called()

    def test_check_reports_an_unreachable_host(self):
        config = {"serverId": "pilot", "provider": "Hetzner", "ssh": self.config,
                  "target": {"product": "pilot", "environment": "staging", "root": "/srv/wheelhouse"}}
        with patch.object(transport.fleet, "resolve_target", return_value=config), \
                patch.object(transport.subprocess, "run", return_value=self.failed("ssh: connect to host vps port 22: Connection refused")):
            result = transport.check(self.root, "pilot-staging")
        self.assertEqual(["SSH identity", "SSH connection"], [item["name"] for item in result["checks"]])
        self.assertTrue(result["checks"][1]["detail"].startswith("SSH connection refused"))

    def test_reconcile_records_the_operator_decision(self):
        config = {"serverId": "pilot", "provider": "Hetzner", "ssh": self.config,
                  "target": {"product": "pilot", "environment": "staging", "root": "/srv/wheelhouse"}}
        job = "00000000-0000-0000-0000-00000000000c"
        acknowledged = {"id": job, "status": "interrupted", "release": "v2", "sourceCommit": "a" * 40, "actor": "ci"}
        with patch.object(transport.fleet, "resolve_target", return_value=config), \
                patch.object(transport, "run_remote", return_value=acknowledged) as remote:
            result = transport.reconcile(self.root, "pilot-staging", job, "max")
        self.assertEqual(("--job", job), remote.call_args.args[3:])
        self.assertEqual(("interrupted", "max"), (result["status"], result["reconciledBy"]))
        self.assertEqual(result, transport.read_json(self.root / "reconciled" / (job + ".json")))
        with self.assertRaises(ValueError):
            transport.reconcile(self.root, "pilot-staging", "../escape", "max")

    def job(self, identifier, submitted, status, started=None, completed=None, target="pilot"):
        job = "00000000-0000-0000-0000-" + identifier.rjust(12, "0")
        transport.write_json(self.root / "jobs" / (job + ".json"),
                             {"id": job, "targetId": target, "release": "v" + identifier, "status": "queued",
                              "submittedAt": submitted, "remoteJobId": job})
        observed = {"id": job, "status": status}
        observed.update({key: value for key, value in (("startedAt", started), ("completedAt", completed)) if value})
        transport.write_json(self.root / "observed" / (job + ".json"), observed)

    def test_stats_count_outcomes_rollout_and_recovery_per_day(self):
        self.job("1", "2026-09-20T09:00:00+00:00", "succeeded", "2026-09-20T09:00:10+00:00", "2026-09-20T09:01:10+00:00")
        self.job("2", "2026-09-24T09:00:00+00:00", "failed", "2026-09-24T09:00:00+00:00", "2026-09-24T09:02:00+00:00")
        self.job("3", "2026-09-24T10:00:00+00:00", "rejected")
        self.job("4", "2026-09-25T09:00:00+00:00", "succeeded", "2026-09-25T09:00:00+00:00", "2026-09-25T09:03:00+00:00")
        self.job("5", "2026-09-26T08:00:00+00:00", "running", "2026-09-26T08:00:00+00:00", target="other")
        self.job("6", "2026-09-01T08:00:00+00:00", "failed", target="old")
        moment = transport.datetime.datetime(2026, 9, 26, 12, tzinfo=transport.datetime.timezone.utc)
        result = transport.stats(self.root, 7, moment)
        self.assertEqual((5, 2, 1, 1, 1), tuple(result[key] for key in ("deploys", "succeeded", "failed", "refused", "pending")))
        self.assertAlmostEqual(2 / 3, result["successRate"])
        self.assertEqual(120, result["medianRolloutSeconds"])
        self.assertEqual(24 * 3600 + 60, result["medianRecoverySeconds"])
        self.assertEqual(["2026-09-20", "2026-09-26"], [result["daily"][0]["date"], result["daily"][-1]["date"]])
        self.assertEqual({"succeeded": 0, "failed": 1, "refused": 1},
                         {key: result["daily"][4][key] for key in ("succeeded", "failed", "refused")})
        pilot = next(item for item in result["targets"] if item["targetId"] == "pilot")
        self.assertEqual((4, 2, 1, "succeeded", None), (pilot["deploys"], pilot["succeeded"], pilot["failed"],
                                                        pilot["lastStatus"], pilot["failingSince"]))
        self.assertNotIn("old", [item["targetId"] for item in result["targets"]])

    def test_stats_measure_the_window_before_for_trends(self):
        self.job("1", "2026-09-10T09:00:00+00:00", "succeeded", "2026-09-10T09:00:00+00:00", "2026-09-10T09:04:00+00:00")
        self.job("2", "2026-09-12T09:00:00+00:00", "failed", "2026-09-12T09:00:00+00:00", "2026-09-12T09:01:00+00:00")
        self.job("3", "2026-09-13T09:00:00+00:00", "succeeded", "2026-09-13T09:00:00+00:00", "2026-09-13T09:02:00+00:00")
        self.job("4", "2026-09-05T09:00:00+00:00", "succeeded", target="before-window")
        moment = transport.datetime.datetime(2026, 9, 26, 12, tzinfo=transport.datetime.timezone.utc)
        self.assertIsNone(transport.stats(self.root, 3, moment)["previous"])
        result = transport.stats(self.root, 14, moment)
        self.assertEqual((1, "2026-09-13"), (result["deploys"], result["since"]))
        previous = result["previous"]
        self.assertEqual(("2026-08-30", 3, 2, 1), (previous["since"], previous["deploys"], previous["succeeded"],
                                                   previous["failed"]))
        self.assertAlmostEqual(2 / 3, previous["successRate"])
        # A recovery that crosses into the current window belongs to neither.
        self.assertEqual((150, None), (previous["medianRolloutSeconds"], previous["medianRecoverySeconds"]))
        self.assertNotIn("targets", previous)

    def test_stats_without_deployments_has_no_rates(self):
        result = transport.stats(self.root, 30)
        self.assertEqual((0, None, None), (result["deploys"], result["successRate"], result["medianRolloutSeconds"]))
        self.assertEqual(30, len(result["daily"]))
        with self.assertRaises(ValueError):
            transport.stats(self.root, 0)

    def test_vitals_reads_every_target_and_names_a_failing_one(self):
        bindings = [transport.fleet.Target(name, "pilot", "pilot", transport.fleet.DeploymentEnvironment.STAGING,
                                           (), "platform") for name in ("pilot-staging", "pilot-broken")]
        config = {"serverId": "pilot", "provider": "Hetzner", "ssh": self.config,
                  "target": {"product": "pilot", "environment": "staging", "root": "/srv/wheelhouse"}}

        def remote(ssh, action, target, *arguments, release=None, timeout=30):
            self.assertEqual(("vitals", 60), (action, timeout))
            if remote.calls:
                raise transport.CommandFailed("SSH connection refused (exit 255); inspect the target privately")
            remote.calls += 1
            return {"project": "pilot-staging", "containers": [], "host": {"cpus": 2}}
        remote.calls = 0
        with patch.object(transport.fleet, "active_targets", return_value=bindings), \
                patch.object(transport.fleet, "resolve_target", return_value=config), \
                patch.object(transport, "ThreadPoolExecutor", FakePool), \
                patch.object(transport, "run_remote", side_effect=remote):
            result = transport.vitals(self.root)
        self.assertEqual([True, False], [item["ok"] for item in result["targets"]])
        self.assertEqual("pilot", result["targets"][1]["serverId"])
        self.assertTrue(result["targets"][1]["reason"].startswith("SSH connection refused"))
        with patch.object(transport.fleet, "active_targets", return_value=bindings):
            with self.assertRaisesRegex(ValueError, "not defined in code"):
                transport.vitals(self.root, "missing")

    def test_cli_rejection_carries_a_safe_reason(self):
        argv = ["transport.py", "template", "--root", str(self.root), "--bundle", "../escape"]
        with patch("sys.argv", argv), contextlib.redirect_stderr(io.StringIO()) as stderr:
            self.assertEqual(1, transport.main())
        self.assertEqual({"status": "rejected", "failure": "Rejected", "reason": "Invalid identifier"},
                         json.loads(stderr.getvalue()))


if __name__ == "__main__":
    unittest.main()
