Feature: Raid targets
  As a raid officer
  I want a raid to list every instance and difficulty it requires
  So that readiness can be checked for each target of a combined raid

  Rule: A raid requires every listed target once

    Scenario: A combined raid keeps both required targets
      When the officer creates a raid requiring these targets
        | instance        | difficulty       |
        | IcecrownCitadel | TwentyFivePlayer |
        | RubySanctum     | TwentyFivePlayer |
      Then the raid requires 2 targets
      And the raid created event lists 2 targets

    Scenario: A raid without targets is rejected
      When the officer creates a raid without targets
      Then the raid creation is rejected

    Scenario: A repeated target is rejected
      When the officer creates a raid requiring these targets
        | instance        | difficulty       |
        | IcecrownCitadel | TwentyFivePlayer |
        | IcecrownCitadel | TwentyFivePlayer |
      Then the raid creation is rejected
