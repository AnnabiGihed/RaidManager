Feature: Community role settings
  As a community Administrator
  I want to see and change the community's roles and which Discord roles give them
  So that RaidManager permissions follow the roles my server already uses

  Background:
    Given a linked community administered by "Gihed"
    And the Discord server is named "Dark Templars" with the roles "Guild Master,Officier,Veteran"
    And the Discord role "Officier" gives Officer
    And these people are in the Discord server
      | name    | roles        |
      | Gihed   | Guild Master |
      | Malarya | Officier     |
      | Daymox  | Veteran      |
      | Orlk    |              |

  Rule: A member of the server sees the roles, the Administrator can edit them

    Scenario: The Administrator sees each role with its Discord roles and members
      When "Gihed" reads the community's roles
      Then the roles can be edited
      And the mappable roles are "Guild Master,Officier,Veteran"
      And these roles have these members
        | role          | discord roles | members |
        | Administrator |               | 1       |
        | Officer       | Officier      | 1       |
        | Raid leader   |               | 0       |
        | Member        |               | 2       |

    Scenario: One Discord role counted on both rows it gives
      Given the Discord role "Officier" gives RaidLeader
      When "Gihed" reads the community's roles
      Then these roles have these members
        | role          | discord roles | members |
        | Administrator |               | 1       |
        | Officer       | Officier      | 1       |
        | Raid leader   | Officier      | 1       |
        | Member        |               | 2       |

    Scenario: Another member sees the roles without editing them
      When "Malarya" reads the community's roles
      Then the roles can't be edited

    Scenario: A Discord role deleted since is shown as missing
      Given the Discord role "Gone" was mapped to RaidLeader and deleted in Discord
      When "Gihed" reads the community's roles
      Then the RaidLeader role shows a missing Discord role

    Scenario: Someone outside the server can't read the roles
      Given "Stranger" is not in the Discord server
      When "Stranger" reads the community's roles
      Then the request is refused because the user is not a member

    Scenario: Discord being unavailable refuses the read
      Given Discord can't be reached
      When "Gihed" reads the community's roles
      Then the request fails because Discord is unavailable

  Rule: Only the Administrator maps Discord roles

    Scenario: The Administrator maps a Discord role to Raid leader
      When "Gihed" maps the Discord role "Veteran" to RaidLeader
      Then the change is saved
      And the Discord role "Veteran" gives RaidLeader

    Scenario: Mapping the Officer role to Raid leader keeps it on Officer
      When "Gihed" maps the Discord role "Officier" to RaidLeader
      Then the change is saved
      And the Discord role "Officier" gives Officer and RaidLeader

    Scenario: Another member can't map a role
      When "Malarya" maps the Discord role "Veteran" to RaidLeader
      Then the request is refused because the user can't manage roles
      And nothing is saved

    Scenario: A role that isn't one of the server's mappable roles is refused
      When "Gihed" maps the Discord role "Dyno" to Officer
      Then the request is refused because the role can't be mapped
      And nothing is saved

    Scenario: A role the community doesn't have can't be mapped
      When "Gihed" maps the Discord role "Veteran" to a role the community doesn't have
      Then the request fails because the role doesn't exist
      And nothing is saved

    Scenario: The Administrator removes a mapping
      When "Gihed" removes the Officer mapping of the Discord role "Officier"
      Then the change is saved
      And the Discord role "Officier" gives nothing

    Scenario: Removing a role that isn't mapped saves nothing
      When "Gihed" removes the Officer mapping of the Discord role "Veteran"
      Then nothing is saved

  Rule: Reading the server keeps its name current

    Scenario: A renamed server's name is refreshed
      When the community's name is refreshed to "Dark Templars Reborn"
      Then the change is saved
      And the community is named "Dark Templars Reborn"

    Scenario: An unchanged name saves nothing
      When the community's name is refreshed to "Dark Templars"
      Then nothing is saved

  Rule: A member of the server sees its people with the roles each one has

    Scenario: Members are listed by role, each with their Discord roles
      Given the Discord role "Officier" gives RaidLeader
      When "Daymox" lists the community's members
      Then the members are listed as
        | name    | discord roles | roles                |
        | Gihed   | Guild Master  | Administrator        |
        | Malarya | Officier      | Officer, Raid leader |
        | Daymox  | Veteran       | Member               |
        | Orlk    |               | Member               |
      And the list says when Discord was asked

    Scenario: Someone outside the server can't list the members
      Given "Stranger" is not in the Discord server
      When "Stranger" lists the community's members
      Then the request is refused because the user is not a member

    Scenario: Discord being unavailable refuses the list
      Given Discord can't be reached
      When "Gihed" lists the community's members
      Then the request fails because Discord is unavailable

    Scenario: Discord failing to read the server refuses the list
      Given Discord can't read the server
      When "Gihed" lists the community's members
      Then the request fails because Discord is unavailable

    Scenario: Discord failing to list the people refuses the list
      Given Discord can't list the server's people
      When "Gihed" lists the community's members
      Then the request fails because Discord is unavailable

    Scenario: A community that doesn't exist can't be listed
      When "Gihed" lists the members of an unknown community
      Then the request fails because the community doesn't exist

  Rule: The Administrator and role managers create, change and delete roles

    Scenario: The Administrator creates a role
      When "Gihed" creates the role "Recruiter" allowing "ReviewConflicts, ManageRaids"
      Then the change is saved
      And the community has the role "Recruiter" allowing "ManageRaids, ReviewConflicts"

    Scenario: A role manager creates a role
      Given the Discord role "Veteran" gives a new role "Council" allowing "ManageCommunityRoles"
      When "Daymox" creates the role "Recruiter" allowing "ReviewConflicts"
      Then the change is saved
      And the community has the role "Recruiter" allowing "ReviewConflicts"

    Scenario: A role manager maps a Discord role to Raid leader
      Given the Discord role "Veteran" gives a new role "Council" allowing "ManageCommunityRoles"
      When "Daymox" maps the Discord role "Guild Master" to RaidLeader
      Then the change is saved
      And the Discord role "Guild Master" gives RaidLeader

    Scenario: A role manager sees which roles they can change
      Given the Discord role "Veteran" gives a new role "Council" allowing "ManageCommunityRoles"
      When "Daymox" reads the community's roles
      Then the roles can be edited
      And the roles can't let a role manage roles
      And the "Officer" role can be changed
      And the "Council" role can't be changed

    Scenario: A role manager can't change, delete or map a role that manages roles
      Given the Discord role "Veteran" gives a new role "Council" allowing "ManageCommunityRoles"
      When "Daymox" changes the role "Council" to "Council" allowing "ManageRaids"
      Then the request is refused because the role is locked
      When "Daymox" deletes the role "Council"
      Then the request is refused because the role is locked
      When "Daymox" maps the Discord role "Guild Master" to the role "Council"
      Then the request is refused because the role is locked

    Scenario: A role manager can't let a role manage roles
      Given the Discord role "Veteran" gives a new role "Council" allowing "ManageCommunityRoles"
      When "Daymox" changes the role "Officer" to "Officer" allowing "ManageCommunityRoles"
      Then the request is refused because only the Administrator can let a role manage roles
      And nothing is saved

    Scenario: A member whose roles don't manage roles can't create one
      When "Malarya" creates the role "Recruiter" allowing "ReviewConflicts"
      Then the request is refused because the user can't manage roles
      And nothing is saved

    Scenario: The Administrator changes a role
      When "Gihed" changes the role "Raid leader" to "Raid lead" allowing "RunRaidNight"
      Then the change is saved
      And the community has the role "Raid lead" allowing "RunRaidNight"

    Scenario: The Administrator deletes a role
      When "Gihed" deletes the role "Raid leader"
      Then the change is saved
      And the community has no role "Raid leader"

    Scenario: A name another role has is refused
      When "Gihed" creates the role "officer" allowing "ManageRaids"
      Then the request fails because the role name is taken

    Scenario: Deleting a role the community doesn't have is not found
      When "Gihed" deletes the role "Ghost"
      Then the request fails because the role doesn't exist

    Scenario: Changing a role in a community that doesn't exist is not found
      When "Gihed" changes a role of a community that doesn't exist
      Then the request fails because the community doesn't exist

    Scenario Outline: A malformed role is rejected before the handler runs
      When a role named "<name>" allowing "<permissions>" is validated
      Then the role is rejected on "<property>"

      Examples:
        | name                                                | permissions | property       |
        |                                                     | ManageRaids | Name           |
        | 123456789012345678901234567890123456789012345678901 | ManageRaids | Name           |
        | Veteran                                             | FlyMounts   | Permissions[0] |

