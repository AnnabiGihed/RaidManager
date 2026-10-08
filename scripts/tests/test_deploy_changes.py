"""Tests for the decision whether a merge needs a dev deployment."""

from __future__ import annotations

import os
import sys
import unittest
import unittest.mock
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import deploy_changes  # noqa: E402
from deploy_changes import affects_deployment, deployed_changes, last_deployed_commit  # noqa: E402

HISTORY = ["c3", "c2", "c1"]


class PathTests(unittest.TestCase):
    def test_source_deployment_and_build_files_deploy(self) -> None:
        for path in ("src/Containers/API/RaidManager.ApiService/Program.cs", "deploy/server/deploy-environment.sh",
                     "src/Containers/UI/Hosting/RaidManager.Web/Program.cs", "src/Containers/UI/Core/RaidManager.ViewModels/A.cs",
                     "Directory.Build.props", "Directory.Packages.props", "nuget.config", "global.json",
                     "dotnet-tools.json", "RaidManager.sln", ".github/workflows/deploy-dev.yml"):
            with self.subTest(path=path):
                self.assertTrue(affects_deployment(path))

    def test_everything_else_skips(self) -> None:
        for path in ("docs/reference/addon-savedvariables.md", "CHANGELOG.md", ".agents/skills/x/SKILL.md",
                     "test/Containers/UI/Hosting/RaidManager.Web.Tests/Support/TestClock.cs", "scripts/work_gate.py",
                     "docs/mockups/sign-in.svg", ".github/workflows/review.yml", "mkdocs.yml",
                     "test/Fixtures/Addon/SavedVariables/one-character/WTF/Account/A/SavedVariables/RaidManager.lua",
                     "srcs/readme.md", "", "src/Addon/RaidManager/Core.lua", "src/Addon/RaidManager/RaidManager.toc",
                     "src/Containers/UI/Core/RaidManager.Companion.Client/Features/Sync/Queue/SnapshotQueue.cs",
                     "src/Containers/UI/Hosting/RaidManager.Companion/Features/Sync/SyncView.axaml"):
            with self.subTest(path=path):
                self.assertFalse(affects_deployment(path))

    def test_only_deploying_files_are_listed(self) -> None:
        changed = ["docs/a.md", "src/Core/A.cs", "CHANGELOG.md", "deploy/b.sh"]
        self.assertEqual(["src/Core/A.cs", "deploy/b.sh"], deployed_changes(changed))


class LastDeploymentTests(unittest.TestCase):
    def test_the_newest_successful_deployment_in_history_is_the_base(self) -> None:
        deployments = [{"id": 3, "sha": "c3"}, {"id": 2, "sha": "c2"}, {"id": 1, "sha": "c1"}]
        states = {3: "failure", 2: "success", 1: "success"}
        self.assertEqual("c2", last_deployed_commit(deployments, states.get, HISTORY))

    def test_a_commit_outside_the_history_is_ignored(self) -> None:
        deployments = [{"id": 9, "sha": "elsewhere"}, {"id": 1, "sha": "c1"}]
        self.assertEqual("c1", last_deployed_commit(deployments, {9: "success", 1: "success"}.get, HISTORY))

    def test_no_successful_deployment_gives_no_base(self) -> None:
        deployments = [{"id": 3, "sha": "c3"}]
        self.assertIsNone(last_deployed_commit(deployments, {3: "in_progress"}.get, HISTORY))


class DecideTests(unittest.TestCase):
    def run_decide(self, trigger: str, deployments: list, states: dict, changed: str) -> tuple[bool, list[str]]:
        def git(*arguments: str) -> str:
            return "\n".join(HISTORY) if arguments[0] == "rev-list" else changed

        environment = {"TRIGGER": trigger}
        with unittest.mock.patch.dict(os.environ, environment), \
                unittest.mock.patch.object(deploy_changes, "git", git), \
                unittest.mock.patch.object(deploy_changes, "gh_json", return_value=deployments), \
                unittest.mock.patch.object(deploy_changes, "latest_state", states.get):
            return deploy_changes.decide()

    def test_a_manual_run_always_deploys(self) -> None:
        deploy, _ = self.run_decide("manual", [], {}, "")
        self.assertTrue(deploy)

    def test_a_documentation_merge_skips(self) -> None:
        deploy, reasons = self.run_decide("merge", [{"id": 1, "sha": "c1"}], {1: "success"}, "docs/a.md\nCHANGELOG.md")
        self.assertFalse(deploy)
        self.assertIn("skipped", reasons[0])

    def test_a_source_change_since_the_last_deployment_deploys(self) -> None:
        deploy, reasons = self.run_decide("merge", [{"id": 1, "sha": "c1"}], {1: "success"}, "docs/a.md\nsrc/A.cs")
        self.assertTrue(deploy)
        self.assertEqual("  src/A.cs", reasons[-1])

    def test_no_known_deployment_deploys(self) -> None:
        deploy, _ = self.run_decide("merge", [{"id": 1, "sha": "c1"}], {1: "failure"}, "docs/a.md")
        self.assertTrue(deploy)

    def test_main_prints_the_output_line(self) -> None:
        with unittest.mock.patch.object(deploy_changes, "decide", return_value=(False, ["why"])), \
                unittest.mock.patch("builtins.print") as printed:
            deploy_changes.main()
        self.assertEqual(unittest.mock.call("deploy=false"), printed.call_args_list[-1])


if __name__ == "__main__":
    unittest.main()
