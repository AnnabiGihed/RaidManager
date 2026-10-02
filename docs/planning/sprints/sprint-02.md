# Sprint 2

The planning record of Sprint 2, kept as the [Work Management and Delivery
Specification](../../reference/work-management-specification.md) requires (§7, §14). Reconstructed on 2026-10-02 from
the items' close dates (#339), when the owner split release 1. No sprint was planned in advance: the goal summarizes
what was done, and no capacity was recorded at the time. These one-day sprints adapt past work to the specification;
they don't change its rules.

| Field | Value |
| --- | --- |
| Sprint | Sprint 2 (Project iteration `Sprint 2`) |
| State | Ended |
| Start | 2026-09-30 00:00 Europe/Brussels, inclusive |
| End | 2026-10-01 00:00 Europe/Brussels, exclusive |
| Duration | One day (history, owner decision on #339) |
| Release | [`v0.1`](../releases/v0.1.md), its final sprint by owner exception: it wasn't a stabilization sprint |

## Sprint Goal

Make delivery safe and repeatable: review and merge automation, quality gates, documentation publishing, the local run
and the first screen mockups.

## Capacity assumptions

None recorded at the time. The sprint is reconstructed from completed work.

## Selected scope and outcome

Completed outcome items, by close date (their tasks closed with them):

- #144 (improvement) Merge approved pull requests automatically
- #145 (improvement) Require an operator review, then a peer review
- #146 (bug) A ready pull request stayed open after GitHub refused the merge
- #149 (improvement) Publish the documentation to the GitHub Wiki
- #151 (improvement) Run the product locally with the Aspire AppHost
- #152 (improvement) Browse the API in an interactive reference
- #155 (improvement) Gate pull requests on test coverage
- #157 (improvement) Codify the Project workflow and completion rules
- #181 (user story) Design the mockup for character review after sign-in
- #207 (improvement) Fail pull requests on SonarCloud findings
- #177 (user story) Design the mockup for Discord sign-in and session
- #178 (user story) Design the mockup for community linking and officer permissions
- #179 (user story) Design the mockup for companion pairing and revocation
- #180 (user story) Design the mockup for companion account discovery and upload
- #182 (user story) Design the mockup for character profiles
- #183 (user story) Design the mockup for the raid list and raid editor
- #184 (user story) Design the mockup for raid templates and recurrence
- #185 (user story) Design the mockup for the website signup form
- #186 (user story) Design the mockup for the Discord signup flow
- #187 (user story) Design the mockup for raid changes and reminders
- #188 (user story) Design the mockup for readiness re-evaluation and exceptions
- #189 (user story) Design the mockup for the candidate workspace
- #190 (user story) Design the mockup for draft compositions and the bench
- #164 (improvement) Require a Penpot mockup for every UI work item
- #168 (improvement) Generate Penpot mockups and render their SVGs in the repository
- #172 (bug) documentation spelling errors pass CI but show in the editor
- #198 (bug) a mockup path counts as a mockup before the file exists
- #349 (user story) Sign in with Discord and keep a secure session (part delivered in v0.1)
- #351 (user story) Review new characters and resolve ownership conflicts (part delivered in v0.1)
- #357 (improvement) Enforce the Epic, Feature, Story and Task hierarchy (part delivered in v0.1)
- #358 (improvement) Adopt the Command Center design system for mockups (part delivered in v0.1)

Corrected on 2026-10-03 to match the Project (#401): the list now follows each item's Sprint field, including
the parts split from mixed-release items (#347).

Unfinished work: items worked on during this day but not finished stay open on their planned release, with their
finished tasks recorded in this sprint. No Story Points were recorded for these days.
