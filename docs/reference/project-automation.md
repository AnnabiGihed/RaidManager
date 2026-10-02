# Project automation

The [Raid Manager Project](https://github.com/users/AnnabiGihed/projects/2) tracks every work item in one hierarchy of
native sub-issues ([ADR-0025](../adr/0025-classify-work-items-with-spikes-under-features.md)). Nothing is
standalone. Automation enforces it without a
personal token or repository secret.

## Hierarchy

| Type | Label | Parent | Children |
| --- | --- | --- | --- |
| Epic | `type:epic` | none | features |
| Feature | `type:feature` | an epic | stories, improvements, bugs and spikes |
| Story | `type:story` | a feature | tasks |
| Improvement | `type:improvement` | a feature | tasks |
| Bug | `type:bug` | a feature | tasks |
| Spike | `type:spike` | a feature | tasks |
| Task | `type:task` | a story, improvement, bug or spike | none |

- Classify by intent first, then scope. A **story** adds a capability a user can't perform today. An **improvement**
  makes existing behavior, tooling, documentation or process better. A **bug** restores agreed or intended behavior.
  A **spike** answers a question, within a time box, before a direction is chosen. ADR-0025 has the identifying
  question and the "choose this, not that" rules for each type.
- A **task** is one bounded piece of work, delivered by one pull request; its parent says why the work exists.
- Every issue has exactly one type label. The issue forms set it and ask for the parent; add the new issue as a
  sub-issue of that parent.

## Rules

- **Parent:** every open issue has exactly one type label, every item except an epic has a parent of the type in
  the table, and an epic has none.
- **Completion:** an epic, feature, story, improvement, bug or spike may be closed as completed only when at least one
  child of the level below is closed as completed and every child is closed. A story, improvement, bug or spike
  therefore never closes without a completed task.
- **Pull requests:** a pull request closes tasks only (`Closes #N`), and each one must reach an epic through a
  story, improvement, bug or spike and a feature. Reference other items with `Refs #N`.
- A child closed as *not planned* or *duplicate* counts as closed but not as completed. An item closed that way is
  abandoned, so the completion and parent rules don't apply to it.

## User-interface mockups

Every item that changes what users see carries the `ui` label and links or shows its Penpot mockup
([ADR-0017](../adr/0017-penpot-mockups-for-ui-work.md), [how to design a screen](../how-to/design-a-screen.md)). A
mockup is `docs/mockups/<screen>.svg`, rendered from its `.penpot` file, shown as an image or link, or a Penpot
share link ([ADR-0018](../adr/0018-generate-penpot-mockups-in-the-repository.md)). A named SVG counts only once it
exists on `main`; until then the item keeps `needs-mockup`, and its comment names the missing file. The issue forms
ask **User interface**, and answering yes adds the `ui` label. The hierarchy workflow labels a `ui` item without a
mockup `needs-mockup`, with one comment, and removes the label once the mockup is linked. A pull request that changes
user-interface files must show its mockup, or state `No visual change:` with a reason.

## How the rules are enforced

The [hierarchy workflow](https://github.com/AnnabiGihed/RaidManager/blob/main/.github/workflows/project-hierarchy.yml)
runs with the built-in `GITHUB_TOKEN`:

- When an issue is closed or reopened, or its labels change, it checks that issue and its three ancestors. A task
  reopened under a completed story therefore reopens the story, its feature and its epic.
- Every 15 minutes it audits every issue, as a safety net for events it missed and for sub-issue changes, which
  start no workflow.
- It reopens an invalid parent with a comment that names the missing or open children.
- It labels a misplaced item, or one without a type label, `needs-parent` with one comment that says where it
  belongs, and a `ui` item without a mockup `needs-mockup`. It removes each label once the item is fixed. A new issue
  gets 10 minutes to be linked before it is flagged.

The docs `validate` check runs the pull-request rules on every pull request, so a task without a full chain, or a
user-interface change without its mockup, can't merge. After you fix a parent link, re-run that check from the pull
request's Checks tab. The review workflow closes only tasks when it merges.

The Project keeps `Status` in step with the issue through its built-in workflows, which run on GitHub's side and
need no token:

| Built-in workflow | Setting | Role |
| --- | --- | --- |
| Item closed | Set `Status` to `Done` | A closed issue shows `Done`. |
| Auto-close issue | Close the issue when `Status` is `Done` | Marking an item `Done` closes its issue, so the guard checks it. |
| Item reopened | Set `Status` to `In Progress` | An issue the guard reopens leaves `Done` again. |
| Auto-add sub-issues to project | On | A new child of a Project item joins the Project. |

Together they undo an early `Done` without anyone's help. Marking a story `Done` closes it; the guard reopens it; the
Project moves it back to `In Progress`.

## One-time setup

Enable **Item reopened** in the Project: open the Project menu, select **Workflows**, then **Item reopened**. Set
the status to `In Progress`, then save and turn the workflow on. Keep the other workflows in the table on.
Repository Actions can't change a user-owned Project's settings, so the owner does this once in the browser.

## Verify the rules

1. Create a test issue labeled `type:story` with no children, and add it to the Project.
2. Set its Project status to `Done`.
3. Within a few minutes, the issue is reopened with a completion comment and its status is `In Progress`.
4. After 10 minutes, the next audit labels it `needs-parent`, because it has no feature.
5. Close the test issue as *not planned*. It stays closed, and the next audit removes the label.
