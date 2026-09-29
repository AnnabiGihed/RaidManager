Feature: Character claims
  As a player
  I want to approve the characters my companion discovers
  So that only characters I own become raid signup options

  Background:
    Given a level 80 character on Icecrown

  Rule: A discovered character waits for its player's decision

    Scenario: Discovery creates a pending claim
      When the companion of "Alice" uploads the character
      Then the claim of "Alice" is "Pending"
      And the character has no owner

    Scenario: Approving a pending claim makes the player the owner
      Given the companion of "Alice" uploaded the character
      When "Alice" approves the claim
      Then the claim decision succeeds
      And the claim of "Alice" is "Approved"
      And "Alice" owns the character

    Scenario: Approving without a claim is not found
      When "Alice" approves the claim
      Then the claim decision fails with "Character.Claim.NotFound"

  Rule: A rejected claim stays rejected

    Scenario: Uploading again after rejection keeps the claim rejected
      Given the companion of "Alice" uploaded the character
      And "Alice" rejected the claim
      When the companion of "Alice" uploads the character
      Then the claim of "Alice" is "Rejected"
      And the character has no owner

  Rule: An upload never transfers an owned character

    Scenario: Another player's discovery of an owned character is a conflict
      Given "Alice" owns the character
      When the companion of "Bob" uploads the character
      Then the claim of "Bob" is "Conflict"
      And "Alice" owns the character

    Scenario: A conflicted claim cannot be approved
      Given "Alice" owns the character
      And the companion of "Bob" uploaded the character
      When "Bob" approves the claim
      Then the claim decision fails with "Character.Claim.NotPending"
      And "Alice" owns the character

    Scenario: Approval after another player became owner turns into a conflict
      Given the companion of "Alice" uploaded the character
      And the companion of "Bob" uploaded the character
      And "Alice" approved the claim
      When "Bob" approves the claim
      Then the claim decision fails with "Character.Claim.OwnedByAnotherUser"
      And the claim of "Bob" is "Conflict"
      And "Alice" owns the character
