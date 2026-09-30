Feature: Pending character claims
  As a player
  I want to see the characters waiting for my decision
  So that I can approve my own characters and resolve conflicts

  Rule: The query returns what the claim reader finds for the player

    Scenario: Claims awaiting a decision are returned in the reader's order
      Given the claim reader has these claims for "Alice"
        | name       | state    |
        | Conflicted | Conflict |
        | Pendingone | Pending  |
      When "Alice" asks for her pending character claims
      Then the query succeeds with these claims
        | name       | state    |
        | Conflicted | Conflict |
        | Pendingone | Pending  |

    Scenario: A player with nothing to decide gets an empty list
      When "Alice" asks for her pending character claims
      Then the query succeeds with no claims

  Rule: The query needs a player

    Scenario: An empty user identifier is rejected before the handler runs
      When the query is validated without a user id
      Then the validation fails on "UserId"
