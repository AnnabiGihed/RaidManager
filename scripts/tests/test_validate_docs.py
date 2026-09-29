"""Behavior tests for repository documentation validation."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_docs import validate  # noqa: E402


class DocumentationValidationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.root = Path(self.temporary_directory.name)
        (self.root / "docs" / "diagrams").mkdir(parents=True)
        (self.root / "README.md").write_text(
            "# RaidManager\n\n## Status\n\n**Experimental.** Owned by Gihed Annabi "
            "(https://github.com/AnnabiGihed/RaidManager/issues).\n\n## Build locally\n\n## Documentation\n",
            encoding="utf-8",
        )
        (self.root / "CHANGELOG.md").write_text(
            "# Changelog\n\n[Keep a Changelog](https://keepachangelog.com/) and "
            "[Semantic Versioning](https://semver.org/).\n\n## [Unreleased]\n\n### Added\n\n- Initial feature.\n",
            encoding="utf-8",
        )
        (self.root / "CONTRIBUTING.md").write_text(
            "# Contributing\n\n"
            + "".join(
                f"## {section}\n\n"
                for section in (
                    "Work items", "Branching", "Commit messages", "Pull requests", "Local setup", "Releases", "Security"
                )
            )
            + "Conventional Commits on main: dotnet restore, dotnet build, dotnet test. See SECURITY.md.\n",
            encoding="utf-8",
        )
        (self.root / "LICENSE").write_text(
            "Copyright (c) 2026 Gihed Annabi. Proprietary.\n", encoding="utf-8"
        )
        (self.root / "SECURITY.md").write_text("# Security policy\n", encoding="utf-8")

    def test_valid_repository_passes(self) -> None:
        self.assertEqual([], validate(self.root))

    def test_missing_required_file_fails(self) -> None:
        for name in ("README.md", "CHANGELOG.md", "CONTRIBUTING.md", "LICENSE", "SECURITY.md"):
            with self.subTest(name=name):
                file = self.root / name
                content = file.read_text(encoding="utf-8")
                file.unlink()
                self.assertIn(f"Missing required root file: {name}", validate(self.root))
                file.write_text(content, encoding="utf-8")

    def test_invalid_release_date_fails(self) -> None:
        with (self.root / "CHANGELOG.md").open("a", encoding="utf-8") as changelog:
            changelog.write("\n## [1.0.0] - 2026-02-30\n")
        self.assertTrue(any("invalid release date" in error for error in validate(self.root)))

    def test_invalid_semantic_version_fails(self) -> None:
        with (self.root / "CHANGELOG.md").open("a", encoding="utf-8") as changelog:
            changelog.write("\n## [01.0.0] - 2026-09-29\n")
        self.assertTrue(any("invalid release heading" in error for error in validate(self.root)))

    def test_missing_contribution_section_fails(self) -> None:
        document = self.root / "CONTRIBUTING.md"
        document.write_text(document.read_text(encoding="utf-8").replace("## Security", "## Other"), encoding="utf-8")
        self.assertIn("CONTRIBUTING.md is missing ## Security", validate(self.root))

    def test_missing_diagram_export_fails(self) -> None:
        (self.root / "docs" / "diagrams" / "context.mmd").write_text("flowchart LR\n", encoding="utf-8")
        self.assertIn("Missing SVG export for docs/diagrams/context.mmd", validate(self.root))

    def test_missing_diagram_source_fails(self) -> None:
        (self.root / "docs" / "diagrams" / "context.svg").write_text("<svg/>", encoding="utf-8")
        self.assertIn("Missing diagram source for docs/diagrams/context.svg", validate(self.root))


if __name__ == "__main__":
    unittest.main()
