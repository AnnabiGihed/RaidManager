Feature: Remove my characters
  As a tester on dev or test
  I want to remove all my characters at once
  So that a sync or review test starts over from a clean state

  Rule: Only what is the player's alone is deleted

    Scenario: The player's own characters are deleted and their other claims withdrawn
      Given "Alice" owns a character "Arthasdk"
      And "Alice" claimed a new character "Uthertank"
      And "Bob" owns a character "Sylvanash" that "Alice" also claimed
      When "Alice" removes all their characters
      Then the removal succeeds with 3 characters
      And "Arthasdk" and "Uthertank" are deleted
      And "Sylvanash" stays with "Bob", without the claim of "Alice"
      And the removal is committed once

    Scenario: A player without characters removes nothing
      When "Alice" removes all their characters
      Then the removal succeeds with 0 characters
      And the removal is committed once

    Scenario: A failed commit is returned
      Given "Alice" owns a character "Arthasdk"
      And the commit will fail
      When "Alice" removes all their characters
      Then the removal fails with "Commit.Failed"

    Scenario: An empty player is refused before anything is loaded
      When a removal is validated without a user id
      Then the validation fails on "UserId"
