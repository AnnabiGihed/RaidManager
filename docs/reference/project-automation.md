# Project automation

The [Raid Manager Project](https://github.com/users/AnnabiGihed/projects/2) uses native issue sub-issues.
The [completion workflow](https://github.com/AnnabiGihed/RaidManager/blob/main/.github/workflows/project-hierarchy.yml)
checks issue closure immediately and
audits closed issues every 15 minutes. It reopens an epic without completed child stories or a story without
completed child work items. Tasks, bugs, and spikes count as work items.

## Project status access

Repository workflow tokens cannot edit a user-owned GitHub Project. To reconcile a direct `Done` status change,
set the repository Actions secret `RAID_MANAGER_PROJECT_TOKEN` to a fine-grained token for `AnnabiGihed` with
Projects read and write access and RaidManager Issues read and write access. Give the token a short expiration,
rotate it before expiry, and store it only as an Actions secret. The scheduled workflow then changes invalid
parent statuses to `In Progress` and reopens those issues where needed. Without this secret, issue closure
enforcement remains active, but direct Project status edits receive a workflow warning and are not corrected.

To verify the rule, close a test story with no native child work item. The issue workflow must reopen it and
explain the missing work item. Set its Project status to `Done`, dispatch the completion workflow, and confirm
that the status returns to `In Progress`.
