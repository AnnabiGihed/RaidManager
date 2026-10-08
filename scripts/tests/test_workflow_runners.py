"""Behavior tests for the self-hosted runner check of the workflows (ADR-0034)."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from workflow_runners import FORK_GUARD, RUNS_ON, runner_problems, workflow_problems  # noqa: E402

REPOSITORY = Path(__file__).resolve().parents[2]
GUARD = f"    if: github.event_name != 'pull_request' || {FORK_GUARD}\n"
PUSH_ONLY = "on:\n  push:\n    branches: [main]\n\njobs:\n"
PULL_REQUEST = "on:\n  pull_request:\n    branches: [main]\n  push:\n    branches: [main]\n\njobs:\n"
STEPS = "    steps:\n      - uses: actions/checkout@v7\n"


def job(name: str, runs_on: str = RUNS_ON, condition: str = "") -> str:
    return f"  {name}:\n{condition}    {runs_on}\n{STEPS}"


class WorkflowRunnerTests(unittest.TestCase):
    def test_repository_workflows_pass(self) -> None:
        self.assertEqual(runner_problems(REPOSITORY), [])

    def test_job_on_the_runner_passes(self) -> None:
        self.assertEqual(workflow_problems("a.yml", PUSH_ONLY + job("build")), [])

    def test_job_on_another_runner_fails(self) -> None:
        problems = workflow_problems("a.yml", PUSH_ONLY + job("build", "runs-on: ubuntu-latest"))
        self.assertEqual(problems, [f"a.yml: job build must use `{RUNS_ON}` (ADR-0034)"])

    def test_every_job_is_checked(self) -> None:
        text = PUSH_ONLY + job("build") + "\n" + job("deploy", "runs-on: [self-hosted, linux]")
        self.assertEqual(len(workflow_problems("a.yml", text)), 1)

    def test_pull_request_job_without_guard_fails(self) -> None:
        problems = workflow_problems("a.yml", PULL_REQUEST + job("build"))
        self.assertEqual(len(problems), 1)
        self.assertIn("must skip forks", problems[0])

    def test_pull_request_job_with_guard_passes(self) -> None:
        self.assertEqual(workflow_problems("a.yml", PULL_REQUEST + job("build", condition=GUARD)), [])

    def test_guard_in_a_folded_condition_passes(self) -> None:
        condition = (f"    if: >-\n      github.event_name == 'pull_request' &&\n      {FORK_GUARD}\n")
        self.assertEqual(workflow_problems("a.yml", PULL_REQUEST + job("sonar", condition=condition)), [])

    def test_guard_in_a_step_does_not_count(self) -> None:
        text = PULL_REQUEST + f"  build:\n    {RUNS_ON}\n    steps:\n      - if: {FORK_GUARD}\n        run: echo\n"
        self.assertEqual(len(workflow_problems("a.yml", text)), 1)

    def test_pull_request_target_needs_no_guard(self) -> None:
        text = "on:\n  pull_request_target:\n    branches: [main]\n\njobs:\n" + job("review")
        self.assertEqual(workflow_problems("a.yml", text), [])

    def test_runs_on_inside_a_step_does_not_count(self) -> None:
        text = PUSH_ONLY + "  build:\n    steps:\n      - name: note\n        run: echo 'runs-on: [self-hosted, linux, pc-personal]'\n"
        self.assertEqual(len(workflow_problems("a.yml", text)), 1)

    def test_python_call_without_setup_fails(self) -> None:
        text = PUSH_ONLY + job("check") + "      - run: python scripts/check.py\n"
        problems = workflow_problems("a.yml", text)
        self.assertEqual(problems, ["a.yml: job check calls Python and must set it up with `actions/setup-python` (#564)"])

    def test_python3_call_without_setup_fails(self) -> None:
        text = PUSH_ONLY + job("check") + "      - run: >-\n          python3 scripts/check.py --flag\n"
        self.assertEqual(len(workflow_problems("a.yml", text)), 1)

    def test_python_call_with_setup_passes(self) -> None:
        setup = "      - uses: actions/setup-python@v7\n        with:\n          python-version: '3.12'\n"
        text = PUSH_ONLY + job("check") + setup + "      - run: python -m unittest\n"
        self.assertEqual(workflow_problems("a.yml", text), [])

    def test_python_in_names_and_paths_is_not_a_call(self) -> None:
        text = PUSH_ONLY + job("check") + "      - name: Check the python scripts\n        run: ./scripts/python.sh\n"
        self.assertEqual(workflow_problems("a.yml", text), [])
        text = PUSH_ONLY + job("check") + "      - run: ./bin/check-python-version\n"
        self.assertEqual(workflow_problems("a.yml", text), [])

    def test_repository_without_workflows_has_no_problem(self) -> None:
        with tempfile.TemporaryDirectory() as folder:
            self.assertEqual(runner_problems(Path(folder)), [])

    def test_yaml_extension_is_checked(self) -> None:
        with tempfile.TemporaryDirectory() as folder:
            workflows = Path(folder) / ".github" / "workflows"
            workflows.mkdir(parents=True)
            (workflows / "old.yaml").write_text(PUSH_ONLY + job("build", "runs-on: ubuntu-latest"), encoding="utf-8")
            self.assertEqual(len(runner_problems(Path(folder))), 1)


if __name__ == "__main__":
    unittest.main()
