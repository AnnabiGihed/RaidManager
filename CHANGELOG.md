# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- ADR-0028 proposes how the test environment's secrets are kept and delivered: environment secrets of a GitHub
  environment named `test`, limited to `main`, written to the server's `.env` file by the deploy workflow; a
  dedicated, restricted SSH key for the deploy; a separate Discord application for the test environment; the rotation
  of each secret; and user secrets kept for local development. The README gains its deployment notes (#364).
- The `raidmanager-board-operations` skill holds the Project's field and option ids and the commands for creating,
  linking, starting, handing over and closing work items, bulk changes with a dry run and read-back, and the pitfalls
  met so far; `raidmanager-conventions` gains the lint quirks, Windows pitfalls and how the owner works with the
  agent, so a new agent session keeps the same practices (#404).
- ADR-0027 proposes how the test environment runs on the OVH VPS-1: containers generated from the AppHost with
  Docker Compose, Caddy with Let's Encrypt in front of `raidmanager-test.pivotsoftwares.com` and its `api.`
  subdomain, SQL Server Express capped at 2 GB, images built in GitHub Actions, and the provisioning steps (#380).
- Three Project views show releases and sprints at a glance: Release plan (a timeline grouped by release, with sprint
  and release markers), Releases (items, counts and Story Points per release), and Sprints (the same per sprint) (#344).
- `scripts/work_gate.py`: the agent preflight checks the seven conditions of the active-sprint gate for a task, and
  the board report lists scheduling violations, Status and closure mismatches, unestimated selected stories,
  contract gaps, unavailable prerequisites and Blocked items without a reason, optionally maintaining the
  `scheduling-violation` label (#328).
- Planning records: Sprint 1 (2026-10-02 to 2026-10-16, adopting the work management specification) and a draft
  release v1.0 record with its goal, acceptance criteria, scope and sprint sequence awaiting owner approval (#330).
- The community page manages roles: one roles card with each role's Discord roles, what it allows and its members;
  Create role, Edit and Delete with a confirmation; a role manager sees the roles they can't change as locked and
  can't let a role manage roles; other members see the card read-only (#314).
- The API lets the Administrator, or a member whose role grants Manage community roles, create, rename, change the
  permissions of and delete a community's roles, and map Discord roles to them. Only the Administrator changes a
  role that manages roles or lets one do so, and role names are unique in a community (ADR-0024) (#313).
- Each community holds its own roles, each with the permissions it allows: Manage raids, Build rosters, Run raid
  night, Review conflicts and Manage community roles. Officer and Raid leader are presets every community starts
  with, and existing mappings keep giving them. A member has every permission of every role their Discord roles
  give, and the members page shows each of their roles (ADR-0024) (#312).
- Every signed-in page is on the design system with a dark content area: the Overview of a player with a community
  welcomes them with their account card, a page that fails shows the error with Try again, and the sidebar's
  community card shows it leads to the community page with a chevron and a hover highlight (#306).
- The character review page follows its mockup: the design system's heading with Decide later, a table with each
  class by name and color and a status badge, a confirmation dialog before a rejection, notifications in the
  design system's style, and an all-set card. Class names read as players say them, such as Death Knight (#305).
- A member of a linked Discord server sees its community after signing in, on the Overview and in the sidebar's
  community card. Sign-in also asks Discord for the servers the player is in and keeps only the matching
  communities until the next sign-in; if Discord or the API can't answer, sign-in still completes. The API matches
  Discord servers to communities and lists a user's matched communities after the ones they administer (ADR-0023)
  (#296).
- Players sign in to the website with Discord and sign out; a canceled or failed sign-in explains why and offers a
  retry. The website runs in Interactive Server mode with Radzen (ADR-0012) (#99).
- The API resolves a Discord sign-in to exactly one local user for the website, refreshing a changed Discord profile.
  Only the website can call it, with a key the Aspire AppHost generates (ADR-0011) (#92, #95).
- Signed-in pages have the dark app shell of ADR-0019: a sidebar with the logo, the community card, the navigation
  of existing pages and the player's card, and a top bar with the breadcrumb, the avatar and Sign out. Signed-out
  pages show the logo above a centered sign-in card (#263).
- The website serves the Open Sans font from its own files instead of Google Fonts (#265).
- The signed-out home and sign-in failure pages follow the sign-in mockup: a centered heading, dark cards with an
  accent for the failure kind, and the design system's teal and secondary buttons, built from generic components (#275).
- After sign-in, a player with characters awaiting their decision lands on the character review page. They approve
  or reject each one, with a confirmation before rejecting, or decide later. A character another player owns waits
  for an officer, and a page that can't load offers a retry (#253). A player whose only waiting characters are
  owned by another player also sees the page, which says an officer will review them (#255).
- The API lets the website list a player's character claims awaiting a decision, oldest first, and approve or reject
  one. Approving a character another player owns keeps the claim in conflict review (ADR-0010) (#85, #89, #161).
- The community page shows its officer roles: each RaidManager role with the Discord roles that give it and how many
  members have it, read from Discord. The Administrator adds a Discord role to Officer or Raid leader from the
  server's roles, removes one with its ×, and sees a role deleted in Discord as missing; others see the card read-only.
  One Discord role can give both Officer and Raid leader, and each row counts the people who get it (#289).
- Members and roles page: from the community page's officer roles card, View members lists the server's members
  read from Discord, with their avatar, their Discord roles and the RaidManager role they get, Administrator first, and
  says when Discord was last checked. A refused, removed-bot or unavailable answer explains itself (#290).
- The API reads a community's Discord roles and members with the bot, giving each role's Discord roles and member
  count, and lets the Administrator map Discord roles to Officer and Raid leader. Reading the server keeps the stored
  server name current (#300).
- The API lists a community's members for its members: each one's Discord roles, avatar and RaidManager role, and
  when Discord was asked (#290).
- A signed-in player without a community sees how to link one on the Overview. Adding RaidManager to a Discord
  server from the website opens Discord's page; RaidManager then learns the server from Discord, asks for the Warmane
  realm, and links the community with the player as its Administrator. A server that is already linked says so, and
  a canceled or failed attempt explains why. The sidebar shows the community, and its Administrator's role (#288).
- The API lets the website link a Discord server as a community, once per server, and read communities back by id,
  by server and by Administrator (#297).
- The API checks a member's role in a community with Discord, using the bot token, and reuses each answer for a
  minute. Someone who isn't in the Discord server gets no role, and a check Discord can't answer is refused. The
  AppHost has a new `discord-bot-token` secret (ADR-0022) (#287).
- Communities are stored in SQL Server: the linked Discord server, its Warmane realm, the Administrator who added the
  bot, and the Discord roles that give Officer or Raid leader. A member's role follows their Discord roles, which
  RaidManager reads from Discord when an action needs them (ADR-0022) (#286).
- Raids are stored in SQL Server with EF Core: their requirements, targets in the organizer's order, signups with
  the offered loadouts, and roster selections (#281).
- Characters are stored in SQL Server with EF Core: claims, loadouts and raid saves, with domain events recorded in an
  outbox table (#87).
- Character claims in the domain: pending, approved, rejected and conflict. A repeated upload never approves,
  reopens or transfers a claim (#48).
- Combined raids in the domain: a raid requires one or more distinct instance and difficulty targets (#49).
- Raids in the domain have a title and a size of 10 or 25 players shared by every target. An officer can edit a
  raid's details until it starts, and the change says whether the start or the targets moved (#257).
- Signup availability in the domain: confirmed, tentative, late with an arrival time, or declined, independent of
  roster selection (#50).
- Raid-start readiness in the domain: each raid target gets an available, resets-before-raid, needs-fresh-sync or
  locked-through-raid verdict. A combined raid takes the most restrictive one, and a locked character can't sign up
  or be rostered (#54).
- Raid-save scan evidence in the domain: only a complete scan replaces a character's raid saves, even when it finds
  none. Incomplete and out-of-order scans keep the newer saves (#57).
- Warmane lockout evidence matrix with explicit unknown reset, difficulty, extension and encounter rules (#40).
- Version 1 visual guide with UML use cases, domain models, components, sequences, state transitions, readiness
  decisions, editable sources and rendered diagrams.
- Initial Clean Architecture solution on Pivot.Framework and its package source, with the Identity, Characters,
  Communities and Raids domain model, its domain events and repository contracts, and the decision record for
  Discord-only authentication and Warmane-first integration.
- An interactive API reference page at `/scalar` in Development, linked from the Aspire dashboard (ADR-0013) (#109).
- Every work item sits in one Epic, Feature, Story, Improvement or Bug, Task or Spike hierarchy. Issue forms ask for
  the parent. The hierarchy workflow labels misplaced items and reopens a parent closed before at least one child is
  completed. A pull request closes only tasks and spikes whose chain reaches an epic (ADR-0016) (#111, #116, #159).
- CI measures test coverage on every run and shows it on each pull request. A pull request fails when its changed
  lines are under 80% covered or total coverage is under 60% (ADR-0015) (#121).
- The GitHub Wiki shows the `docs/` documentation, generated and published after every merge, and pull requests check
  that it builds. The site navigation lists every decision record (ADR-0014) (#119).
- Penpot is the UI design tool. Every item and pull request that changes what users see shows its mockup from
  `docs/mockups/`, and issue forms ask about user-interface impact (ADR-0017) (#165).
- UI mockups are generated in the repository as native Penpot files with editable text, shared colors and text
  styles, a clickable prototype and WCAG AA contrast. Each mockup's SVG is rendered from its `.penpot` file, and the
  docs check fails on a stale SVG or on a mockup outside `docs/mockups/`. The `penpot-mockups` skill holds the
  workflow (ADR-0018) (#169).
- A dark design system from the owner's reference design: named colors, Open Sans text styles, and an app shell on
  every website screen with the Player and Officer navigation, community, breadcrumb, realm status and user. Mockups
  are generated with it, and the website will be re-themed to match (ADR-0019) (#202).
- The app shell's Officer section lists Conflicts, which opens the ownership conflicts page; every officer mockup
  is regenerated with it (ADR-0019) (#251).
- The claim conflicts mockup in `docs/mockups/`: the officer's list of ownership conflicts with resolved decisions kept
  for audit, resolving one with a required reason, the outcome each player sees, and the access error for
  non-officers (#249).
- The preparation and history mockup in `docs/mockups/`: missing enchant and gem warnings, an inspected loadout,
  advisory gear requirements that never exclude anyone, and attendance history with each record's source (#247).
- The roster is imported into the WoW addon by pasting a versioned code from the website (ADR-0021). The addon
  roster export mockup in `docs/mockups/` shows the code with the characters left out, the in-game paste box, the
  roster frame with invites on click only, an older code refused, and no code before publication (#245).
- The boss assignments mockup in `docs/mockups/`: defining a boss's assignments, copying them from a prior run with a
  missing character left to replace, a player's own assignments, and a stale reference flagged after a swap (#243).
- The raid-night attendance mockup in `docs/mockups/`: check-in with late, absent and bench, a substitute swap with
  ownership and lockout checked again, a swap refused for an active lock, and the night's log after the raid (#241).
- The roster publication mockup in `docs/mockups/`: the final review with changed places and open warnings, the
  published roster with its single Discord post edited in place, and a published roster flagged after a lockout
  change with nothing replaced (#239).
- The roster coverage mockup in `docs/mockups/`: covered, placeholder and missing raid buffs and debuffs, the five
  kinds of composition warning, and one warning inspected with the officer's choices (#237).
- The roster composer mockup in `docs/mockups/`: unpublished drafts in five groups with the bench, one copied from a
  prior raid, swapping with the bench, placeholders, one character per player, and role and class balance (#235).
- The candidate workspace mockup in `docs/mockups/`: every candidate with preferred option, offered loadouts,
  response, note, readiness, data freshness and assignment, role counts, search and filters, an empty result, and
  data still syncing (#233).
- The readiness review mockup in `docs/mockups/`: signups and roster places checked again with verdicts per target
  and their evidence, a published place made ineligible with nothing replaced, and an officer's exception for Needs
  fresh sync with its reason (#231).
- The raid notifications mockup in `docs/mockups/`: reminders and material changes as Discord direct messages, the
  website's notification settings, and the banner with Retry when a direct message can't be delivered (#229).
- The Discord signup mockup in `docs/mockups/`: the raid post with its buttons, choosing characters, the preferred
  one and availability in a private message, the late arrival time, the confirmation, and refusals when signups
  are closed or a character is locked (#227).
- The raid signup mockup in `docs/mockups/`: offering several characters and specs with one preferred, a note,
  presets and the readiness of each option, availability including late with an arrival time, and editing or
  withdrawing a signup (#225).
- The raid templates mockup in `docs/mockups/`: a template with weekly recurrence, reviewing the generated raids
  with an existing week skipped, and copying a previous raid into a draft whose readiness is checked again (#223).
- The raid list and editor mockup in `docs/mockups/`: upcoming and past raids with local and UTC times, status and
  signup totals, creating a raid with several targets and readiness requirements, and editing an open raid with the
  readiness recalculation warning (#221).
- The character profile mockup in `docs/mockups/`: the player's characters with sync freshness, a profile with
  sources, professions, loadouts, raid saves and equipment, and editing visibility, note, loadout labels and
  player-reported data (#219).
- The companion sync mockup in `docs/mockups/`: watched installations and account folders with exclude, sync
  status with the upload queue, paused, offline, and an incomplete SavedVariables write with its fix (#217).
- The companion pairing mockup in `docs/mockups/`: the companion's code while waiting, paired, expired and revoked,
  and the website pages to confirm the code, list paired companions and revoke one (#215).
- The community settings mockup in `docs/mockups/`: linking a Discord server by adding the bot, choosing the realm,
  a server already linked, the Discord roles mapped to Officer and Raid leader, members and their roles, and a
  change refused after a role loss (#213).
- The sign-in mockup in `docs/mockups/`: the signed-out page, the signed-in shell with Sign out, and the canceled
  and failed sign-in pages. Signed-out pages are centered pages without the app shell, and the shell's top bar
  gains Sign out (ADR-0019) (#211).
- A required `sonar` check fails a pull request on any open SonarCloud issue or security hotspot to review, with
  an annotation on each line; SonarCloud's own check passed code smells (ADR-0020) (#208).
- The character review mockup in `docs/mockups/`: pending claims with a conflict, the reject confirmation, the
  all-reviewed state with its notification, and the load error, linked as a clickable prototype (#167).
- A how-to page on reviewing a pull request, with comments that pass and fail the review gate (#81).

### Changed

- Sprint 4 has its review: goal achieved, 11 items completed (18 Story Points of stories and 38 of improvements,
  retrospective), nothing unfinished. The item lists of Sprints 1 to 4 and of releases v0.1 and v0.2 now match the
  Project, including the parts split from mixed-release items (#400).
- Every item records who works on it and when: it is assigned to its author and its Start date set when work starts,
  and its Target date set to the day it closes. The 188 closed items and 30 started items now have them too, and the
  `work-task-execution-and-completion` skill makes it part of starting and closing work (#397).
- Sprint 5 holds every doable v0.3 item, 75 Story Points in dependency order, against a capacity assumption raised
  to 70 points per sprint; the Project records a Delivery Stage on stories, improvements, bugs and spikes, a new Risk
  field with its reason on each item, and a planned Start and Target date on every open item (#390, #394).
- Sprint 5 also holds the OVH spike #373, so both deployment decisions finish before Sprint 6 (#379).
- Release v0.3 now includes the deployment of a test environment on an OVH VPS-1 under a subdomain of
  `pivotsoftwares.com` (epic #369, starting in Sprint 6), and story #14 moves to v0.4 with the raid commands it needs
  (#368).
- Release v0.3 is approved and Planned, and Sprint 5 (3 to 17 October) is planned: Discord sign-in proven on evidence,
  the SonarCloud findings on `main` fixed, and the companion threat model and deployed secrets spikes, 10 points and
  two 3-day spikes (#361).
- Every sprint now holds one release: finished items moved to the release in which their work was done, items with
  tasks in two releases were split into one item per release (#349 to #358), every story, improvement, bug and spike
  has Story Points, every task has a Delivery Stage, and the Sprints view lists outcome items. The board report and
  the preflight flag an item whose release differs from its sprint's release (#347, #348).
- Release 1 is split into five releases: v0.1 Delivery foundation and v0.2 Design system and process group the
  finished work by day in one-day history sprints (Sprints 1 to 4); v0.3 Identity and characters, v0.4 Raid scheduling
  and readiness, and v1.0 Rosters and raid night are planned in two-week sprints (Sprints 5 to 16), each ending in a
  stabilization sprint. Every item is on its release milestone, and each release and sprint has a record (#339).
- The issue forms ask for each type's contract from the work management specification, and the type labels describe
  when to choose each type. The hierarchy guard applies the specification's milestone rule (a task shares its
  parent's release; a feature or epic has a milestone only when all its scope is in it), labels new items missing
  part of their contract `needs-contract`, labels dependency cycles and canceled prerequisites `dependency-problem`,
  and reopens a release milestone closed before its record shows the delivery (#326).
- The Raid Manager Project follows the work management specification: Status has Backlog, Ready, In Progress,
  In Review, Blocked, Done and Canceled (every item kept its value); new Sprint, Delivery Stage and Story Points
  fields; the 12 views of the specification replace the five earlier ones; and the built-in Item closed and Item
  reopened workflows are switched off (#327).
- Eight work management skills apply the specification: classification and hierarchy, backlog refinement, release
  planning, sprint planning and the active-sprint gate, task execution and completion, sprint review and carryover,
  stabilization and release closure, and board configuration and validation. The GitHub Project workflow skill keeps
  only branch, pull request and review mechanics, and `AGENTS.md` makes the active-sprint gate mandatory (#325).
- The Work Management and Delivery Specification is adopted as the authority for RaidManager's work management,
  with the owner's amendments: release milestones per level, a Canceled status, deliberate Status on close and
  reopen, merging as execution, and RaidManager's project policies such as two-week sprints in Europe/Brussels
  (ADR-0026) (#324).
- Work items follow ADR-0025, which supersedes ADR-0016: a spike sits under a feature beside stories, improvements
  and bugs, and holds tasks like them; each type is chosen by intent first, then scope. A spike closes only with a
  completed task, a pull request closes tasks only, and an open issue without exactly one type label is flagged
  `needs-parent`. The issue forms, skills and agent instructions state the rule (#318).
- The agent's working practices live in the skills and `AGENTS.md` instead of an agent's memory: drafting both
  review comments, a clean Vale and SonarCloud before handover, checking a page against its mockup, shared-checkout
  git hygiene, Windows line endings, and leaving the owner's running app alone (#279).
- When a pull request is reported merged, the workflow skill requires checking the merge, making sure the remote
  branch is deleted and deleting the local branch (#271).
- The website's colors, font and Radzen overrides live in one project-wide theme (`wwwroot/theme/`), and the app
  shell is built from standalone, generic components; the Blazor skills make both rules mandatory (#268).
- The website's pages, layout and their tests are organized by feature (`Features/<Feature>/Pages`, `Features/Shared/Layout`),
  and the Blazor and DDD skills make that layout mandatory (#261).
- A pull request merges only after its operator posts a meaningful review comment and marks it ready, and a peer
  then approves it with a meaningful comment. The review workflow squash-merges it with the workflow token, closes
  its tasks and deletes the branch; no personal token is involved, and a comment copied from another reviewer fails
  the gate (ADR-0006, ADR-0008, ADR-0009) (#46, #59, #65, #73, #75, #77, #79, #83).
- Documentation contributions pass root-file, style, link, diagram and rendered-site checks before merging (#44).
- The README explains step by step how to give a machine access to the Pivot.Framework package feed (#97).
- The API settings file no longer carries an unused Discord section or a placeholder database password (#102).

### Fixed

- Every work item now carries its parent's milestone, so the Release v1.0 view no longer shows tasks and
  improvements without their parents; the hierarchy guard flags a child on another milestone `needs-parent` (#321).
- The AppHost no longer logs a resource watch timeout every minute: the Aspire packages are pinned to 13.5.4, the AppHost
  SDK's version, until the Aspire 13.6.0 regression is fixed (#274).
- A `ui` item keeps its `needs-mockup` label until the mockup it names exists on `main`; naming a file that a task
  will create no longer counts as a mockup (#199).
- Documentation spelling is checked as strictly as the editor: US English plus one project word list that CI, Vale
  and Visual Studio share. British spellings and code names written as prose are fixed (#173).
- The repository scripts and their tests pass a type check. CI runs `mypy`, and a Pyright configuration lets editors
  resolve the scripts' imports (#165).
- The review workflow no longer leaves a ready pull request open without a sign. It retries a refused merge for about
  five minutes and, if nothing else would retry it, fails the `review` job with GitHub's message (#123).
- A parent marked `Done` too early returns to `In Progress` through the Project's built-in workflows, with no manual
  status correction (#116).
- Starting the Aspire AppHost opens the dashboard at a fixed address instead of leaving only a console (#105).
- The review workflow closes a pull request's tasks even when GitHub doesn't link its "Closes #N" (#91).
