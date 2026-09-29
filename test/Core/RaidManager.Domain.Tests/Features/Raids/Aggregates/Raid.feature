Feature: Raid roster selection
  As a raid leader
  I want one signup to offer several character loadouts
  So that I can choose the best configuration without roster duplication

  Background:
    Given an Icecrown Citadel 25-player raid open for signups
    And a participant has signed up offering two loadouts

  @RosterSelection
  Scenario: 1.01 Selecting one offered loadout creates one roster place
    When the raid leader selects the participant first offered loadout for group 1 position 1
    Then the raid roster should contain 1 selection for that participant

  @RosterSelection
  Scenario: 1.02 Selecting another offered loadout replaces the previous selection
    Given the participant first offered loadout is selected for group 1 position 1
    When the raid leader selects the participant second offered loadout for group 2 position 1
    Then the raid roster should contain 1 selection for that participant
    And the participant selected loadout should be the second offered loadout
