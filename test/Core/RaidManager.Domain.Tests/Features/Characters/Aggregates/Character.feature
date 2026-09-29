Feature: Character synchronization
  As a raid organizer
  I want character lockouts and loadouts to be modeled independently
  So that roster decisions use current raid-ready configurations

  Background:
    Given a level 80 Human Paladin named Paladinlol on Icecrown

  @LoadoutSynchronization
  Scenario: 1.01 Synchronizing a first loadout stores its own GearScore
    When the Retribution loadout is synchronized with GearScore 6372
    Then the character should contain 1 loadout
    And the synchronized loadout GearScore should be 6372
    And a loadout synchronized domain event should exist

  @RaidLockouts
  Scenario: 2.01 Synchronizing ICC25 creates a character-wide lockout
    When the character raid lockouts are synchronized with Icecrown Citadel 25-player
    Then the character should be saved to Icecrown Citadel 25-player
    And a raid lockouts synchronized domain event should exist
