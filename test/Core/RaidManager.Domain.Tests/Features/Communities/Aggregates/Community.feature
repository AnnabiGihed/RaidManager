Feature: Community linking and roles
  As a community Administrator
  I want my Discord server linked once and my Discord roles to give RaidManager roles
  So that only the people Discord trusts manage our raids

  Rule: Adding the bot links a server and makes the installer its Administrator

    Scenario: Linking a server makes the installer its Administrator
      When a user links the Discord server "123456789012345678" named "  Citadel Vanguard  " on Icecrown
      Then the community is named "Citadel Vanguard"
      And the installer has every permission

    Scenario: A server id that is not a snowflake is rejected
      When a user links the Discord server "citadel" named "Citadel Vanguard" on Icecrown
      Then the community change is rejected

    Scenario: A blank name is rejected
      When a user links the Discord server "123456789012345678" named "   " on Icecrown
      Then the community change is rejected

    Scenario: A name longer than 100 characters is rejected
      When a user links a Discord server with a name of 101 characters
      Then the community change is rejected

  Rule: Every community starts with the Officer and Raid leader presets

    Scenario: A new community has the two presets with their permissions
      Given a linked community
      Then the community's roles are
        | role        | permissions                                                |
        | Officer     | ManageRaids, BuildRosters, RunRaidNight, ReviewConflicts   |
        | Raid leader | ManageRaids, BuildRosters, RunRaidNight                    |

  Rule: The Administrator maps Discord roles to the community's roles

    Scenario: Mapping a Discord role reports a change
      Given a linked community
      When the Administrator maps the Discord role "111" to "Officer"
      Then the Discord role "111" gives "Officer"
      And the role mappings changed once

    Scenario: Mapping the same role again changes nothing
      Given a linked community
      And the Discord role "111" gives "Officer"
      When the Administrator maps the Discord role "111" to "Officer"
      Then the role mappings did not change

    Scenario: One Discord role can give both Officer and Raid leader
      Given a linked community
      And the Discord role "111" gives "Officer"
      When the Administrator maps the Discord role "111" to "Raid leader"
      Then the Discord role "111" gives "Raid leader"
      And the Discord role "111" gives "Officer"
      And the community has 2 role mappings
      And the role mappings changed once

    Scenario: A role the community doesn't have can't be mapped
      Given a linked community
      When the Administrator maps the Discord role "111" to a role the community doesn't have
      Then the community change is rejected

    Scenario: A role id that is not a snowflake is rejected
      Given a linked community
      When the Administrator maps the Discord role "officers" to "Officer"
      Then the community change is rejected

    Scenario: Removing a mapping reports a change
      Given a linked community
      And the Discord role "111" gives "Officer"
      When the Administrator removes the "Officer" mapping of the Discord role "111"
      Then the community has 0 role mappings
      And the role mappings changed once

    Scenario: Removing one of a Discord role's mappings keeps the other
      Given a linked community
      And the Discord role "111" gives "Officer"
      And the Discord role "111" gives "Raid leader"
      When the Administrator removes the "Officer" mapping of the Discord role "111"
      Then the Discord role "111" gives "Raid leader"
      And the community has 1 role mapping

    Scenario: Removing a role that was not mapped changes nothing
      Given a linked community
      When the Administrator removes the "Officer" mapping of the Discord role "111"
      Then the role mappings did not change

  Rule: A member has every role and permission their Discord roles give

    Scenario Outline: Permissions add up across the member's roles
      Given a linked community
      And the Discord role "111" gives "Officer"
      And the Discord role "222" gives "Raid leader"
      When a member has the Discord roles "<discord roles>"
      Then the member's roles are "<roles>"
      And the member's permissions are "<permissions>"

      Examples:
        | discord roles | roles                | permissions                                              |
        | 111,222       | Officer, Raid leader | ManageRaids, BuildRosters, RunRaidNight, ReviewConflicts |
        | 222           | Raid leader          | ManageRaids, BuildRosters, RunRaidNight                  |
        | 222,333       | Raid leader          | ManageRaids, BuildRosters, RunRaidNight                  |
        | 333           |                      | None                                                     |
        |               |                      | None                                                     |

  Rule: The Administrator and role managers shape the community's roles

    Scenario: A created role goes to the end of the list with what it allows
      Given a linked community
      When the Administrator creates the role "  Veteran  " allowing "RunRaidNight"
      Then the role change succeeds
      And the community's roles are
        | role        | permissions                                              |
        | Officer     | ManageRaids, BuildRosters, RunRaidNight, ReviewConflicts |
        | Raid leader | ManageRaids, BuildRosters, RunRaidNight                  |
        | Veteran     | RunRaidNight                                             |

    Scenario Outline: A role that can't be created is refused
      Given a linked community
      When <who> creates the role "<name>" allowing "<permissions>"
      Then the role change fails with "<error>"

      Examples:
        | who               | name                                                | permissions          | error                                |
        | the Administrator |                                                     | RunRaidNight         | Community.RoleNameInvalid            |
        | the Administrator | 123456789012345678901234567890123456789012345678901 | RunRaidNight         | Community.RoleNameInvalid            |
        | the Administrator | officer                                             | RunRaidNight         | Community.RoleNameTaken              |
        | the Administrator | Veteran                                             | 64                   | Community.RolePermissionsInvalid     |
        | a role manager    | Council                                             | ManageCommunityRoles | Community.CannotGrantRoleManagement  |

    Scenario: Only the Administrator lets a role manage roles
      Given a linked community
      When the Administrator creates the role "Council" allowing "ManageCommunityRoles, ManageRaids"
      Then the role change succeeds

    Scenario: Changing a role renames it and changes what it allows
      Given a linked community
      And the Discord role "222" gives "Raid leader"
      When the Administrator changes the role "Raid leader" to "Raid lead" allowing "RunRaidNight"
      Then the role change succeeds
      And the role mappings changed once
      And a member with the Discord roles "222" may "RunRaidNight"

    Scenario: A role keeps its own name when it changes
      Given a linked community
      When the Administrator changes the role "Officer" to "OFFICER" allowing "ManageRaids"
      Then the role change succeeds

    Scenario Outline: A role change that isn't allowed is refused
      Given a linked community
      And the community has the role "Council" allowing "ManageCommunityRoles"
      When <who> changes the role "<role>" to "<name>" allowing "<permissions>"
      Then the role change fails with "<error>"

      Examples:
        | who               | role    | name        | permissions          | error                               |
        | the Administrator | Officer | Raid leader | ManageRaids          | Community.RoleNameTaken             |
        | the Administrator | Ghost   | Ghost       | ManageRaids          | Community.RoleNotFound              |
        | a role manager    | Council | Council     | ManageRaids          | Community.RoleLocked                |
        | a role manager    | Officer | Officer     | ManageCommunityRoles | Community.CannotGrantRoleManagement |

    Scenario: Deleting a role removes its mappings
      Given a linked community
      And the Discord role "222" gives "Raid leader"
      When the Administrator deletes the role "Raid leader"
      Then the role change succeeds
      And the community has 0 role mappings
      And a member with the Discord roles "222" may "None"

    Scenario Outline: A role deletion that isn't allowed is refused
      Given a linked community
      And the community has the role "Council" allowing "ManageCommunityRoles"
      When <who> deletes the role "<role>"
      Then the role change fails with "<error>"

      Examples:
        | who               | role    | error                  |
        | the Administrator | Ghost   | Community.RoleNotFound |
        | a role manager    | Council | Community.RoleLocked   |

    Scenario: A role manager deletes a role that doesn't manage roles
      Given a linked community
      When a role manager deletes the role "Raid leader"
      Then the role change succeeds
