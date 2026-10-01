Feature: Community linking and roles
  As a community Administrator
  I want my Discord server linked once and my Discord roles to give RaidManager permissions
  So that only the people Discord trusts manage our raids

  Rule: Adding the bot links a server and makes the installer its Administrator

    Scenario: Linking a server makes the installer its Administrator
      When a user links the Discord server "123456789012345678" named "  Citadel Vanguard  " on Icecrown
      Then the community is named "Citadel Vanguard"
      And the installer's role is Administrator

    Scenario: A server id that is not a snowflake is rejected
      When a user links the Discord server "citadel" named "Citadel Vanguard" on Icecrown
      Then the community change is rejected

    Scenario: A blank name is rejected
      When a user links the Discord server "123456789012345678" named "   " on Icecrown
      Then the community change is rejected

    Scenario: A name longer than 100 characters is rejected
      When a user links a Discord server with a name of 101 characters
      Then the community change is rejected

  Rule: The Administrator maps Discord roles to Officer and Raid leader

    Scenario: Mapping a Discord role reports a change
      Given a linked community
      When the Administrator maps the Discord role "111" to Officer
      Then the Discord role "111" gives Officer
      And the role mappings changed once

    Scenario: Mapping the same role again changes nothing
      Given a linked community
      And the Discord role "111" gives Officer
      When the Administrator maps the Discord role "111" to Officer
      Then the role mappings did not change

    Scenario: Mapping a Discord role to another role replaces its mapping
      Given a linked community
      And the Discord role "111" gives Officer
      When the Administrator maps the Discord role "111" to RaidLeader
      Then the Discord role "111" gives RaidLeader
      And the community has 1 role mapping

    Scenario: Only Officer and Raid leader can be mapped
      Given a linked community
      When the Administrator maps the Discord role "111" to Administrator
      Then the community change is rejected

    Scenario: A role id that is not a snowflake is rejected
      Given a linked community
      When the Administrator maps the Discord role "officers" to Officer
      Then the community change is rejected

    Scenario: Removing a mapping reports a change
      Given a linked community
      And the Discord role "111" gives Officer
      When the Administrator removes the mapping of the Discord role "111"
      Then the community has 0 role mappings
      And the role mappings changed once

    Scenario: Removing a role that was not mapped changes nothing
      Given a linked community
      When the Administrator removes the mapping of the Discord role "111"
      Then the role mappings did not change

  Rule: A member's role follows their Discord roles

    Scenario Outline: The highest mapped role applies
      Given a linked community
      And the Discord role "111" gives Officer
      And the Discord role "222" gives RaidLeader
      When a member has the Discord roles "<discord roles>"
      Then the member's role is <role>

      Examples:
        | discord roles | role       |
        | 111,222       | Officer    |
        | 222           | RaidLeader |
        | 222,333       | RaidLeader |
        | 333           | Member     |
        |               | Member     |
