"""Tests for the choice of the checks a pull request needs."""

from __future__ import annotations

import io
import os
import sys
import unittest
import unittest.mock
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_scope  # noqa: E402
from check_scope import ChangedFile, affects_companion, affects_dotnet, publishes, scopes  # noqa: E402

ALL = {"docs": True, "build-test": True, "companion": True}


def files(*paths: str) -> list[ChangedFile]:
    return [ChangedFile(path) for path in paths]


class DotnetTests(unittest.TestCase):
    def test_code_tests_build_files_fixtures_and_tools_run_it(self) -> None:
        for path in ("src/Core/RaidManager.Domain/Character.cs", "test/Core/RaidManager.Domain.Tests/A.cs",
                     "test/Fixtures/Addon/SavedVariables/one-character/WTF/Account/A/SavedVariables/RaidManager.lua",
                     "Directory.Build.props", "RaidManager.sln", ".editorconfig", "coverage.runsettings",
                     ".github/actions/setup-tools/action.yml", "scripts/coverage_gate.py", ".gitattributes"):
            with self.subTest(path=path):
                self.assertTrue(affects_dotnet(path))

    def test_documentation_skills_scripts_addon_and_other_workflows_skip_it(self) -> None:
        for path in ("docs/adr/0001-x.md", "CHANGELOG.md", "README.md", ".agents/skills/x/SKILL.md",
                     ".claude/skills/x/SKILL.md", "scripts/work_gate.py", "src/Addon/RaidManager/Core.lua",
                     "test/Addon/core_spec.lua", ".github/workflows/review.yml", ".github/ISSUE_TEMPLATE/6-task.yml",
                     "mkdocs.yml", ".vale/styles/config/vocabularies/RaidManager/accept.txt", ".luacheckrc"):
            with self.subTest(path=path):
                self.assertFalse(affects_dotnet(path))


class CompanionTests(unittest.TestCase):
    def test_the_companion_projects_and_build_files_run_it(self) -> None:
        for path in ("src/Containers/UI/Core/RaidManager.Companion.Client/Features/Sync/SyncViewModel.cs",
                     "src/Containers/UI/Hosting/RaidManager.Companion/Features/Sync/SyncView.axaml",
                     "Directory.Packages.props", "nuget.config"):
            with self.subTest(path=path):
                self.assertTrue(affects_companion(path))

    def test_other_code_and_the_companion_tests_skip_it(self) -> None:
        for path in ("src/Containers/UI/Hosting/RaidManager.Web/Program.cs",
                     "test/Containers/UI/Hosting/RaidManager.Companion.Tests/AppTests.cs", "docs/index.md"):
            with self.subTest(path=path):
                self.assertFalse(affects_companion(path))


class ScopeTests(unittest.TestCase):
    def test_a_documentation_change_runs_only_docs(self) -> None:
        self.assertEqual(scopes(files("docs/reference/x.md", ".agents/skills/x/SKILL.md")),
                         {"docs": True, "build-test": False, "companion": False})

    def test_a_code_change_skips_docs(self) -> None:
        self.assertEqual(scopes(files("src/Core/A.cs", "test/Core/ATests.cs")),
                         {"docs": False, "build-test": True, "companion": False})

    def test_a_companion_change_runs_build_test_and_companion(self) -> None:
        changed = files("src/Containers/UI/Hosting/RaidManager.Companion/App.axaml")
        self.assertEqual(scopes(changed), {"docs": False, "build-test": True, "companion": True})

    def test_an_addon_change_runs_only_docs_for_its_markdown(self) -> None:
        self.assertEqual(scopes(files("src/Addon/RaidManager/Core.lua")),
                         {"docs": False, "build-test": False, "companion": False})
        self.assertTrue(scopes(files("src/Addon/README.md"))["docs"])

    def test_deleted_renamed_or_documented_code_runs_docs(self) -> None:
        for changed in ([ChangedFile("src/Core/A.cs", "removed")],
                        [ChangedFile("src/Core/B.cs", "renamed", "src/Core/A.cs")],
                        files("src/Containers/UI/Hosting/RaidManager.Web/wwwroot/logo.svg")):
            with self.subTest(changed=changed):
                self.assertTrue(scopes(changed)["docs"])

    def test_a_renamed_file_counts_where_it_came_from(self) -> None:
        moved = [ChangedFile("docs/old-code.md", "renamed", "src/Core/A.cs")]
        self.assertTrue(scopes(moved)["build-test"])

    def test_changing_how_checks_are_chosen_runs_everything(self) -> None:
        for path in (".github/workflows/checks.yml", "scripts/check_scope.py"):
            with self.subTest(path=path):
                self.assertEqual(scopes(files(path)), ALL)


class PublishTests(unittest.TestCase):
    def test_documentation_the_site_config_and_the_wiki_builder_publish(self) -> None:
        for path in ("docs/reference/project-automation.md", "mkdocs.yml", "scripts/build_wiki.py",
                     ".github/workflows/docs-publish.yml"):
            with self.subTest(path=path):
                self.assertTrue(publishes(files(path)))

    def test_code_skills_and_root_documents_do_not_publish(self) -> None:
        self.assertFalse(publishes(files("src/Core/A.cs", ".agents/skills/x/SKILL.md", "CHANGELOG.md",
                                         "scripts/work_gate.py")))

    def test_a_page_moved_out_of_docs_publishes(self) -> None:
        self.assertTrue(publishes([ChangedFile("notes/a.md", "renamed", "docs/a.md")]))


class MainTests(unittest.TestCase):
    def run_main(self, environment: dict[str, str], *arguments: str) -> str:
        output = io.StringIO()
        with unittest.mock.patch.dict(os.environ, environment, clear=True), \
                unittest.mock.patch.object(sys, "argv", ["check_scope.py", *arguments]), redirect_stdout(output), \
                redirect_stderr(io.StringIO()):
            check_scope.main()
        return output.getvalue()

    def test_published_reads_the_merged_pull_request(self) -> None:
        listed = '{"path": "src/Core/A.cs", "status": "modified", "previous_path": null}\n'
        with unittest.mock.patch.object(check_scope.subprocess, "run", return_value=unittest.mock.Mock(stdout=listed)):
            self.assertEqual(self.run_main({"PR_NUMBER": "586"}, "--published"), "publish=false\n")

    def test_a_run_on_main_runs_every_check(self) -> None:
        self.assertEqual(self.run_main({"EVENT_NAME": "workflow_dispatch"}),
                         "docs=true\nbuild-test=true\ncompanion=true\n")

    def test_a_pull_request_reads_its_files(self) -> None:
        listed = '{"path": "docs/a.md", "status": "modified", "previous_path": null}\n'
        result = unittest.mock.Mock(stdout=listed)
        with unittest.mock.patch.object(check_scope.subprocess, "run", return_value=result) as run:
            output = self.run_main({"EVENT_NAME": "pull_request", "PR_NUMBER": "582"})
        self.assertEqual(output, "docs=true\nbuild-test=false\ncompanion=false\n")
        self.assertIn("repos/AnnabiGihed/RaidManager/pulls/582/files", run.call_args.args[0])

    def test_a_pull_request_number_must_be_a_number(self) -> None:
        with self.assertRaises(ValueError):
            self.run_main({"EVENT_NAME": "pull_request", "PR_NUMBER": "582; rm -rf /"})


if __name__ == "__main__":
    unittest.main()
