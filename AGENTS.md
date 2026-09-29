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
- For GitHub Project work items or user-story development, use
  `.agents/skills/raidmanager-github-project-workflow/SKILL.md`. A linked task issue is required before implementation.
- For `.csproj` creation or package metadata changes, use `.agents/skills/raidmanager-project-packaging/SKILL.md`.
- For the World of Warcraft 3.3.5a addon (Lua, TOC, SavedVariables), use
  `.agents/skills/wow-addon-335a-lua/SKILL.md`.
