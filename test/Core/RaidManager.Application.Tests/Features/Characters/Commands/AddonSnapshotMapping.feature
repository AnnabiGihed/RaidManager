Feature: Addon snapshot mapping
  As a player
  I want my companion's upload read the way the game meant it
  So that my character's facts match what the game showed

  Rule: Game tokens become the character's identity

    Scenario: The identity tokens become class, race and faction
      When the sample snapshot is mapped
      Then the identity is a level 80 "Undead" "DeathKnight" of the "Horde"
      And the guild is "Dark Templars"
      And the professions are "Blacksmithing 450/450, Fishing 1/75"

  Rule: Only current saves of WotLK raids become raid saves

    Scenario: Saved raids keep their difficulty, reset and lockout id
      When the sample snapshot is mapped
      Then the raid saves are
        | raid            | difficulty       | lockout    | hours | extended |
        | IcecrownCitadel | TwentyFivePlayer | 31415926   | 96    | no       |
        | RubySanctum     | TenPlayerHeroic  | 8321499137 | 264   | yes      |

    Scenario Outline: The client's language decides whether a scan can be complete
      When the sample snapshot from a "<locale>" client is mapped
      Then the raid-save scan is "<completeness>"

      Examples:
        | locale | completeness |
        | enUS   | complete     |
        | enGB   | complete     |
        | deDE   | incomplete   |

  Rule: Worn gear keeps each slot's item, emptiness or unread state

    Scenario: The game's inventory slots become equipment slots
      When the sample snapshot is mapped
      Then the gear holds item 51312 in the "Head" slot
      And the gear holds item 50737 in the "MainHand" slot
      And the "Neck" slot is unread
      And the gear holds 2 items

  Rule: A talent group is named after its main tree and keeps its glyphs

    Scenario: Each talent group keeps its trees and its glyphs
      When the sample snapshot is mapped
      Then talent group 1 is "Unholy" with 0, 17 and 54 points
      And talent group 1 has the major glyph 63335 and the minor glyph 60200
      And talent group 2 is "Blood" with 51, 10 and 10 points
      And talent group 1 is active

  Rule: An unavailable section stays unknown

    Scenario: A snapshot whose sections are unavailable maps to no facts
      When the sample snapshot with every section unavailable is mapped
      Then the mapped snapshot has no section

    Scenario: A character in no guild has no guild name
      When the sample snapshot in no guild is mapped
      Then the guild is observed without a name
