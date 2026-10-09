Feature: Companion token
  As a player
  I want only my active companions to upload
  So that a revoked or forgotten computer can't send my character data

  Background:
    Given a companion paired for "Bryn"

  Rule: An active companion is admitted and its use is recorded at most hourly

    Scenario: A request within the hour is admitted without a write
      When the companion calls 59 minutes after its last use
      Then the companion is admitted
      And its last use is unchanged

    Scenario: A request after an hour records the use
      When the companion calls 60 minutes after its last use
      Then the companion is admitted
      And its last use moves to the request

  Rule: A companion unused for 180 days expires

    Scenario: A companion used within 180 days is active
      When the companion calls 180 days after its last use
      Then the companion is admitted

    Scenario: A companion unused for longer than 180 days is refused
      When the companion calls 181 days after its last use
      Then the companion is refused with "Companion.Expired"
      And its status is Expired

  Rule: The player revokes a companion once, and it is refused at once

    Scenario: A revoked companion is refused
      Given "Bryn" revoked the companion
      When the companion calls 1 minutes after its last use
      Then the companion is refused with "Companion.Revoked"
      And its status is Revoked

    Scenario: Revoking records the time and raises an event
      When "Bryn" revokes the companion
      Then the revocation succeeds

    Scenario: Another player can't see or revoke the companion
      When "Mallory" revokes the companion
      Then the revocation fails with "Companion.NotFound" as NotFound
      And its status is Active

    Scenario: Revoking twice is refused
      Given "Bryn" revoked the companion
      When "Bryn" revokes the companion
      Then the revocation fails with "Companion.AlreadyRevoked" as Conflict

  Rule: A companion's last upload is the latest upload it made

    Scenario: A companion that never uploaded has no last upload
      When the companion calls 1 minutes after its last use
      Then the companion has no last upload

    Scenario Outline: An upload moves the last upload forward only
      Given the companion uploaded 2 hours after its pairing
      When the companion uploads <hours> hours after its pairing
      Then its last upload is <latest> hours after its pairing

      Examples:
        | hours | latest |
        | 3     | 3      |
        | 1     | 2      |

  Rule: The player can ask a companion to send every character again

    Scenario: Asking to sync again records the request's time
      When "Bryn" asks the companion to sync again 2 hours after its pairing
      Then the companion's request to sync again is 2 hours after its pairing
