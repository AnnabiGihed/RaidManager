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

  Rule: Removing a player's characters on dev and test deletes only what is theirs alone

    Scenario Outline: A character is only the player's when they own it or are its only claimant
      Given <setup>
      Then the character <belongs> only to "Alice"

      Examples:
        | setup                                           | belongs        |
        | "Alice" owns the character                      | belongs        |
        | the companion of "Alice" uploaded the character | belongs        |
        | "Bob" owns the character                        | doesn't belong |

    Scenario: A character another player claims too isn't only the player's
      Given the companion of "Alice" uploaded the character
      And the companion of "Bob" uploaded the character
      Then the character doesn't belong only to "Alice"

    Scenario: Withdrawing a claim on another player's character keeps their ownership
      Given "Bob" owns the character
      And the companion of "Alice" uploaded the character
      When "Alice" withdraws the claim
      Then the claim decision succeeds
      And "Alice" has no claim
      And "Bob" owns the character

    Scenario: Withdrawing without a claim is not found
      When "Alice" withdraws the claim
      Then the claim decision fails with "Character.Claim.NotFound"
