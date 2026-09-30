# Project automation

The [Raid Manager Project](https://github.com/users/AnnabiGihed/projects/2) uses native issue sub-issues.
The [completion workflow](https://github.com/AnnabiGihed/RaidManager/blob/main/.github/workflows/project-hierarchy.yml)
checks issue closures and audits closed issues every 15 minutes. It reopens an epic without completed child stories
or a story without completed child work items. Tasks, bugs, and spikes count as work items. The workflow uses
GitHub Actions' built-in repository permissions. You do not need to create or store a personal token.

## Project status

The Project's built-in automation sets closed issues to `Done` and has an enabled auto-close issue workflow.
These workflows cannot test native child completion. The repository workflow reopens a parent issue that was
closed too early, including one closed after a Project status change. GitHub does not provide this repository
with a built-in way to reset the status of a user-owned Project item. A manual `Done` edit may therefore remain
visible after the issue is reopened. Move that item back to `In Progress` in the Project. Keep native sub-issue
progress visible when reviewing a parent. The [Status exceptions](https://github.com/users/AnnabiGihed/projects/2/views/5)
view shows issues that remain open while marked `Done`.

To verify the rule, close a test story with no native child work item. The issue workflow must reopen it and
explain the missing work item. If the Project still shows `Done`, restore its status to `In Progress`.
