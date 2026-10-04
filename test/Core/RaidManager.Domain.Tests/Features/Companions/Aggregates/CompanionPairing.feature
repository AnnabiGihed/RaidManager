Feature: Companion pairing
  As a player
  I want to pair my companion by confirming the code it shows
  So that only a computer I approve uploads my character data

  Background:
    Given a companion asked to be paired as "BRYN-DESKTOP"

  Rule: A pairing lasts ten minutes from the companion's request

    Scenario: The pairing expires ten minutes after the request
      Then the pairing expires 10 minutes after the request

    Scenario: The pairing shows its code with a dash
      Then the pairing shows the code "K7M-4QX"

  Rule: The signed-in player confirms the pairing once, before it expires

    Scenario: Confirming a fresh pairing binds it to the player
      When "Bryn" confirms the pairing 9 minutes after the request
      Then the confirmation succeeds
      And the pairing is confirmed by "Bryn"

    Scenario: Confirming an expired pairing is refused
      When "Bryn" confirms the pairing 10 minutes after the request
      Then the confirmation fails with "CompanionPairing.Expired" as Conflict

    Scenario: Confirming a pairing twice is refused
      Given "Bryn" confirmed the pairing
      When "Mallory" confirms the pairing 1 minutes after the request
      Then the confirmation fails with "CompanionPairing.AlreadyConfirmed" as Conflict
      And the pairing is confirmed by "Bryn"

  Rule: The companion collects its token once, after the confirmation and before expiry

    Scenario: Collecting before the confirmation answers pending
      When the companion collects its token 1 minutes after the request
      Then the collection fails with "CompanionPairing.Pending"

    Scenario: Collecting after the confirmation pairs the companion for the player
      Given "Bryn" confirmed the pairing
      When the companion collects its token 1 minutes after the request
      Then the companion is paired for "Bryn" as "BRYN-DESKTOP"
      And the pairing is completed

    Scenario: Collecting a second time is refused
      Given "Bryn" confirmed the pairing
      And the companion collected its token
      When the companion collects its token 2 minutes after the request
      Then the collection fails with "CompanionPairing.Invalid"

    Scenario: Collecting after expiry is refused
      Given "Bryn" confirmed the pairing
      When the companion collects its token 11 minutes after the request
      Then the collection fails with "CompanionPairing.Expired"

  Rule: The computer label is kept short and never blank

    Scenario Outline: The label is normalized
      When a companion asks to be paired as "<label>"
      Then the pairing's computer label is "<kept>"

      Examples:
        | label                                                                            | kept                                                             |
        | BRYN-LAPTOP                                                                      | BRYN-LAPTOP                                                      |
        |                                                                                  | Unnamed computer                                                 |
        | A-COMPUTER-NAME-THAT-IS-FAR-LONGER-THAN-THE-SIXTY-FOUR-CHARACTERS-THE-LIST-KEEPS | A-COMPUTER-NAME-THAT-IS-FAR-LONGER-THAN-THE-SIXTY-FOUR-CHARACTER |
