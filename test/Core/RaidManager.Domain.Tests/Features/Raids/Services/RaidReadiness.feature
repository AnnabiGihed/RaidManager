Feature: Raid-start readiness
  As a player
  I want each target of a combined raid checked at raid start
  So that I cannot accidentally offer a character that is still locked

  Background:
    Given a raid starting in 48 hours requiring these targets
      | instance        | difficulty       |
      | IcecrownCitadel | TwentyFivePlayer |
      | RubySanctum     | TwentyFivePlayer |
    And the raid accepts lockout evidence up to 24 hours old

  Rule: Every required target receives its own verdict at raid start

    Scenario: Fresh evidence without a matching save is available
      Given the character "Frostmourne" synchronized 1 hours ago without saves
      When the character "Frostmourne" is assessed
      Then the IcecrownCitadel TwentyFivePlayer verdict is Available
      And the RubySanctum TwentyFivePlayer verdict is Available
      And the overall verdict is Available

    Scenario: A save that resets before raid start is allowed with its reset time
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance        | difficulty       | resetsHoursFromRaidStart | extended |
        | IcecrownCitadel | TwentyFivePlayer | -12                      | false    |
      When the character "Frostmourne" is assessed
      Then the IcecrownCitadel TwentyFivePlayer verdict is ResetsBeforeRaid
      And the IcecrownCitadel TwentyFivePlayer verdict shows a reset 12 hours before raid start
      And the overall verdict is ResetsBeforeRaid

    Scenario: A save that lasts past raid start is locked through the raid
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance    | difficulty       | resetsHoursFromRaidStart | extended |
        | RubySanctum | TwentyFivePlayer | 24                       | false    |
      When the character "Frostmourne" is assessed
      Then the RubySanctum TwentyFivePlayer verdict is LockedThroughRaid
      And the IcecrownCitadel TwentyFivePlayer verdict is Available
      And the overall verdict is LockedThroughRaid

    Scenario: An extended save is never treated as expiring before the raid
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance        | difficulty       | resetsHoursFromRaidStart | extended |
        | IcecrownCitadel | TwentyFivePlayer | -12                      | true     |
      When the character "Frostmourne" is assessed
      Then the IcecrownCitadel TwentyFivePlayer verdict is NeedsFreshSync
      And the overall verdict is NeedsFreshSync

  Rule: Missing or stale evidence needs a fresh sync, never Available

    Scenario: A character without addon evidence needs a fresh sync
      Given the character "Frostmourne" was never synchronized
      When the character "Frostmourne" is assessed
      Then the IcecrownCitadel TwentyFivePlayer verdict is NeedsFreshSync
      And the RubySanctum TwentyFivePlayer verdict is NeedsFreshSync
      And the overall verdict is NeedsFreshSync

    Scenario: Evidence older than the accepted age needs a fresh sync
      Given the character "Frostmourne" synchronized 30 hours ago without saves
      When the character "Frostmourne" is assessed
      Then the overall verdict is NeedsFreshSync

  Rule: A combined raid takes the most restrictive target

    Scenario: A locked target outweighs an unknown target
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance        | difficulty       | resetsHoursFromRaidStart | extended |
        | IcecrownCitadel | TwentyFivePlayer | -12                      | true     |
        | RubySanctum     | TwentyFivePlayer | 24                       | false    |
      When the character "Frostmourne" is assessed
      Then the overall verdict is LockedThroughRaid

    Scenario: An unknown target outweighs a save that resets before the raid
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance        | difficulty       | resetsHoursFromRaidStart | extended |
        | IcecrownCitadel | TwentyFivePlayer | -12                      | false    |
        | RubySanctum     | TwentyFivePlayer | -12                      | true     |
      When the character "Frostmourne" is assessed
      Then the overall verdict is NeedsFreshSync

  Rule: A confirmed active lock blocks signup and roster assignment

    Scenario: A player cannot offer a character locked through the raid
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance    | difficulty       | resetsHoursFromRaidStart | extended |
        | RubySanctum | TwentyFivePlayer | 24                       | false    |
      When "Arthas" signs up offering "Frostmourne"
      Then the signup is rejected

    Scenario: A player can offer a different eligible character instead
      Given the character "Frostmourne" synchronized 1 hours ago with these saves
        | instance    | difficulty       | resetsHoursFromRaidStart | extended |
        | RubySanctum | TwentyFivePlayer | 24                       | false    |
      And the character "Shadowmourne" synchronized 1 hours ago without saves
      When "Arthas" signs up offering "Shadowmourne"
      Then "Arthas" has signed up offering "Shadowmourne"

    Scenario: A signup without a current assessment is rejected
      Given the character "Shadowmourne" synchronized 1 hours ago without saves
      When "Arthas" signs up offering "Shadowmourne" assessed for a different start time
      Then the signup is rejected

    Scenario: An officer cannot roster a character that became locked
      Given the character "Shadowmourne" synchronized 1 hours ago without saves
      And "Arthas" signed up offering "Shadowmourne"
      And the character "Shadowmourne" synchronized 0 hours ago with these saves
        | instance        | difficulty       | resetsHoursFromRaidStart | extended |
        | IcecrownCitadel | TwentyFivePlayer | 24                       | false    |
      When the officer selects "Shadowmourne" of "Arthas" for group 1 position 1
      Then the roster assignment is rejected
      And the roster contains no selection for "Arthas"
