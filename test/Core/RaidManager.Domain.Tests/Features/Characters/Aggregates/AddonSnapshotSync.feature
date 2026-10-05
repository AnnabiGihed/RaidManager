Feature: Addon snapshot sync
  As a player
  I want each snapshot my companion uploads to update my character
  So that officers see current facts without my copying files

  Rule: Only a snapshot newer than the last one applied changes the character

    Scenario: A newer snapshot refreshes the identity and the guild
      Given a level 70 character in no guild
      When a snapshot captured 1 hours ago shows level 80 in the guild "Citadel Vanguard"
      Then the snapshot is applied
      And the character is level 80
      And the character's guild is "Citadel Vanguard"

    Scenario Outline: A repeated or older snapshot changes nothing
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured <hours> hours ago shows level 80 in the guild "Ashen Verdict"
      Then the snapshot is ignored
      And the character is level 70
      And the character's guild is "Citadel Vanguard"

      Examples:
        | hours |
        | 2     |
        | 3     |

    Scenario: A snapshot that leaves the guild clears it
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago shows the character in no guild
      Then the snapshot is applied
      And the character is in no guild

  Rule: A section the game didn't answer keeps what was known

    Scenario: A snapshot without answered sections keeps every fact
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago has no answered section
      Then the snapshot is applied
      And the character is level 70
      And the character's guild is "Citadel Vanguard"
      And the character has 1 professions
      And the character is saved to 1 raids

    Scenario: An incomplete raid-save scan keeps the raid saves
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago has an incomplete raid-save scan
      Then the character is saved to 1 raids

    Scenario: A complete raid-save scan without saves clears them
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago has a complete raid-save scan without saves
      Then the character is saved to 0 raids

    Scenario: A skill list without professions clears them
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago has a skill list without professions
      Then the character has 0 professions

  Rule: Each talent group becomes a loadout and the active one gets the worn gear

    Scenario: The first snapshot makes the active talent group the primary loadout
      Given a level 80 character in no guild
      When a snapshot captured 1 hours ago shows talent group 2 active wearing 3 items
      Then the snapshot is applied
      And the character has these loadouts
        | group | name  | role        | primary | items |
        | 1     | Frost | MeleeDamage | no      | 0     |
        | 2     | Blood | Tank        | yes     | 3     |

    Scenario: A switch of talent group keeps the other loadout's gear and the primary loadout
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago shows talent group 2 active wearing 2 items
      Then the character has these loadouts
        | group | name  | role        | primary | items |
        | 1     | Frost | MeleeDamage | yes     | 3     |
        | 2     | Blood | Tank        | no      | 2     |

    Scenario: A slot the game didn't describe keeps the item known before
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago shows talent group 1 active with the head slot unread
      Then the head of talent group 1 holds item 51312
      And talent group 1 wears 3 items

    Scenario: Gear that couldn't be read keeps the active loadout's gear
      Given a character whose snapshot captured 2 hours ago was applied
      When a snapshot captured 1 hours ago shows talent group 1 active without readable gear
      Then talent group 1 wears 3 items

    Scenario: An addon loadout has no GearScore and no stats until an item catalog exists
      Given a level 80 character in no guild
      When a snapshot captured 1 hours ago shows talent group 1 active wearing 3 items
      Then no loadout has a GearScore or stats

    Scenario: A third talent group is rejected
      Given a level 80 character in no guild
      When a snapshot captured 1 hours ago shows a third talent group
      Then the snapshot fails with a domain error

  Rule: The talent tree with the most points spent gives the loadout its role

    Scenario Outline: A class and its main tree give a role
      When the role of a "<class>" with <first>, <second> and <third> points is read
      Then the role is "<role>"

      Examples:
        | class       | first | second | third | role         |
        | DeathKnight | 51    | 10     | 10    | Tank         |
        | DeathKnight | 0     | 53     | 18    | MeleeDamage  |
        | Druid       | 0     | 0      | 0     | RangedDamage |
        | Druid       | 10    | 10     | 51    | Healer       |
        | Paladin     | 0     | 55     | 16    | Tank         |
        | Priest      | 13    | 0      | 58    | RangedDamage |
        | Warrior     | 0     | 17     | 54    | Tank         |
        | Shaman      | 0     | 57     | 14    | MeleeDamage  |

  Rule: A snapshot from the future is rejected

    Scenario: A snapshot captured an hour from now fails
      Given a level 80 character in no guild
      When a snapshot captured -1 hours ago shows level 80 in the guild "Citadel Vanguard"
      Then the snapshot fails with a domain error
