Feature: Community linking
  As a server manager who added the RaidManager bot
  I want the server linked once, with me as its Administrator
  So that my community can plan raids in RaidManager

  Rule: A server links to one community

    Scenario: Linking a new server makes the user its Administrator
      Given "Alice" is a RaidManager user
      When "Alice" links the Discord server "123456789012345678" named "Citadel Vanguard" on Icecrown
      Then the community is linked with "Alice" as its Administrator
      And the link is saved

    Scenario: A server that is already linked is refused and left unchanged
      Given "Alice" is a RaidManager user
      And the Discord server "123456789012345678" is already linked
      When "Alice" links the Discord server "123456789012345678" named "Citadel Vanguard" on Icecrown
      Then the link is refused because the server is already linked
      And nothing is saved

    Scenario: An unknown user can't link a server
      When an unknown user links the Discord server "123456789012345678" named "Citadel Vanguard" on Icecrown
      Then the link is refused because the user was not found
      And nothing is saved

  Rule: The link needs a server, a name, a realm and a user

    Scenario Outline: An invalid link is rejected before the handler runs
      When a link is validated with <case>
      Then the link validation fails on "<property>"

      Examples:
        | case                    | property            |
        | a server id of letters  | DiscordGuildId      |
        | a blank name            | Name                |
        | an unknown realm        | Realm               |
        | a realm number          | Realm               |
        | no user                 | AdministratorUserId |

    Scenario: A valid link passes validation
      When a link is validated with every field set
      Then the link validation passes
