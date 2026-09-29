Feature: Raid-save scans
  As a player
  I want only complete saved-instance scans to change my raid saves
  So that a partial or late upload never hides an active lock

  Background:
    Given a character saved to IcecrownCitadel TwentyFivePlayer by a complete scan 2 hours ago

  Rule: A complete scan replaces the raid saves, even when it finds none

    Scenario: A complete empty scan clears the raid saves
      When a complete scan without saves is recorded 1 hours ago
      Then the scan is accepted
      And the character has no raid saves
      And the last complete scan was 1 hours ago

  Rule: An incomplete scan is recorded without touching the raid saves

    Scenario: An incomplete scan keeps the last complete saves
      When an incomplete scan is recorded 1 hours ago
      Then the character is saved to IcecrownCitadel TwentyFivePlayer
      And the last complete scan was 2 hours ago
      And the last incomplete scan was 1 hours ago

  Rule: An older complete scan cannot replace newer raid saves

    Scenario: An out-of-order complete scan is rejected
      When a complete scan without saves is recorded 3 hours ago
      Then the scan is rejected as outdated
      And the character is saved to IcecrownCitadel TwentyFivePlayer
      And the last complete scan was 2 hours ago
