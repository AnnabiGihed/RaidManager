Feature: Character claim decisions
  As a player
  I want to approve or reject the characters my companion discovered
  So that only characters I own become raid signup options

  Background:
    Given a character discovered by the companion of "Alice"

  Rule: Approving a pending claim makes the player the owner

    Scenario: Approving a pending claim is committed
      When "Alice" approves the claim
      Then the decision succeeds
      And "Alice" owns the character
      And the decision is committed

    Scenario: Approving a character that does not exist is not found
      When "Alice" approves the claim on an unknown character
      Then the decision fails with "Character.NotFound" as NotFound
      And nothing is committed

    Scenario: Approving a claim that is no longer pending is refused
      Given "Alice" rejected the claim
      When "Alice" approves the claim
      Then the decision fails with "Character.Claim.NotPending" as Conflict
      And nothing is committed

    Scenario: Approving a character another player owns keeps the conflict for review
      Given "Bob" owns the character
      When "Alice" approves the claim
      Then the decision fails with "Character.Claim.OwnedByAnotherUser" as Conflict
      And "Bob" owns the character
      And the decision is committed

    Scenario: A failed commit is returned
      Given the commit will fail
      When "Alice" approves the claim
      Then the decision fails with "Commit.Failed" as ValidationError

  Rule: Rejecting a pending claim keeps the character out of the player's options

    Scenario: Rejecting a pending claim is committed
      When "Alice" rejects the claim
      Then the decision succeeds
      And the character has no owner
      And the decision is committed

    Scenario: Rejecting without a claim is not found
      When "Carol" rejects the claim
      Then the decision fails with "Character.Claim.NotFound" as NotFound
      And nothing is committed

  Rule: A decision needs both identifiers

    Scenario Outline: An empty identifier is rejected before the handler runs
      When a <decision> is validated without a <identifier>
      Then the validation fails on "<property>"

      Examples:
        | decision | identifier   | property    |
        | approval | character id | CharacterId |
        | approval | user id      | UserId      |
        | rejection | character id | CharacterId |
        | rejection | user id      | UserId      |
