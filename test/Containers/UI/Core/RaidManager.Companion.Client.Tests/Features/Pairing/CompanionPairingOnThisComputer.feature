Feature: Companion pairing on this computer
  As a player
  I want my computer paired once I confirm the code it shows
  So that the companion uploads only for me

  Rule: Without a stored pairing the companion shows a code and opens the website with it

    Scenario: A code is shown and the website opens with it
      Given no pairing is stored
      And RaidManager answers with the code "K7M-4QX" valid for 10 minutes
      When the companion starts
      Then the companion shows "Waiting"
      And the code "K7M-4QX" is shown with "Expires in 10:00"
      And the website opens with the code "K7M-4QX"

    Scenario: An unreachable RaidManager offers to try again
      Given no pairing is stored
      And RaidManager can't be reached
      When the companion starts
      Then the companion shows "CodeRequestFailed"

  Rule: The companion polls every five seconds or more and waits longer when asked

    Scenario Outline: Polls follow the interval and the back-off
      Given RaidManager answers with the code "K7M-4QX" valid for 10 minutes and a polling interval of <interval> seconds
      And RaidManager answers the first poll with "<answer>"
      And the companion shows the code
      When <seconds> seconds pass
      Then RaidManager was asked for the token <polls> times

      Examples:
        | interval | answer      | seconds | polls |
        | 2        | Pending     | 4       | 0     |
        | 2        | Pending     | 5       | 1     |
        | 5        | SlowDown    | 14      | 1     |
        | 5        | SlowDown    | 15      | 2     |
        | 5        | Unavailable | 15      | 2     |

  Rule: A confirmed code pairs this computer

    Scenario: The token is stored and the player is named
      Given RaidManager answers with the code "K7M-4QX" valid for 10 minutes and a polling interval of 5 seconds
      And "Bryn" confirms the code before the first poll
      And the companion shows the code
      When 5 seconds pass
      Then the companion shows "Paired"
      And the stored pairing belongs to "Bryn"
      And the heading reads "Paired with Bryn"

  Rule: A code that expires offers a new one

    Scenario: The code runs out of time
      Given RaidManager answers with the code "K7M-4QX" valid for 1 minutes and a polling interval of 5 seconds
      And the companion shows the code
      When 60 seconds pass
      Then the companion shows "Expired"

    Scenario Outline: RaidManager refuses the code
      Given RaidManager answers with the code "K7M-4QX" valid for 10 minutes and a polling interval of 5 seconds
      And RaidManager answers the first poll with "<answer>"
      And the companion shows the code
      When 5 seconds pass
      Then the companion shows "Expired"

      Examples:
        | answer  |
        | Expired |
        | Invalid |

  Rule: A stored pairing is forgotten only when RaidManager refuses it

    Scenario Outline: The stored pairing is checked at start
      Given a pairing for "Bryn" is stored
      And RaidManager answers the token check with "<check>"
      When the companion starts
      Then the companion shows "<state>"
      And the stored pairing is "<storage>"

      Examples:
        | check       | state   | storage |
        | Valid       | Paired  | kept    |
        | Unavailable | Paired  | kept    |
        | Refused     | Revoked | deleted |
