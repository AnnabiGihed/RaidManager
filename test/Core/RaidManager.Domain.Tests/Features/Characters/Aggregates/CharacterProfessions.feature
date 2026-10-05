Feature: Character professions
  As a player
  I want my profile to show the professions the addon read
  So that officers and I see trustworthy skill facts before signups

  Background:
    Given a character whose professions were read 2 hours ago
      | name          | rank | maxRank |
      | Blacksmithing | 450  | 450     |
      | Mining        | 450  | 450     |

  Rule: A complete read replaces the professions, in the game's order

    Scenario: A newer read replaces the professions
      When the professions are read 1 hours ago
        | name          | rank | maxRank |
        | Jewelcrafting | 440  | 450     |
        | Mining        | 450  | 450     |
      Then the read is recorded
      And the character has these professions
        | name          | rank | maxRank |
        | Jewelcrafting | 440  | 450     |
        | Mining        | 450  | 450     |
      And the professions were last read 1 hours ago
      And the last addon sync was 1 hours ago

    Scenario: A read without professions clears them
      When the professions are read 1 hours ago
        | name | rank | maxRank |
      Then the read is recorded
      And the character has no professions

  Rule: An older read cannot replace newer professions

    Scenario: An out-of-order read is ignored
      When the professions are read 3 hours ago
        | name    | rank | maxRank |
        | Fishing | 75   | 150     |
      Then the read is ignored
      And the character has these professions
        | name          | rank | maxRank |
        | Blacksmithing | 450  | 450     |
        | Mining        | 450  | 450     |
      And the professions were last read 2 hours ago

  Rule: A profession needs a name and a skill within its training

    Scenario Outline: An invalid profession is rejected and nothing changes
      When the professions are read 1 hours ago with "<name>" at <rank> of <maxRank>
      Then the read fails with a domain error
      And the professions were last read 2 hours ago

      Examples:
        | name    | rank | maxRank |
        |         | 75   | 150     |
        | Fishing | -1   | 150     |
        | Fishing | 151  | 150     |

  Rule: A profile is visible to the community until its owner chooses otherwise

    Scenario: A new character is visible to the community
      Then the character's visibility is Community
