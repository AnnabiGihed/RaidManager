Feature: Raid signup availability
  As a player
  I want my availability to stay my own answer
  So that officer roster decisions never rewrite what I said

  Background:
    Given an Icecrown Citadel raid open for signups

  Rule: A late signup states its expected arrival

    Scenario: A late signup records the arrival time
      When "Alice" signs up late arriving 30 minutes after raid start
      Then the availability of "Alice" is "Late"
      And the signup of "Alice" expects arrival 30 minutes after raid start

    Scenario: A late signup without an arrival time is rejected
      When "Alice" signs up late without an arrival time
      Then the signup is rejected

    Scenario: An arrival time on a signup that is not late is rejected
      When "Alice" signs up as "Confirmed" with an arrival time
      Then the signup is rejected

  Rule: Only a declined signup may offer no character

    Scenario: A declined signup needs no character
      When "Alice" declines without offering a character
      Then the availability of "Alice" is "Declined"

    Scenario: A confirmed signup without a character is rejected
      When "Alice" signs up as "Confirmed" without offering a character
      Then the signup is rejected

  Rule: Roster selection leaves the player's availability unchanged

    Scenario: A tentative player stays tentative after selection
      Given "Alice" signed up as "Tentative" offering one loadout
      When the officer selects the offered loadout of "Alice" for group 1 position 1
      Then the availability of "Alice" is "Tentative"
      And the roster contains 1 selection for "Alice"
