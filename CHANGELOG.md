# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The companion's sync screens (boards 1 to 10 of the companion sync mockup): after pairing, Choose folders opens the
  watched WoW folders, where the player clears a whole folder or an account to keep its characters out, finds folders
  again or adds one with Windows' folder picker, then saves; the installations scroll when they don't fit. The sync
  screen shows the last success, the snapshots queued, the accounts watched and the recent activity, pauses and resumes
  uploads, and says what to do, with a retry, when RaidManager can't be reached, a snapshot is incomplete or refused, or
  an account's file comes from another addon version or isn't the addon's. A folder that isn't a WoW installation shows
  a warning (board 9). The window follows each board: 480 x 600 for pairing and 560 x 680 for sync, and a computer
  already paired opens on the sync (#551).
- The companion syncs characters in the background: at its first start it finds the WoW installations on the
  computer's fixed drives, then reads each account's `RaidManager.lua` without running it, once WoW has finished
  writing it, and uploads each character's snapshot to RaidManager. Waiting snapshots survive restarts and retry with
  growing waits while RaidManager can't be reached; a cut file uploads the characters before the cut and reports the
  cut one. Pausing stops uploads but not reading, and excluding an account drops its waiting snapshots. The screens
  that show and control this come with #551 (#550).
- The API imports the character snapshots a paired companion uploads (`POST /companion/snapshots`, schema 1 of the
  addon contract): a new character arrives with a pending claim for the companion's player, a character another player
  owns keeps its owner, and only a snapshot newer than the last one applied changes anything, so a retried upload is
  safe. Each talent group becomes a loadout whose active group gets the gear worn at the capture; GearScore, stats and
  item levels show "—" until an item catalog exists (#552). The website's paired companions list shows each
  companion's last upload (#384).
- My characters and the character profile on the website (boards 1 and 2 of the character profile mockup): a player
  sees each approved character with its primary loadout, current raid saves and how fresh its data is, and opens its
  profile with data sources, professions, visibility, loadouts, raid saves and every equipped slot. Only the owner sees
  a profile for now, and a profile is visible to the community until its owner chooses otherwise; editing comes with
  #545. The mockup's Equipment card now lists all 17 slots (#385).
- ADR-0033 (Proposed): players will install the companion from the Microsoft Store as an MSIX the Store signs, built on
  a Windows runner only when the companion changes and published to the Store by GitHub Actions on every merge; the
  listing stays private on dev until the release that ships the companion. The spike's prototype package ran on Windows
  11 (#533).
- The first time the companion's window is closed, a small notification at the bottom right of the screen says
  "RaidManager Companion is still running" and to quit it from its icon in the notification area; it closes after 6
  seconds or on a click, and never shows again (board 21, #530).
- The companion pairing mockup gains board 21: the first time the companion's window is closed, a Windows notification
  says "RaidManager Companion is still running" and to quit it from its icon in the notification area; later closes stay
  silent (#529).
- The Windows desktop companion, in Avalonia (ADR-0032, now Accepted): it asks RaidManager for a pairing code, shows it
  with a countdown, opens the website's pairing page with it, and polls for its device token every five seconds or more,
  waiting five seconds longer when asked to slow down or when RaidManager doesn't answer. Once the player confirms the
  code it keeps the token encrypted for the Windows user (DPAPI) in `%LOCALAPPDATA%\RaidManager\companion.dat`, and at
  each start it checks the token, forgetting it only when RaidManager revokes or refuses it. Its window shows the six
  states of the mockup, including two new boards for getting a code and for a code request that failed, and a tray icon
  (new board 18) keeps it running when the window closes. CI builds the `Dev` executable as an artifact, and every build
  opts out of Avalonia's telemetry (#514).
- Two skills for the desktop companion in Avalonia (ADR-0032): `avalonia-desktop` says how to create, organize and write
  it (the two projects, feature folders, MVVM without a framework, compiled bindings, the theme and embedded Open Sans,
  the generic host, the tray and close-to-tray, the UI thread, DPAPI behind a seam, HTTP, the self-contained publish and
  the telemetry opt-out), and `avalonia-tests` how to test it headless on Linux (xUnit v3 for the host, Reqnroll for the
  client's rules, Windows-only tests, rendered frames for the mockup comparison). Every version-dependent fact was
  checked against Avalonia 12.1 in a prototype; `AGENTS.md` and four skills point to them (#521).
- The website's Companion & sync pages: the page a companion opens with its code shows the code, the computer and
  the expiry, and confirms the pairing, or says why the code can't be confirmed (expired, already confirmed, unknown,
  missing, or not checkable); the paired companions list shows each computer's pairing time and status and revokes
  one after a confirmation. Last upload says "No upload yet" until uploads come with #384, and a companion unused for
  180 days shows as expired. "Companion & sync" joins the sidebar, and the mockup gains the boards for these states
  (#513).
- The API pairs a desktop companion as ADR-0030 decides, now Accepted: the companion starts a pairing on
  `POST /companion/pairings` and gets a code to show, the signed-in player checks and confirms the code through the
  website's internal routes within 10 minutes, and the companion collects its device token once on
  `POST /companion/pairings/token`. The API keeps only hashes of the device code and token, checks the token on every
  `/companion/` route, and refuses a missing, unknown, revoked or 180-days-unused one with 401 and its reason. The
  website's routes list a player's companions and revoke one at once. Pairing starts and token polls are
  rate-limited per address, code checks per player (#382).
- The addon captures the raid saves: it asks the game when the character enters the world, on a new zone and on
  `/rm sync`, at most once every five seconds, and records each saved instance with its lockout id, seconds to
  reset, difficulty, size and whether it is locked or extended, marking the scan incomplete when an instance can't
  be read. Every section of the contract is now captured; the addon's version is 0.4.0 (#489).
- The addon captures the loadouts: each saved equipment set with every slot's item and where it is (worn, in the
  bags, or unavailable in the bank or missing), and both talent groups with the points, the rank of each talent and
  the glyphs, the active group flagged. It captures them again when the sets, the gear, the talents, the active
  group or a glyph change. The addon's version is 0.3.0 (#488).
- The addon captures the professions, from the skill lines under the client's localized professions and secondary
  skills headers, and the equipped gear, slot by slot with item, enchant, gems, suffix and unique id; it captures
  them again when the skill list or the player's inventory changes. A collapsed header, an item the client hasn't
  described yet and an unreadable link are recorded as unavailable, never as empty. The addon's version is 0.2.0, so
  a snapshot shows which version wrote it (#487).
- The RaidManager addon for WoW 3.3.5a, in `src/Addon/RaidManager/`: on entering the world it writes the
  character's identity, guild, client and server time into `RaidManagerDB`, following the snapshot contract, with
  the other sections unavailable until their capture comes; `/rm` shows the sections and `/rm sync` captures again.
  busted specs compare the capture with the contract's fixtures, and the `addon` workflow runs them with luacheck
  and StyLua. ADR-0031 proposes the language, place and tools, and an addon change no longer deploys dev (#383).
- The addon snapshot contract, schema version 1: every section of a character's SavedVariables snapshot is observed
  or unavailable, with its observation time, so an unavailable value is never read as an empty one; raw game values
  only, and a complete-scan flag on raid saves. Fixtures cover one character, two account folders, an incomplete
  scan and raid saves with reset data (#38).
- Dependabot proposes each new release of a GitHub Action every Monday, keeping the commit-id pins of #365. The
  `dependency-task` workflow gives each of its pull requests a task under #461 and the description the `validate`
  check needs, and cancels the task of an update Dependabot closes without merging; the owner reviews these pull
  requests as the operator, and the agent selects their tasks into the sprint (#462).
- Sign-in tests prove the rest of story #13: a code or token Discord refuses, as it does after an expired or revoked
  authorization, leads to the retry page without a session, and a session left unused past its 12 hours asks the
  player to sign in again, while one used within them still opens protected pages (#362).
- Each dev deployment marks what it delivered: completed items, parents whose completed children all are, and
  releases whose items all are get `deployed:dev`, or `deploy-failed:dev` with a comment when the deployment fails;
  a release milestone's description notes the deployment. The first run labels everything already deployed. The
  test and production workflows will run the same job (#393).
- ADR-0030 proposes how the desktop companion pairs and uploads: a code shown on the companion and confirmed on the
  website, a revocable device token kept hashed by the API and encrypted with Windows DPAPI on the computer, expiry
  after 180 days unused, snapshot ids against replay, and a keyed fingerprint of the WoW account as conflict
  evidence; the companion threat model lists each threat and what remains (#363).
- The `deploy-dev` workflow deploys `main` to the dev environment after each merge: it builds the images, copies them
  to the server over SSH with the dev deploy key, writes `.env` from the `dev` secrets, applies the migrations with the
  new API image (`--Database:MigrateAndExit=true`), starts the new version, smoke-tests it through Caddy, and puts the
  previous version back on any failure; unused images are removed (#389).
- The AppHost publishes the Compose project each deployed environment runs: the website and API under the names the
  shared Caddy forwards to, on the external `web` network, with no published port; forwarded headers, the
  environment name and the Data Protection volume on; PostgreSQL capped, on a volume with a fixed name; no dashboard.
  A test publishes it and checks that its `.env` template holds no value (#444).
- The website keeps its Data Protection keys in the directory set by `DataProtection:KeysPath`, so a deployed
  website keeps its sessions across deployments; tests prove that forwarded headers make Discord sign-in return over
  HTTPS behind Caddy, and that the deployed `Dev` environment keeps the API reference off (#388).
- ADR-0029 proposes PostgreSQL instead of SQL Server, with one instance per environment, so dev, test and production
  fit on the shared OVH server (about 2.8 GB of 3.9 GB planned); ADR-0027's database and memory sections point to it
  (#410).
- `project-automation.md` lists what is fully automated and what still needs the owner or the agent, with the work
  item for each gap; the Sprint 5 record lists the selections of 3 October; `raidmanager-board-operations` records the
  pitfalls of unchained commands, creation scripts that aren't safe to rerun, and the Project add racing the sub-issue
  link (#437).
- `raidmanager-board-operations` keeps closing keywords out of pull request prose and checks, right after opening a
  pull request, that GitHub links exactly its task (#422).
- `raidmanager-conventions` gives owner steps one at a time with what, where, why and what to expect, keeps the
  agent off the owner's server, fixes the word list order rule, and requires the script tests before pushing;
  `raidmanager-board-operations` records how to reopen a task that a merge closed before its server verification
  (#416).
- The public API hostnames of every environment serve only the desktop companion's routes under `/companion/` and
  answer 404 to everything else, including `/internal/...`; the website reaches its API inside Docker (#387).
- `deploy/server/provision.sh` prepares the shared OVH server for the dev, test and production environments: no `root`
  sign-in, and keys-only SSH when asked with `--keys-only` after a key sign-in; the firewall, security upgrades, swap,
  the `deploy` user, and six RaidManager sites in the shared Caddy, which answer 503 until each environment is
  deployed. ADR-0027 is amended for the server already hosting other applications and for the three environments,
  and "Provision the server" explains the steps (#387).
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

- The skills keep the background sync and self-hosted runner session's lessons: a section on the self-hosted runners
  (labels, fork guard, Python, no `sudo`, `setup-tools`, the review job's checkout, `actionlint`), Linux runs in
  Docker, when to list changed files, two more SonarCloud findings, test doubles that take their time from the test's
  clock, hosted services that need Windows-only services, and the owner's choices of this session (#572).
- Every GitHub Actions job now runs on the owner's self-hosted runner (`self-hosted`, `linux`, `pc-personal`) instead of
  `ubuntu-latest`, and the jobs a pull request starts skip pull requests from forks, so a fork's code never runs on
  that machine. The docs check enforces both (ADR-0034, #562).
- The skills keep the snapshot upload session's lessons: give the scripts their Python packages from the scratchpad,
  restore the Pivot packages from the local cache when the feed credentials are missing, list changed files without
  `git add -N`, take the Radzen version from the package versions in the mockup comparison, start a fake clock at the
  real time when the domain checks the real clock, wait for the `sonar` check rather than every check, say the merge
  closes the task before its evidence comment, and offer the owner's repeated choices (split a task by intent, store
  what a source reports and ask about the values it lacks) as the recommended options (#555).
- The skills keep the character profiles session's lessons: check each pull request of a handover before acting on
  it, set the domain's default in a generated migration, give each owner its own owned instance, keep test
  names unique on the shared PostgreSQL fixture, avoid parentheses in cucumber expressions, leave regenerated
  `.feature.cs` files of untouched features out, use the compact card title on boards with many cards, quote
  the coverage numbers of CI, and offer the owner's repeated choices (split a whole-story task by viewing and editing,
  list every item, check a website change on dev after the merge) as the recommended options (#548).
- The skills keep the Avalonia companion session's lessons: give the owner's check steps before the review texts when a
  pull request needs a check, check every fact a description states, never race a fake clock, name a mockup script's
  repeated texts before pushing, guard scratch scripts, and the Avalonia notification and Microsoft Store facts (#543).
- The skills hold the companion session's lessons: run the Sonar gate only after the `sonar` check finished, compare a
  page with its mockup through headless Chrome when the browser pane is too small, never pass `class` to a shared
  component, keep a sidebar entry lit on the pages under it with `MatchesSubpaths`, give class value objects their equality
  operators, and add the boards for states a mockup lacks before building them (#518).
- "New session" from the owner starts a fixed routine in `AGENTS.md` and `raidmanager-conventions`: a checkpoint, the
  session's lessons recorded through a work item and one pull request, and a handover message with fixed parts. The
  addon skill holds what Warmane's client was seen to answer, the in-game check with the contract checker and the
  version bump per capture change; the board skill holds how features with contract gaps and defects found after a
  merge are handled (#511).
- A merge deploys dev only when it changes a file that can affect the deployment: `src/`, `deploy/`, the build files
  or `deploy-dev.yml`, compared with the last successful dev deployment. Otherwise `deploy-dev` skips the build and
  the deployment and still marks the delivered items `deployed:dev`; a run started by hand always deploys. The
  specification records it as amendment A10 (#492).
- The workflow skill hands over one pull request at a time or states the merge order, and brings a branch up to date
  by merging `main` instead of a forced push; the board skill lists the out-of-date pull request and the missing closing
  link of a workflow-written description among its pitfalls (#485).
- Dependabot sends the week's GitHub Actions updates as one grouped pull request, with one task and one review,
  instead of a pull request per action (#480).
- The skills keep the session's lessons: rendering Mermaid without hiding errors, keeping command-line text away
  from the commands a script runs (Sonar S8705), the required sections of a pull request description, creating a
  Project view, and noting the first real failed dev deployment on #392 (#456).
- The Discord sign-in sequence now shows the built, website-owned flow: the website runs the Discord exchange, gets
  the local user id from the API's internal route with its website key, looks up communities and pending claims,
  and sets its own cookie; the denied, failed and review branches are drawn (#100).
- ADR-0028 now keeps the secrets of the dev, test and production environments in three GitHub environments: `main`
  deploys to dev only, release tags to test and production only, one deploy key and one Discord application per
  environment, and the database password for PostgreSQL. The README's deployment notes cover the three environments
  (#386).
- RaidManager stores its data in PostgreSQL instead of SQL Server (ADR-0029): the persistence project uses
  Pivot.Framework's PostgreSQL package, the AppHost runs `postgres`, one fresh migration replaces the six SQL Server
  ones, and the tests run against PostgreSQL 18.3 containers. Local data from the SQL Server volume isn't carried
  over (#411).
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

- The workflows start fewer runs on the owner's runners with the same results: `docs` no longer runs again when a draft
  is marked ready, a pull request's newer commit or description cancels the older `ci`, `addon` and `docs` runs,
  `review` starts no run for the checks on `main` or for a canceled check, and the hierarchy check no longer loses an
  issue whose waiting run another issue's event replaced (#579).
- The workflows' `setup-tools` step now installs its pinned GitHub CLI and `jq` when the runner's are older, not only
  when they are missing: an older `gh` in the runner image made the review merge of #566 fail (#568).
- The dev deployment works again on the self-hosted runners: its `changes` and `record` jobs called `python`, which
  only GitHub's image had, so the first deployment after the switch failed. Every job that calls Python now sets it
  up, and the docs check fails on one that doesn't (#565).
- A test of the paired companions list failed on every run since 2026-10-05, failing the build of every pull request:
  its test double stamped companions with the real clock while the test's clock is fixed. The doubles now pair their
  companions at a fixed date (#558).
- After the player confirms a code, the paired companions list now shows the computer without a refresh: the companion
  creates its row when it collects its token at its next poll, a few seconds after the confirmation, so the list reloads
  every 2 seconds, for up to 30 seconds, until the computer is listed (#527).
- A running companion now notices that its computer was revoked: while paired it checks its token every 5 minutes and
  whenever the player opens it from the tray or starts it again, and a refusal shows "Uploads stopped" and forgets the
  token; a check that can't reach RaidManager keeps the pairing. Before, it checked only when its process started, and
  closing the window doesn't end the process (#525).
- The snapshot contract says a raid's `difficulty` is read with its `maxPlayers`: a raid with a single size, such
  as `Gruul's Lair`, reports difficulty `1` whatever its size, as the owner's saves showed; the `raid-save` fixture
  holds one (#508).
- An equipment set's item that is gone from the character is recorded as missing without `itemId = -1`: Warmane's
  client gives `-1` instead of the item's id, and the addon no longer copies it. The addon's version is 0.3.1
  (#504).
- The snapshot contract says an item string's gem fields hold gem enchantment ids, as the game writes them, not the
  gems' item ids, and its fixtures use such ids; the owner's check in the game showed the difference (#500).
- Each merge starts `ci`, `docs`, `docs-publish` and `deploy-dev` on `main` once: a later review run of a pull
  request already merged now stops, instead of dispatching them, and deploying dev, a second time (#494).
- The description written for a Dependabot update says it changes the action's version, true for a tag such as
  `@v4` as for a commit id, instead of always mentioning a pinned commit id (#477).
- A pull request description saved in the browser, which GitHub stores with `\r\n` line endings, keeps its
  `Closes #<task>` line: `validate` finds it, and the review workflow closes that task when it merges (#475).
- The 37 SonarCloud findings open on `main` are fixed (#365):
  - The website, API and bot `Dockerfile` images run as the image's non-root `app` user.
  - The documentation workflows pin the Lychee and Vale actions to full commit ids, install MkDocs from wheels only,
    and run `npx` without package scripts.
  - The scripts no longer act on any path or commit text from their command line. `penpot_render.py` renders only
    files in `docs/mockups/`, and `build_wiki.py --write` writes to `site/wiki/`. `coverage_gate.py` reads
    `TestResults`, `--summary` writes `coverage-summary.md` at the repository root, and `--base` must be a ref or a
    full commit id the repository has.
  - The remaining code smells (complex functions, nested conditionals, slow regular expressions, repeated literals)
    are split or simplified without changing behavior.

- A failed deployment's rollback no longer restarts the running version: `deploy-dev` tags each run's images
  uniquely, so a rebuild of the same commit never moves the running version's tag (#450).
- `provision.sh` no longer lists the SSH rule among the firewall rules that aren't RaidManager's: `ufw` names it
  `OpenSSH`, which the filter didn't recognize (#418).
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
