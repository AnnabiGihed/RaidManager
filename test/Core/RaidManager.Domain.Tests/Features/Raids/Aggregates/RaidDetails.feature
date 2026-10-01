Feature: Raid details
  As a raid officer
  I want to give a raid a title and change its details before it starts
  So that players see accurate raids and readiness follows the start and targets

  Rule: A raid has a title

    Scenario: The title is trimmed
      When the officer creates a raid titled "  Weekly ICC  "
      Then the raid title is "Weekly ICC"

    Scenario: A blank title is rejected
      When the officer creates a raid titled "   "
      Then the raid change is rejected

    Scenario: A title longer than 100 characters is rejected
      When the officer creates a raid with a title of 101 characters
      Then the raid change is rejected

  Rule: Every target of a raid is for the same size

    Scenario: A combined raid takes the size of its targets
      When the officer creates a raid requiring these targets
        | instance        | difficulty             |
        | IcecrownCitadel | TwentyFivePlayerHeroic |
        | RubySanctum     | TwentyFivePlayer       |
      Then the raid is for 25 players

    Scenario: A raid mixing 10 and 25 players is rejected
      When the officer creates a raid requiring these targets
        | instance        | difficulty       |
        | IcecrownCitadel | TwentyFivePlayer |
        | RubySanctum     | TenPlayer        |
      Then the raid change is rejected

  Rule: An officer edits a raid that has not started

    Scenario: Moving the start reports a start change
      Given a draft Icecrown Citadel raid for 25 players
      When the officer moves the start one hour later
      Then the raid starts one hour later
      And the raid reports a change to its start but not its targets

    Scenario: Changing the targets reports a target change
      Given a draft Icecrown Citadel raid for 25 players
      When the officer adds Ruby Sanctum for 25 players
      Then the raid requires 2 targets
      And the raid reports a change to its targets but not its start

    Scenario: Renaming a raid open for signups reports neither
      Given a draft Icecrown Citadel raid for 25 players
      And the raid is open for signups
      When the officer renames the raid "Friday ICC"
      Then the raid title is "Friday ICC"
      And the raid reports a change to neither its start nor its targets

    Scenario: Saving identical details reports nothing
      Given a draft Icecrown Citadel raid for 25 players
      When the officer saves the raid without changes
      Then the raid reports no change

    Scenario: An invalid edit keeps the raid unchanged
      Given a draft Icecrown Citadel raid for 25 players
      When the officer moves the signup deadline after the start
      Then the raid change is rejected
      And the raid reports no change
