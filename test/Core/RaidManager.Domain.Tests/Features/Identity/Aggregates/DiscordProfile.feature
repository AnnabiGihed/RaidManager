Feature: Discord profile
  As a player
  I want my Discord name and avatar kept current
  So that officers and teammates recognize me

  Background:
    Given a user registered as "Arthas" with avatar "https://cdn.discordapp.com/avatars/1/a.png"

  Rule: Only a real change updates the profile

    Scenario: A new display name updates the profile and raises an event
      When the Discord profile is refreshed as "Arthas the Pure" with avatar "https://cdn.discordapp.com/avatars/1/a.png"
      Then the profile changed
      And the display name is "Arthas the Pure"
      And a Discord profile updated event was raised

    Scenario: An unchanged profile raises nothing
      When the Discord profile is refreshed as "Arthas" with avatar "https://cdn.discordapp.com/avatars/1/a.png"
      Then the profile did not change
      And no Discord profile updated event was raised

    Scenario: Surrounding spaces are not a change
      When the Discord profile is refreshed as "  Arthas  " with avatar "https://cdn.discordapp.com/avatars/1/a.png"
      Then the profile did not change
