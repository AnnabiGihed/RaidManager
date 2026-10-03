# Version 1 domain model

The domain model explains business concepts and their relationships.
It extends the current C# foundation to cover the agreed version 1 workflows.
These are conceptual types; a box does not imply an implemented class, a table, or a separate aggregate.

A multiplicity such as `0..*` means zero or more; `1..*` means one or more.
A filled diamond means the child belongs to that concept's lifecycle.
Each composed child has one owning parent; repeated parent-side `1` labels are omitted where they would overlap.
Other relationships refer to identities rather than requiring an in-memory object graph.

## Domain overview

A community organizes raids, a user offers character loadouts, and an officer selects one option per player.
Character ownership and saved-instance state remain independent of the selected loadout.

[![Domain overview connecting users, communities, characters, raids, signups, and compositions](../diagrams/domain-overview.svg)](../diagrams/domain-overview.svg)

Source: [Mermaid](../diagrams/domain-overview.mmd).

## Identity and community

A community is a linked Discord server. The user who added the bot is its Administrator. The community holds its roles,
starting with the Officer and Raid leader presets, each allowing a set of permissions, and its role mappings say which
Discord roles give which roles ([ADR-0024](../adr/0024-give-each-community-its-own-roles.md)). Membership and each
member's Discord roles stay in Discord: what a member may do is read from Discord when an action needs it
([ADR-0022](../adr/0022-check-discord-roles-at-action-time.md)).
That same user can authorize companion pairings and request character claims across multiple game accounts.

[![Identity model with community membership, Discord identity, companion pairings, and claims](../diagrams/domain-identity.svg)](../diagrams/domain-identity.svg)

Source: [Mermaid](../diagrams/domain-identity.mmd).

## Characters and observations

Pairings authorize uploads, snapshots preserve observation evidence, and claims control approval.
Gear belongs to a loadout; raid saves belong to a character.

[![Character model with pairings, snapshots, claims, loadouts, equipment, and raid lockouts](../diagrams/domain-characters.svg)](../diagrams/domain-characters.svg)

Source: [Mermaid](../diagrams/domain-characters.mmd).

| Concept | Responsibility and invariant |
| --- | --- |
| User | Local identity linked to Discord. One user may own characters on several WoW accounts. |
| `CompanionPairing` | Revocable permission for a companion to upload for one user. Credentials stay outside addon files. |
| `CharacterSnapshot` | Versioned observation from a specific source. Duplicate or older input must not regress current facts. |
| `ObservationMetadata` | Source, observation time, and receipt time. Freshness uses observation time rather than upload time alone. |
| `CharacterClaim` | Pending, approved, rejected, or conflicted request to associate a character with a user. |
| Character | Realm and character identity, approved owner, and character-wide raid saves. |
| Loadout | Spec, role, gear, GearScore, talents, glyphs, stats, and their provenance. |
| `RaidLockout` | Instance, difficulty, lockout identifier, reset, extension, and available progress evidence. |

An empty lockout collection is meaningful only with evidence of a complete and fresh scan.
An omitted save list or failed scan remains unknown.
Manual facts need their own source label, including when the current code's source enum does not yet support them.

## Raid planning

A raid has required targets and one response per user.
Alternative compositions, bench entries, boss assignments, and attendance refer back to the same participants.

[![Raid domain model with templates, recurrence, signup options, compositions, readiness, and attendance](../diagrams/domain-raids.svg)](../diagrams/domain-raids.svg)

Source: [Mermaid](../diagrams/domain-raids.mmd).

| Concept | Responsibility and invariant |
| --- | --- |
| `RaidTemplate` and `RecurrenceRule` | Reusable settings and the local-time rule for creating distinct raid occurrences. |
| `RaidTarget` | One required instance and difficulty in a possibly combined raid. |
| `RaidSignup` | One response for a raid and user, regardless of whether it was edited on the website or in Discord. |
| `SignupOption` | An approved character and loadout offered by that user, with notes and preference where applicable. |
| `SignupPreset` | Reusable choices; loading a preset still requires current ownership and readiness checks. |
| `RaidComposition` | A draft or published alternative with a revision for reviewing changes. |
| `RosterSelection` | One selected option per user in a composition; a roster position cannot be occupied twice. |
| `BenchEntry` | A substitute candidate, distinct from a selected participant and from availability. |
| `BossAssignment` | Encounter-specific responsibility associated with a selected participant. |
| `EligibilityAssessment` | Derived verdict for a character, target, start time, and current evidence. |
| `OfficerException` | Visible reason attached to an unknown-data assessment. It cannot authorize a confirmed active lock. |
| `AttendanceRecord` | What happened for a player at a raid, distinct from their earlier intended availability. |

Publication validates each selected option against all required targets.
A new observation or changed schedule invalidates the relevance of the old assessment and requires re-evaluation.
Reusing an officer exception against changed evidence requires renewed review.

## Relationship to current code

The current types are under `src/Core/RaidManager.Domain/Features/`, grouped by domain area.
The diagrams above describe the version 1 target model.
A diagram concept is not an implemented class merely because it appears there.

| Current foundation | Version 1 design extension |
| --- | --- |
| `Identity/Aggregates/User.cs` and `Communities/Aggregates/Community.cs` exist. The `SignInWithDiscord` command resolves a Discord account to one persisted user, whose Discord id is unique. A community stores its server, realm, Administrator and Discord role mappings, one per Discord server, and gives a member's role from their Discord roles. | The Discord OAuth flow, sessions, pairing and permission workflows need implementation. |
| `Characters/Aggregates/Character.cs` owns `CharacterClaim` entries with the pending, approved, rejected and conflict lifecycle; uploads never transfer ownership. The application commands `ApproveCharacterClaim` and `RejectCharacterClaim` apply a player's decision, and `GetPendingCharacterClaims` lists the claims awaiting it. | The API, ownership evidence, conflict resolution and the mandatory approval page are planned. |
| `Characters/Aggregates/Loadout.cs` and raid lockouts exist. Only a complete saved-instance scan replaces the raid saves; an incomplete scan is recorded without erasing them, and an older complete scan is rejected. | The addon and companion complete-scan marker, snapshot history, source-specific freshness and manual-source handling are planned. |
| `Raids/Aggregates/Raid.cs` requires one or more distinct `RaidTarget` instance and difficulty pairs, all for one size. A raid is stored in PostgreSQL with its targets, signups and roster selections. | Recurrence, composition revisions and re-evaluation are planned. |
| `Raids/Services/RaidReadinessEvaluator.cs` gives each target a `ReadinessVerdict` at raid start; `CharacterReadiness` takes the most restrictive, and `Raid` rejects a locked character for signup or roster. | Validated realm reset rules, difficulty sharing, snapshot provenance and officer exceptions are planned. |
| `Raids/Aggregates/RaidSignup.cs` stores options and a confirmed, tentative, late or declined availability; selection no longer changes it. | Withdrawal, presets, preferred option, notes and bench entries are planned. |
| `Raids/Aggregates/RosterSelection.cs` supports one selection per user. | Multiple draft compositions, publication review, boss assignments and attendance are planned. |

Users, characters, communities and raids are stored with EF Core; each aggregate's mapping is in the
`Configurations` folder of its feature in `RaidManager.Persistence.EntityFrameworkCore`. No storage diagram is drawn
yet: generate one from the real schema rather than drawing it by hand.
