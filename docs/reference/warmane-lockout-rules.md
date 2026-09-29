# Warmane raid lockout evidence

This evidence review supports [raid-start readiness](../explanation/state-models.md#raid-start-readiness) and
[issue #40](https://github.com/AnnabiGihed/RaidManager/issues/40).
It records what could be verified on 29 September 2026. It is not a complete executable rule set.
No in-game observation was available during this review.

## Evidence standard

- **Confirmed:** A Warmane staff statement or a reproducible observation, scoped to its date and named realm.
  Historical confirmation does not prove the current configuration is unchanged.
- **Reported:** A dated player observation on Warmane's own forum, without current realm confirmation.
- **Unknown:** No evidence meeting the confirmed standard. Do not infer availability from this entry.

The client saved-instance list is the authority for a character's observed save and remaining time.
A calendar rule may help explain an expected reset, but it cannot replace a fresh, complete scan.
The current domain evaluator compares exact difficulty values and does not apply realm reset or shared-difficulty rules.
Do not use this page to claim that the application already enforces the matrix below.

## Realm reset schedule

| Realm | Raid reset time | Evidence | Readiness rule status |
| --- | --- | --- | --- |
| Icecrown | Wednesday 04:00 server time, **reported in 2017 and 2019**. | [2017 report](https://forum.warmane.com/showthread.php?t=351559), [2019 report](https://forum.warmane.com/showthread.php?t=397056) | **Unknown now.** Server time to UTC conversion and instance exceptions remain unverified. |
| Lordaeron | Wednesday 04:00 server time, **reported in 2017**. | [2017 report](https://forum.warmane.com/showthread.php?t=343399) | **Unknown now.** The report does not establish a current UTC instant or per-instance cadence. |
| Onyxia | No validated schedule. | No qualifying source or observation found. | **Unknown.** Its progression and raid cadence require realm-specific evidence. |
| Blackrock | No validated raid schedule or supported player raid set. | No qualifying source or observation found. | **Unsupported for automatic raid readiness** until raid availability is confirmed. |

Warmane [staff acknowledged](https://forum.warmane.com/showthread.php?t=457620) a raid-ID reset incident in 2023.
This shows why a scheduled reset cannot overrule a character's actual saved-instance evidence.
The reports above do not establish daylight-saving behavior or whether all instances reset together.

## Difficulty and instance matrix

| Instance and size | Normal and Heroic lockout | Evidence | Status |
| --- | --- | --- | --- |
| Icecrown Citadel, 10 players | Shared ID. | [Warmane staff discussed the shared lockout and difficulty switching in 2016](https://forum.warmane.com/showthread.php?p=2744145); a [2025 staff reply](https://forum.warmane.com/showthread.php?t=481641) refers to the continuing single weekly lockout. A [realm report](https://forum.warmane.com/showthread.php?t=342958) names the 10-player pairing. | **Historically confirmed shared difficulty**; current behavior needs a named-realm observation. |
| Icecrown Citadel, 25 players | Shared ID. | Same sources as above. | **Historically confirmed shared difficulty**; current behavior needs a named-realm observation. |
| Icecrown Citadel, 10 versus 25 players | Separate IDs, as reported. | [2016 realm report](https://forum.warmane.com/showthread.php?t=342958). | **Reported**, not confirmed per realm. |
| Ruby Sanctum, each size | Normal and Heroic share an ID, as reported. | [Warmane player report](https://forum.warmane.com/showthread.php?t=391214). | **Reported**, not confirmed per realm. |
| Trial of the Crusader, each size | Normal and Heroic have separate IDs, as reported. | [Warmane player report](https://forum.warmane.com/showthread.php?t=391214). | **Reported**, not confirmed per realm. |
| Other modeled raids | Size and difficulty sharing has no complete current matrix. | [Player discussion](https://forum.warmane.com/showthread.php?t=438424) describes Wrath of the Lich King examples. | **Unknown** per instance and realm. |

An Icecrown Citadel save cannot be declared free solely because the target's Normal/Heroic enum value differs.
Warmane staff discussed changing difficulty inside one Icecrown Citadel run in 2016.
It does not establish that a character with one saved ID can join an unrelated group's partially cleared ID.

## Extensions and encounter progress

| Question | Finding | Status |
| --- | --- | --- |
| Can a character extend a saved raid ID? | [A Warmane player describes extension](https://forum.warmane.com/showthread.php?t=449324), but no current staff rule or observed before/after save was found. | **Unknown** per realm and instance. |
| Does an extension survive the displayed reset? | No qualifying observation was found. | **Unknown**; an extended save must never be treated as expired from a calendar alone. |
| Can a character join another ID with different bosses killed? | No current realm-specific compatibility rule was found. | **Unknown**; encounter progress must not imply free access. |
| Does Normal/Heroic switching create a second boss kill? | Icecrown Citadel shares one ID, but the exact encounter-progress payload still needs an observation. | **Unknown** for automated per-boss eligibility. |

## Observation needed to promote a rule

Record one observation per realm, instance, size, and difficulty combination that the product will support:

1. Capture the realm name, server clock, client build, UTC observation time, and the character's saved-instance list.
1. Record the instance ID, displayed difficulty, remaining reset duration, extension state, and killed encounters.
1. Repeat immediately before and after the claimed reset; convert both observations to UTC.
1. For shared difficulty, compare the ID before and after switching Normal and Heroic inside the same size.
1. For encounter compatibility, attempt entry with a second group whose ID has different progress and record the result.
1. Repeat a reset crossing a daylight-saving change before publishing a fixed server-time-to-UTC rule.

Keep the raw screenshots or addon snapshots with their timestamps and character details redacted.
Until a combination has that evidence, return **Needs fresh sync** for an unknown rule rather than **Available**.
This does not change the existing rule that a confirmed active lock blocks signup and roster selection.
