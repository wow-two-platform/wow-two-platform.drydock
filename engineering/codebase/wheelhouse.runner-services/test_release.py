import copy
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import release
import runner

try:
    import yaml  # noqa: F401
    HAS_YAML = True
except ImportError:
    HAS_YAML = False

DIGEST = "@sha256:" + "a" * 64
DESCRIPTOR = {
    "descriptor": 1, "product": "pilot",
    "shared": ["codebase/pilot.backend-services/Directory.*.props"],
    "services": {
        "api": {"build": {"dockerfile": "deployment/backend.Dockerfile", "args": {"SERVICE": "Api"}},
                "paths": ["codebase/pilot.backend-services/Pilot.Api/**"], "health": "/health",
                "settings": {"required": ["Database:Connection"]}, "volumes": {"keys": "/data/keys"},
                "sites": {"app": {"path": "/api"}}, "needs": ["postgres"]},
        "edge": {"build": {"dockerfile": "deployment/edge.Dockerfile", "contexts": {"deployment": "deployment"}},
                 "paths": ["codebase/pilot.frontend-services/**", "deployment/edge/**"], "port": 80,
                 "health": {"command": ["wget", "-q", "-O-", "http://localhost/healthz"]},
                 "capabilities": ["NET_BIND_SERVICE"], "sites": {"app": None, "landing": {"exposure": "public"}}},
    },
}


def git(repo, *arguments):
    return subprocess.run(["git", "-C", str(repo), *arguments], check=True, capture_output=True, text=True).stdout.strip()


class DescriptorTests(unittest.TestCase):
    def test_defaults_fill_every_service(self):
        descriptor = release.validate(copy.deepcopy(DESCRIPTOR))
        api, edge = descriptor["services"]["api"], descriptor["services"]["edge"]
        self.assertEqual(("linux/amd64", 8080, "512m", "30s", "codebase"),
                         (descriptor["platform"], api["port"], api["memory"], api["stopGrace"], api["build"]["context"]))
        self.assertEqual(["CMD", "curl", "--fail", "--silent", "http://localhost:8080/health"], api["health"])
        self.assertEqual({"file": "/app/appsettings.Local.json", "required": ["Database:Connection"]}, api["settings"])
        self.assertEqual({"app": {"port": 80, "path": "/", "exposure": "public"},
                          "landing": {"port": 80, "path": "/", "exposure": "public"}}, edge["sites"])
        self.assertEqual(["CMD", "wget", "-q", "-O-", "http://localhost/healthz"], edge["health"])

    def test_rule_breaks_name_the_rule(self):
        cases = (
            (lambda d: d.update(extra=1), "Unknown top-level key"),
            (lambda d: d.update(product="Pilot"), "product must be"),
            (lambda d: d.update(descriptor=2), "descriptor must be 1"),
            (lambda d: d["services"]["api"].pop("health"), "health must be"),
            (lambda d: d["services"]["api"].update(paths=["../outside/**"]), "relative to engineering"),
            (lambda d: d["services"]["api"].update(settings={"file": "appsettings.json"}), "absolute path"),
            (lambda d: d["services"]["api"].update(sites={"app": {}}), "already serves site app"),
            (lambda d: d["services"]["api"].update(needs=["mongo"]), "needs takes"),
            (lambda d: d["services"]["api"].update(memory="lots"), "memory must"),
            (lambda d: d["services"]["api"].update(image="ghcr.io/o/api:1"), "without a tag"),
            (lambda d: d["services"]["api"].update(unknown=True), "unknown key"),
        )
        for mutate, message in cases:
            with self.subTest(message=message):
                descriptor = copy.deepcopy(DESCRIPTOR)
                mutate(descriptor)
                with self.assertRaisesRegex(release.Invalid, message):
                    release.validate(descriptor)

    def test_globs_match_inside_engineering_only(self):
        descriptor = release.validate(copy.deepcopy(DESCRIPTOR))
        self.assertEqual(["api"], release.changed_services(descriptor, [
            "engineering/codebase/pilot.backend-services/Pilot.Api/Controllers/Home.cs"]))
        self.assertEqual(["edge"], release.changed_services(descriptor, ["engineering/deployment/edge/Caddyfile"]))
        self.assertEqual(["api", "edge"], release.changed_services(descriptor, [
            "engineering/deployment/backend.Dockerfile", "engineering/deployment/edge.Dockerfile"]))
        self.assertEqual([], release.changed_services(descriptor, ["README.md", "codebase/pilot.backend-services/x.cs"]))

    def test_a_shared_path_changes_every_service(self):
        descriptor = release.validate(copy.deepcopy(DESCRIPTOR))
        self.assertEqual(["api", "edge"], release.changed_services(descriptor, [
            "engineering/codebase/pilot.backend-services/Directory.Packages.props"]))

    @unittest.skipUnless(HAS_YAML, "PyYAML is not installed for this interpreter")
    def test_the_local_server_foreverpin_descriptor_parses(self):
        path = Path(__file__).resolve().parents[2] / "deployment" / "rehearsal" / "foreverpin.deploy.yml"
        descriptor = release.validate(release.parse(path.read_text()))
        self.assertEqual(["management", "redirect"], list(descriptor["services"]))
        self.assertEqual({"app"}, set(descriptor["services"]["management"]["sites"]))


class PlanTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.repo = Path(self.temporary.name)
        git(self.repo, "init", "-q", "-b", "main")
        git(self.repo, "config", "user.email", "test@example.invalid")
        git(self.repo, "config", "user.name", "Test")
        self.write("engineering/codebase/pilot.backend-services/Pilot.Api/Program.cs", "one")
        self.write("engineering/codebase/pilot.frontend-services/app.ts", "one")
        self.first = self.commit("first")
        self.descriptor = release.validate(copy.deepcopy(DESCRIPTOR))
        self.base = {"product": "pilot", "release": "v1.0.0", "sourceCommit": self.first,
                     "images": {"api": "ghcr.io/o/pilot/api" + DIGEST, "edge": "ghcr.io/o/pilot/edge" + DIGEST},
                     "versions": {"api": {"version": "1.0.0", "changedIn": "v1.0.0"},
                                  "edge": {"version": "0.9.0", "changedIn": "v0.9.0"}}}

    def tearDown(self):
        self.temporary.cleanup()

    def write(self, path, text):
        target = self.repo / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text)

    def commit(self, message):
        git(self.repo, "add", "-A")
        git(self.repo, "commit", "-q", "-m", message)
        return git(self.repo, "rev-parse", "HEAD")

    def test_a_candidate_rebuilds_changed_services_and_keeps_the_rest(self):
        self.write("engineering/codebase/pilot.backend-services/Pilot.Api/Program.cs", "two")
        second = self.commit("api change")
        planned = release.plan(self.descriptor, self.repo, second, self.base)
        short = second[:7]
        self.assertEqual(("sha-" + short, "candidate", "v1.0.0"), (planned["release"], planned["kind"], planned["base"]))
        self.assertEqual({"build": True, "version": "1.0.0+" + short, "changedIn": "sha-" + short},
                         planned["services"]["api"])
        self.assertEqual({"build": False, "version": "0.9.0", "changedIn": "v0.9.0",
                          "image": "ghcr.io/o/pilot/edge" + DIGEST}, planned["services"]["edge"])

    def test_a_release_versions_changed_services_with_its_tag(self):
        self.write("engineering/codebase/pilot.frontend-services/app.ts", "two")
        second = self.commit("edge change")
        planned = release.plan(self.descriptor, self.repo, second, self.base, "v1.1.0")
        self.assertEqual(("v1.1.0", "release"), (planned["release"], planned["kind"]))
        self.assertEqual(("1.1.0", "v1.1.0"), (planned["services"]["edge"]["version"], planned["services"]["edge"]["changedIn"]))
        self.assertEqual(("1.0.0", False), (planned["services"]["api"]["version"], planned["services"]["api"]["build"]))

    def test_without_a_base_every_service_builds(self):
        planned = release.plan(self.descriptor, self.repo, self.first)
        self.assertTrue(all(entry["build"] for entry in planned["services"].values()))
        self.assertEqual("0.0.0+" + self.first[:7], planned["services"]["api"]["version"])

    def test_a_service_the_base_never_shipped_builds(self):
        base = copy.deepcopy(self.base)
        del base["images"]["edge"], base["versions"]["edge"]
        planned = release.plan(self.descriptor, self.repo, self.first, base)
        self.assertEqual({"edge"}, {name for name, entry in planned["services"].items() if entry["build"]})

    def test_a_base_of_another_product_is_refused(self):
        with self.assertRaisesRegex(release.Invalid, "another product"):
            release.plan(self.descriptor, self.repo, self.first, {**self.base, "product": "other"})

    def test_the_rendered_bundle_passes_the_runner(self):
        self.write("engineering/codebase/pilot.backend-services/Pilot.Api/Program.cs", "two")
        second = self.commit("api change")
        planned = release.plan(self.descriptor, self.repo, second, self.base)
        images = {"api": "ghcr.io/o/pilot/api@sha256:" + "b" * 64, "edge": planned["services"]["edge"]["image"]}
        compose_bytes, manifest = release.render(self.descriptor, planned, images, branch="main")
        output = self.repo / "bundle"
        release.write_bundle(output, compose_bytes, manifest, self.repo / "bundle.tar.gz")
        validated = runner.validate_bundle(output)
        self.assertEqual({"api": ["Database:Connection"]}, validated["requiredConfiguration"])
        self.assertEqual({"api": {"version": "1.0.0+" + second[:7], "changedIn": "sha-" + second[:7]},
                          "edge": {"version": "0.9.0", "changedIn": "v0.9.0"}}, validated["versions"])
        self.assertEqual(["api", "edge", "edge"], [site["service"] for site in runner.release_sites(validated)])
        self.assertEqual(8080, runner.service_port(validated, "api"))
        self.assertEqual(set(self.descriptor["services"]), set(validated["ports"]))
        compose = json.loads(compose_bytes)
        api, edge = compose["services"]["api"], compose["services"]["edge"]
        self.assertEqual(["pilot-${DEPLOY_ENVIRONMENT}-api"], api["networks"]["platform"]["aliases"])
        self.assertEqual("${API_SETTINGS:?Set API_SETTINGS}", api["volumes"][0]["source"])
        self.assertEqual({"type": "volume", "source": "keys", "target": "/data/keys"}, api["volumes"][1])
        self.assertEqual((["NET_BIND_SERVICE"], ["ALL"]), (edge["cap_add"], edge["cap_drop"]))
        self.assertEqual({"keys": {}}, compose["volumes"])
        self.assertEqual("main", manifest["branch"])


if __name__ == "__main__":
    unittest.main()
