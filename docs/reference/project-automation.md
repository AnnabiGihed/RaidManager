# Project automation

The [Raid Manager Project](https://github.com/users/AnnabiGihed/projects/2) uses native issue sub-issues. Epics and
stories follow one completion rule, enforced without a personal token or repository secret.

## Completion rule

- A story may be closed as completed only when it has at least one native child work item closed as completed,
  and every child work item is closed. Tasks, bugs, and spikes count as work items.
- An epic may be closed as completed only when it has at least one native child story closed as completed, and
  every child story is closed.
- A child closed as *not planned* or *duplicate* counts as closed but not as completed.
- An epic or story closed as *not planned* or *duplicate* is abandoned, not completed, so the rule doesn't apply.

## How the rule is enforced

The [completion workflow](https://github.com/AnnabiGihed/RaidManager/blob/main/.github/workflows/project-hierarchy.yml)
runs with the built-in `GITHUB_TOKEN`:

- When an issue is closed or reopened, or its labels change, it checks that issue, then its parent and
  grandparent. A child reopened under a completed story therefore reopens the story, and the epic above it.
- Every 15 minutes it audits every closed issue, as a safety net for events it missed.
- It reopens an invalid parent with a comment that names the missing or open children.

The Project keeps `Status` in step with the issue through its built-in workflows, which run on GitHub's side and
need no token:

| Built-in workflow | Setting | Role |
| --- | --- | --- |
| Item closed | Set `Status` to `Done` | A closed issue shows `Done`. |
| Auto-close issue | Close the issue when `Status` is `Done` | Marking an item `Done` closes its issue, so the guard checks it. |
| Item reopened | Set `Status` to `In Progress` | An issue the guard reopens leaves `Done` again. |

Together they undo an early `Done` without anyone's help. Marking a story `Done` closes it; the guard reopens it; the
Project moves it back to `In Progress`.

## One-time setup

Enable **Item reopened** in the Project: open the Project menu, select **Workflows**, then **Item reopened**. Set
the status to `In Progress`, then save and turn the workflow on. Keep **Item closed** and **Auto-close issue** on.
Repository Actions can't change a user-owned Project's settings, so the owner does this once in the browser.

## Verify the rule

1. Create a test issue labeled `type:story` with no children, and add it to the Project.
2. Set its Project status to `Done`.
3. Within a few minutes, the issue is reopened with a completion comment and its status is `In Progress`.
4. Close the test issue as *not planned*. It stays closed.
