Feature: Community role settings
  As a community Administrator
  I want to see and change which Discord roles give Officer and Raid leader
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
        | RaidLeader    |               | 0       |
        | Member        |               | 2       |

    Scenario: One Discord role counted on both rows it gives
      Given the Discord role "Officier" gives RaidLeader
      When "Gihed" reads the community's roles
      Then these roles have these members
        | role          | discord roles | members |
        | Administrator |               | 1       |
        | Officer       | Officier      | 1       |
        | RaidLeader    | Officier      | 1       |
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
      Then the request is refused because the user is not the Administrator
      And nothing is saved

    Scenario: A role that isn't one of the server's mappable roles is refused
      When "Gihed" maps the Discord role "Dyno" to Officer
      Then the request is refused because the role can't be mapped
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

  Rule: A member of the server sees its people with the role each one gets

    Scenario: Members are listed by role, each with their Discord roles
      Given the Discord role "Officier" gives RaidLeader
      When "Daymox" lists the community's members
      Then the members are listed as
        | name    | discord roles | role          |
        | Gihed   | Guild Master  | Administrator |
        | Malarya | Officier      | Officer       |
        | Daymox  | Veteran       | Member        |
        | Orlk    |               | Member        |
      And the list says when Discord was asked

    Scenario: Someone outside the server can't list the members
      Given "Stranger" is not in the Discord server
      When "Stranger" lists the community's members
      Then the request is refused because the user is not a member

    Scenario: Discord being unavailable refuses the list
      Given Discord can't be reached
      When "Gihed" lists the community's members
      Then the request fails because Discord is unavailable
