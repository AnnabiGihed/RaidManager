Feature: Community permission check
  As a community officer
  I want RaidManager to check what I may do with Discord before a raid action
  So that only the people Discord currently trusts manage our raids

  Background:
    Given a linked community whose Discord role "111" gives Officer

  Rule: Discord says who is in the server and with which roles

    Scenario: A member with a mapped Discord role gets what its role allows
      Given Discord says "Alice" is in the server with the roles "111"
      When the permissions of "Alice" are checked
      Then the check allows "ManageRaids, BuildRosters, RunRaidNight, ReviewConflicts"

    Scenario: A member without a mapped Discord role is allowed nothing
      Given Discord says "Alice" is in the server with the roles "999"
      When the permissions of "Alice" are checked
      Then the check allows "None"

    Scenario: The Administrator is still asked about with Discord
      Given Discord says the Administrator is in the server with no roles
      When the permissions of the Administrator are checked
      Then the check allows "All"

    Scenario: Someone who left the server gets nothing
      Given Discord says "Alice" is not in the server
      When the permissions of "Alice" are checked
      Then the check is refused because "Alice" is not a member

  Rule: A check that can't be answered gives nothing

    Scenario: Discord being unavailable refuses the check
      Given Discord can't be reached
      When the permissions of "Alice" are checked
      Then the check fails because Discord is unavailable

    Scenario: An unknown community is not found
      When the permissions of "Alice" are checked in an unknown community
      Then the check fails because the community was not found

    Scenario: A user RaidManager doesn't know is not a member
      When the permissions of an unknown user are checked
      Then the check is refused because the user is not a member

  Rule: The check needs a community and a user

    Scenario Outline: A missing identifier is rejected before the handler runs
      When the check is validated without a <field>
      Then the validation fails on "<property>"

      Examples:
        | field        | property    |
        | community id | CommunityId |
        | user id      | UserId      |
