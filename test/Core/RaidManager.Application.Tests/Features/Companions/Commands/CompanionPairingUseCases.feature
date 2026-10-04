Feature: Companion pairing use cases
  As a player
  I want my companion paired, checked and revoked through one flow
  So that only the computers I confirm upload for me

  Background:
    Given "Bryn" is a registered player

  Rule: Starting a pairing draws a code no live pairing shows

    Scenario: A code in use is replaced by a free one
      Given the next codes drawn are "K7M4QX" then "ABC234"
      And the code "K7M4QX" is in use
      When a companion starts pairing as "BRYN-DESKTOP"
      Then the pairing shows "ABC-234"
      And the pairing is committed

    Scenario: A generator that only draws codes in use is a fault
      Given the next codes drawn are "K7M4QX" then "K7M4QX"
      And the code "K7M4QX" is in use
      When a companion starts pairing as "BRYN-DESKTOP" expecting a fault
      Then the start fails with a fault

    Scenario: A failed commit is returned from the start
      Given the next codes drawn are "K7M4QX" then "ABC234"
      And the commit will fail
      When a companion starts pairing as "BRYN-DESKTOP"
      Then the use case fails with "Commit.Failed" as ValidationError

  Rule: Confirming needs a known player and a pairing that shows the code

    Scenario: An unknown player can't confirm
      Given a companion waits with the code "K7M4QX"
      When "Mallory" confirms the code "K7M-4QX"
      Then the use case fails with "User.NotFound" as NotFound
      And nothing is committed

    Scenario: An unknown code isn't found
      When "Bryn" confirms the code "ABC-234"
      Then the use case fails with "CompanionPairing.NotFound" as NotFound
      And nothing is committed

    Scenario: A confirmed code is committed
      Given a companion waits with the code "K7M4QX"
      When "Bryn" confirms the code "k7m-4qx"
      Then the use case succeeds
      And the pairing is committed

    Scenario: A code confirmed twice isn't committed again
      Given a companion waits with the code "K7M4QX"
      And "Bryn" confirmed the code "K7M4QX"
      When "Bryn" confirms the code "K7M4QX"
      Then the use case fails with "CompanionPairing.AlreadyConfirmed" as Conflict
      And nothing is committed

  Rule: The companion collects its token once its player confirmed the code

    Scenario: An unknown device code is invalid
      When the companion collects its token with an unknown device code
      Then the use case fails with "CompanionPairing.Invalid" as ValidationError
      And nothing is committed

    Scenario: A pending pairing isn't committed
      Given a companion waits with the code "K7M4QX"
      When the companion collects its token
      Then the use case fails with "CompanionPairing.Pending" as ValidationError
      And nothing is committed

    Scenario: A confirmed pairing gives the token with the player's name
      Given a companion waits with the code "K7M4QX"
      And "Bryn" confirmed the code "K7M4QX"
      When the companion collects its token
      Then the companion receives a token for "Bryn"
      And the companion is committed

    Scenario: A player removed after confirming isn't paired
      Given a companion waits with the code "K7M4QX"
      And "Bryn" confirmed the code "K7M4QX"
      And "Bryn" no longer exists
      When the companion collects its token
      Then the use case fails with "User.NotFound" as NotFound
      And nothing is committed

  Rule: Every companion request is checked by its token

    Scenario Outline: A missing or unknown token is refused
      When a companion calls with the token "<token>"
      Then the use case fails with "Companion.TokenUnknown" as AuthenticationRequired

      Examples:
        | token         |
        |               |
        | unknown-token |

    Scenario: A paired companion is admitted and its first use in an hour is committed
      Given a companion paired for "Bryn" 2 hours ago
      When the companion calls with its token
      Then the companion is admitted for "Bryn"
      And the companion is committed

    Scenario: A failed commit of last use is returned
      Given a companion paired for "Bryn" 2 hours ago
      And the commit will fail
      When the companion calls with its token
      Then the use case fails with "Commit.Failed" as ValidationError

    Scenario: A revoked companion is refused without a commit
      Given a companion paired for "Bryn" 2 hours ago
      And "Bryn" revoked the companion
      When the companion calls with its token
      Then the use case fails with "Companion.Revoked" as AuthenticationRequired
      And nothing is committed

  Rule: The player lists and revokes their companions

    Scenario: The list shows each companion's status
      Given a companion paired for "Bryn" 2 hours ago
      And "Bryn" revoked the companion
      When "Bryn" lists their companions
      Then the list shows one companion with the status Revoked

    Scenario: Revoking an unknown companion isn't found
      When "Bryn" revokes an unknown companion
      Then the use case fails with "Companion.NotFound" as NotFound
      And nothing is committed

    Scenario: Revoking another player's companion isn't found
      Given a companion paired for "Bryn" 2 hours ago
      And "Mallory" is a registered player
      When "Mallory" revokes the companion
      Then the use case fails with "Companion.NotFound" as NotFound
      And nothing is committed

    Scenario: Revoking a companion is committed
      Given a companion paired for "Bryn" 2 hours ago
      When "Bryn" revokes the companion
      Then the use case succeeds
      And the companion is committed

  Rule: Requests are validated before their handler runs

    Scenario Outline: Malformed requests are refused
      When the <request> is validated
      Then the validation fails on "<property>"

      Examples:
        | request                       | property      |
        | confirmation without a player | UserId        |
        | confirmation with a bad code  | PairingCode   |
        | lookup with a bad code        | PairingCode   |
        | collection without a code     | DeviceCode    |
        | start with a long label       | ComputerLabel |
        | list without a player         | UserId        |
        | revocation without a player   | UserId        |
        | revocation without a companion | CompanionId  |
