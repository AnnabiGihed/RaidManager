# Repository authorship

Use Gihed Annabi for author and owner attribution in source comments, documentation, project metadata,
and copyright notices. Keep this spelling consistent in future edits.

## Skills

Project skills live in `.agents/skills/` and, identically, in `.claude/skills/` (for Claude Code).

- Every change to a skill (add, edit, rename, delete) is applied by hand to both trees in the same commit.
  Before committing, confirm the two trees are identical (for example `git diff --no-index .agents/skills
  .claude/skills` prints nothing).

- Start every task with `.agents/skills/raidmanager-conventions/SKILL.md`. It states which imported house and
  Pivot.Framework rules apply to RaidManager, which accepted ADRs replace (for example Discord instead of
  Keycloak), and which conflicts must be asked about.
- For work management (creating, classifying and planning work items, sprints, releases, status and completion),
  follow `docs/reference/work-management-specification.md` through the eight `.agents/skills/work-*/SKILL.md` skills
  (ADR-0026).
- For operating the GitHub Project (field and option ids, creating and linking items, setting fields, the closing
  order, bulk changes and known pitfalls), use `.agents/skills/raidmanager-board-operations/SKILL.md`.
- For delivering a change (branch, pull request, review comments, merge and cleanup), use
  `.agents/skills/raidmanager-github-project-workflow/SKILL.md`. A linked task issue is required before implementation.
- For `.csproj` creation or package metadata changes, use `.agents/skills/raidmanager-project-packaging/SKILL.md`.
- For the World of Warcraft 3.3.5a addon (Lua, TOC, SavedVariables), use
  `.agents/skills/wow-addon-335a-lua/SKILL.md`.
- For the desktop companion in Avalonia (ADR-0032), use `.agents/skills/avalonia-desktop/SKILL.md` to create, organize
  and write it, and `.agents/skills/avalonia-tests/SKILL.md` to test it.
- For UI mockups (Penpot files and their SVGs in `docs/mockups/`), use `.agents/skills/penpot-mockups/SKILL.md`.
  Never put a `.penpot` or mockup `.svg` file in the repository root.

## Working agreements (every session, mandatory)

These hold in every agent session; the skills named give the details.

- **No standalone work item, ever (non-negotiable).** Epic → Feature → User Story, Improvement, Bug or Spike → Task:
  every item has exactly one type label and, except an epic, a parent of the allowed type, in this Project or any
  other board. Classify by intent first, then scope, with `work-classification-and-hierarchy`, before creating an
  issue. Breaking this is a major failure.
- **No execution outside an active sprint.** Before starting or resuming any task, story, improvement, bug or spike,
  pass the gate: `python scripts/work_gate.py preflight <task>` (`work-sprint-planning-and-eligibility`, spec §8). If
  it fails, stop and report the violated rule; never move dates or sprints to pass it. When the sprint ends, stop at a
  safe checkpoint. Start every session with `python scripts/work_gate.py report` and report its violations.
- **One task, one branch, one draft PR**, following `raidmanager-github-project-workflow`. Branch from `origin/main`
  after checking `git branch --show-current`; stage explicit paths only, because other sessions share this checkout.
- **Before handover:** build, tests, coverage after committing, the docs check, the spell check and Vale, then a
  zero-finding `sonar_gate.py` (`raidmanager-conventions` §7, §8, §11). For UI changes, compare each state with its
  mockup board using the real styles (`raidmanager-conventions` §10).
- **With every PR, draft both review comments**, the operator's and the peer's, checked with
  `scripts/verify_review.py`. Never post them, approve, mark ready or merge (workflow skill, step 6). When the PR
  needs an owner check before merging (a companion build, a server run), give the check steps first and the review
  texts only after the check: posting both reviews merges the PR (#523 and #531 merged before their checks).
- **When told a PR is merged**, confirm it, make sure the remote branch is deleted, and delete the local branch
  before anything else (workflow skill, step 7).
- **Owner decisions** are recorded on the issue they settle; product questions are asked, never assumed.
- **"New session"**: when the owner says "new session", run the session-close routine of `raidmanager-conventions`
  §14 without asking for the details again: reach a safe checkpoint, record the session's lessons through a work item
  and one pull request, then give the handover message for the next session (owner decision on #510).
- **Blazor work** follows `blazor-components`: pages in feature folders, specific looks as standalone generic
  components, colors only from the project-wide theme.
