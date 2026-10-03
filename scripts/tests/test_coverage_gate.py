"""Tests for the coverage gate."""

from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from coverage_gate import (  # noqa: E402
    MARKER, Coverage, Ratio, changed_lines, changed_ratio, gate_errors, known_base, line_ranges, read_reports,
    summary,
    total_ratio, unmeasured_projects,
)

DIFF = """diff --git a/src/Core/Domain/Raid.cs b/src/Core/Domain/Raid.cs
--- a/src/Core/Domain/Raid.cs
+++ b/src/Core/Domain/Raid.cs
@@ -10,0 +11,3 @@ public sealed class Raid
+    first
+    second
+    third
@@ -40 +43 @@ public sealed class Raid
-    old
+    new
@@ -60,2 +62,0 @@ public sealed class Raid
-    removed
-    removed
diff --git a/src/Core/Domain/Gone.cs b/src/Core/Domain/Gone.cs
--- a/src/Core/Domain/Gone.cs
+++ /dev/null
@@ -1,2 +0,0 @@
-    deleted
-    deleted
"""


def report(source: str, package: str, filename: str, hits: dict[int, int]) -> str:
    lines = "".join(f'<line number="{number}" hits="{count}"/>' for number, count in hits.items())
    return (f'<coverage><sources><source>{source}</source></sources><packages><package name="{package}"><classes>'
            f'<class name="C" filename="{filename}"><lines>{lines}</lines></class>'
            f'</classes></package></packages></coverage>')


class ReportTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)

    def write(self, name: str, content: str) -> Path:
        path = self.root / "TestResults" / name / "coverage.cobertura.xml"
        path.parent.mkdir(parents=True)
        path.write_text(content, encoding="utf-8")
        return path

    def test_a_line_hit_in_any_report_counts_as_covered(self) -> None:
        source = str(self.root)
        first = self.write("a", report(source, "Domain", "src/Core/Domain/Raid.cs", {11: 0, 12: 0}))
        second = self.write("b", report(source, "Domain", "src/Core/Domain/Raid.cs", {11: 3, 12: 0}))
        coverage = read_reports([first, second], self.root)
        self.assertEqual(coverage.lines, {"src/Core/Domain/Raid.cs": {11: True, 12: False}})
        self.assertEqual(total_ratio(coverage), Ratio(1, 2))

    def test_absolute_file_names_are_made_relative(self) -> None:
        absolute = (self.root / "src" / "Web" / "Program.cs").as_posix()
        path = self.write("a", report("/elsewhere", "Web", absolute, {1: 1}))
        self.assertIn("src/Web/Program.cs", read_reports([path], self.root).lines)

    def test_files_outside_src_are_ignored(self) -> None:
        path = self.write("a", report(str(self.root), "Tests", "test/Domain.Tests/RaidTests.cs", {1: 1}))
        self.assertEqual(read_reports([path], self.root).lines, {})

    def test_projects_without_coverage_data_are_listed(self) -> None:
        for project in ("src/Core/Domain/Domain.csproj", "src/Jobs/Bot/Bot.csproj"):
            (self.root / project).parent.mkdir(parents=True)
            (self.root / project).write_text("<Project/>", encoding="utf-8")
        coverage = Coverage()
        coverage.add("src/Core/Domain/Raid.cs", "Domain", 1, True)
        self.assertEqual(unmeasured_projects(self.root, coverage), ["Bot"])


class ChangedLineTests(unittest.TestCase):
    def test_reads_added_and_modified_lines_and_skips_deletions(self) -> None:
        self.assertEqual(changed_lines(DIFF), {"src/Core/Domain/Raid.cs": {11, 12, 13, 43}})

    def test_only_coverable_changed_lines_count(self) -> None:
        coverage = Coverage()
        for number, covered in {11: True, 13: False, 43: True, 50: False}.items():
            coverage.add("src/Core/Domain/Raid.cs", "Domain", number, covered)
        ratio, uncovered = changed_ratio(coverage, changed_lines(DIFF))
        self.assertEqual(ratio, Ratio(2, 3))
        self.assertEqual(uncovered, {"src/Core/Domain/Raid.cs": [13]})

    def test_a_change_without_coverable_lines_has_no_percentage(self) -> None:
        ratio, _ = changed_ratio(Coverage(), changed_lines(DIFF))
        self.assertIsNone(ratio.percent)


