Feature: Companion pairing lookup
  As a player
  I want to see the computer and the expiry of a code before confirming it
  So that I pair only my own computer

  Background:
    Given a companion asked to be paired with the code "K7M4QX" 2 minutes ago

  Rule: Only a waiting pairing is described

    Scenario: A waiting pairing is described
      When the player looks up the code "K7M-4QX"
      Then the lookup shows "BRYN-DESKTOP" expiring 8 minutes from now

    Scenario: An unknown code isn't found
      When the player looks up the code "ABC-234"
      Then the lookup fails with "CompanionPairing.NotFound" as NotFound

    Scenario: A confirmed code is refused
      Given the pairing was confirmed
      When the player looks up the code "K7M-4QX"
      Then the lookup fails with "CompanionPairing.AlreadyConfirmed" as Conflict

    Scenario: An expired code is refused
      Given 10 minutes passed
      When the player looks up the code "K7M-4QX"
      Then the lookup fails with "CompanionPairing.Expired" as Conflict
