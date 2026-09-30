Feature: Discord sign-in
  As a player
  I want every Discord sign-in to reach the same account
  So that my characters and raid activity stay attached to one identity

  Rule: One local user per Discord account

    Scenario: A first sign-in registers the player
      When Discord account "80351110224678912" signs in as "Arthas"
      Then the sign-in succeeds with a new user
      And the user is added and committed

    Scenario: A returning player reaches the same user
      Given Discord account "80351110224678912" is registered as "Arthas"
      When Discord account "80351110224678912" signs in as "Arthas"
      Then the sign-in returns the registered user
      And nothing is committed

    Scenario: A returning player with a new display name is updated
      Given Discord account "80351110224678912" is registered as "Arthas"
      When Discord account "80351110224678912" signs in as "Arthas the Pure"
      Then the sign-in returns the registered user
      And the registered user is updated and committed

    Scenario: A failed commit is returned
      Given the commit will fail
      When Discord account "80351110224678912" signs in as "Arthas"
      Then the sign-in fails with "Commit.Failed"

  Rule: Only values Discord issues are accepted

    Scenario Outline: An invalid sign-in is rejected before the handler runs
      When a sign-in for Discord account "<discordId>" as "<displayName>" is validated
      Then the validation fails on "<property>"

      Examples:
        | discordId              | displayName | property      |
        |                        | Arthas      | DiscordUserId |
        | not-a-number           | Arthas      | DiscordUserId |
        | 1234567890123456789012 | Arthas      | DiscordUserId |
        | 80351110224678912      |             | DisplayName   |
