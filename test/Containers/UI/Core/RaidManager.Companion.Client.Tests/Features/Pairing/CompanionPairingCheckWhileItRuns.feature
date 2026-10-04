Feature: Companion pairing check while it runs
  As a player
  I want a computer I revoke to stop at once, even while its companion runs
  So that only the computers I keep paired upload for me

  Rule: A paired companion checks its token every five minutes

    Scenario Outline: A revocation shows at the next check
      Given a pairing for "Bryn" is stored
      And RaidManager answers the token checks with "Valid" then "Refused"
      And the companion has started
      When <seconds> seconds pass
      Then the companion shows "<state>"

      Examples:
        | seconds | state   |
        | 299     | Paired  |
        | 300     | Revoked |

    Scenario: A check that can't reach RaidManager keeps the pairing
      Given a pairing for "Bryn" is stored
      And RaidManager answers the token checks with "Valid" then "Unavailable"
      And the companion has started
      When 300 seconds pass
      Then the companion shows "Paired"
      And the stored pairing is "kept"

    Scenario: A computer just paired with a code is checked too
      Given RaidManager answers with the code "K7M-4QX" valid for 10 minutes and a polling interval of 5 seconds
      And "Bryn" confirms the code before the first poll
      And RaidManager answers the token checks with "Refused" then "Refused"
      And the companion shows the code
      When 305 seconds pass
      Then the companion shows "Revoked"

  Rule: Opening the companion checks its token at once

    Scenario: Opening a revoked companion shows it at once
      Given a pairing for "Bryn" is stored
      And RaidManager answers the token checks with "Valid" then "Refused"
      And the companion has started
      When the player opens the companion
      Then the companion shows "Revoked"
      And the stored pairing is "deleted"

    Scenario: Opening a companion that isn't paired checks nothing
      Given RaidManager answers with the code "K7M-4QX" valid for 10 minutes and a polling interval of 5 seconds
      And the companion shows the code
      When the player opens the companion
      Then RaidManager was asked to check the token 0 times