class GateTests(unittest.TestCase):
    def test_passes_at_both_minimums(self) -> None:
        self.assertEqual(gate_errors(Ratio(60, 100), Ratio(8, 10), 60, 80), [])

    def test_fails_when_changed_lines_are_under_the_minimum(self) -> None:
        errors = gate_errors(Ratio(90, 100), Ratio(7, 10), 60, 80)
        self.assertEqual(errors, ["Changed lines are 70.0% covered, under the 80% minimum."])

    def test_fails_when_the_total_is_under_the_minimum(self) -> None:
        errors = gate_errors(Ratio(59, 100), None, 60, 80)
        self.assertEqual(errors, ["Total line coverage is 59.0%, under the 60% minimum."])

    def test_a_change_without_coverable_lines_passes(self) -> None:
        self.assertEqual(gate_errors(Ratio(70, 100), Ratio(0, 0), 60, 80), [])

    def test_missing_coverage_data_fails(self) -> None:
        self.assertIn("No coverage data", gate_errors(Ratio(0, 0), None, 60, 80)[0])


class SummaryTests(unittest.TestCase):
    def coverage(self) -> Coverage:
        coverage = Coverage()
        coverage.add("src/Core/Domain/Raid.cs", "RaidManager.Domain", 11, True)
        coverage.add("src/Core/Domain/Raid.cs", "RaidManager.Domain", 13, False)
        return coverage

    def test_passing_summary_has_the_marker_and_the_tables(self) -> None:
        text = summary(self.coverage(), Ratio(1, 2), Ratio(1, 1), {}, [], 40, 80)
        self.assertTrue(text.startswith(MARKER))
        self.assertIn("✅ **The coverage gate passed.**", text)
        self.assertIn("| Changed lines | 1 / 1 | 100.0% | 80% | ✅ |", text)
        self.assertIn("| RaidManager.Domain | 1 / 2 | 50.0% |", text)

    def test_failing_summary_lists_the_reasons_and_uncovered_lines(self) -> None:
        text = summary(self.coverage(), Ratio(1, 2), Ratio(0, 3), {"src/Core/Domain/Raid.cs": [13, 14, 15, 20]},
                       ["Changed lines are 0.0% covered, under the 80% minimum."], 40, 80, ["RaidManager.DiscordBot"])
        self.assertIn("❌ **The coverage gate failed.**", text)
        self.assertIn("- `src/Core/Domain/Raid.cs`: 13-15, 20", text)
        self.assertIn("`RaidManager.DiscordBot`", text)

    def test_total_only_summary_has_no_changed_row(self) -> None:
        self.assertNotIn("Changed lines", summary(self.coverage(), Ratio(1, 2), None, {}, [], 40, 80))

    def test_line_ranges_join_consecutive_numbers(self) -> None:
        self.assertEqual(line_ranges([1, 2, 3, 7, 9, 10]), "1-3, 7, 9-10")



class KnownBaseTests(unittest.TestCase):
    def setUp(self) -> None:
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.root = Path(scratch.name)
        self.git("init", "--quiet", "--initial-branch=main")
        (self.root / "file.txt").write_text("content\n", encoding="utf-8")
        self.git("add", "file.txt")
        self.git("-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "--quiet", "-m", "first")
        self.commit = self.git("rev-parse", "HEAD").strip()

    def git(self, *arguments: str) -> str:
        return subprocess.run(["git", *arguments], cwd=self.root, check=True, capture_output=True, text=True).stdout

    def test_a_ref_is_known(self) -> None:
        self.assertEqual(known_base("main", self.root), "main")

    def test_a_full_commit_id_is_known(self) -> None:
        self.assertEqual(known_base(self.commit, self.root), self.commit)

    def test_anything_else_is_refused(self) -> None:
        for requested in ("missing", self.commit[:7], "main; rm -rf /", "--output=/tmp/x"):
            with self.subTest(requested=requested):
                self.assertIsNone(known_base(requested, self.root))

if __name__ == "__main__":
    unittest.main()
