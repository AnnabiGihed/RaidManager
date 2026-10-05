Feature: Character profiles
  As a player
  I want to see my characters and the profile of each
  So that I know what RaidManager holds about them and how fresh it is

  Rule: The characters list returns what the profile reader finds for the player

    Scenario: The player's characters are returned in the reader's order
      Given the profile reader has these characters for "Alice"
        | name     |
        | Arthasdk |
        | Jainaice |
      When "Alice" asks for her characters
      Then the list succeeds with these characters
        | name     |
        | Arthasdk |
        | Jainaice |

    Scenario: The reader is asked with the clock's time, which decides the current raid saves
      When "Alice" asks for her characters
      Then the reader was asked for the characters of "Alice" at the clock's time

  Rule: Only the character's owner gets its profile

    Scenario: The owner gets the profile
      Given the profile reader has the profile of "Arthasdk" for "Alice"
      When "Alice" asks for the profile of "Arthasdk"
      Then the profile of "Arthasdk" is returned

    Scenario: Anyone else gets not found
      Given the profile reader has the profile of "Arthasdk" for "Alice"
      When "Bob" asks for the profile of "Arthasdk"
      Then the profile query fails as not found

  Rule: The queries need a player and a character

    Scenario: An empty user identifier is rejected before the list handler runs
      When the characters list is validated without a user id
      Then the validation fails on "UserId"

    Scenario Outline: An empty identifier is rejected before the profile handler runs
      When the profile query is validated without a "<identifier>"
      Then the validation fails on "<property>"

      Examples:
        | identifier   | property    |
        | user id      | UserId      |
        | character id | CharacterId |
