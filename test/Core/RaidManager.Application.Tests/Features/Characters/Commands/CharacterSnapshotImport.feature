Feature: Character snapshot import
  As a player
  I want my companion's uploads to reach my characters
  So that new characters wait for my review and known ones stay current

  Background:
    Given the companion of "Bryn" is paired

  Rule: A new character is imported with a pending claim for the companion's player

    Scenario: A snapshot of an unknown character imports it
      When the companion uploads a snapshot of "Arthasdk" captured 1 hours ago
      Then the upload is "Imported"
      And "Arthasdk" is imported with a "Pending" claim for "Bryn"
      And the companion's last upload is recorded
      And the import is committed

    Scenario: An unknown character without its identity is refused
      When the companion uploads a snapshot of "Arthasdk" without its identity
      Then the upload fails with "Character.Snapshot.IdentityUnavailable" as ValidationError
      And nothing is committed

  Rule: A known character keeps its owner and changes only for a newer snapshot

    Scenario: A character another player owns keeps its owner
      Given "Alice" owns "Arthasdk"
      When the companion uploads a snapshot of "Arthasdk" captured 1 hours ago
      Then the upload is "Imported"
      And "Alice" owns "Arthasdk"
      And "Arthasdk" has a "Conflict" claim for "Bryn"

    Scenario: A repeated snapshot is already current
      Given the companion uploaded a snapshot of "Arthasdk" captured 1 hours ago
      When the companion uploads the same snapshot again
      Then the upload is "AlreadyCurrent"
      And the companion's last upload is recorded
      And the import is committed

  Rule: An upload needs a paired companion and a commit

    Scenario: A companion that no longer exists is not found
      Given the companion no longer exists
      When the companion uploads a snapshot of "Arthasdk" captured 1 hours ago
      Then the upload fails with "Companion.NotFound" as NotFound
      And nothing is committed

    Scenario: A failed commit is returned
      Given the commit will fail
      When the companion uploads a snapshot of "Arthasdk" captured 1 hours ago
      Then the upload fails with "Commit.Failed" as ValidationError

  Rule: A snapshot that breaks the addon contract is refused before the handler runs

    Scenario: A snapshot that follows the contract passes validation
      When a snapshot without changes is validated
      Then the validation passes

    Scenario Outline: A snapshot that breaks the contract fails validation
      When a snapshot with "<change>" is validated
      Then the validation fails on "<property>"

      Examples:
        | change                       | property                                  |
        | schema version 2             | SchemaVersion                             |
        | no companion                 | CompanionId                               |
        | no character                 | Character                                 |
        | realm Northrend              | Character.Realm                           |
        | realm 1                      | Character.Realm                           |
        | name with a digit            | Character.Name                            |
        | capture tomorrow             | Character.CapturedAt                      |
        | unknown section status       | Character.Guild                           |
        | observed guild without time  | Character.Guild                           |
        | unknown class                | Character.Identity.Class                  |
        | unknown race                 | Character.Identity.Race                   |
        | unknown faction              | Character.Identity.Faction                |
        | level 81                     | Character.Identity.Level                  |
        | guild without a name         | Character.Guild.Name                      |
        | profession above its maximum | Character.Professions.Items[0].MaxRank    |
        | slot 20                      | Character.Equipped.Slots[0].Slot          |
        | slot listed twice            | Character.Equipped.Slots                  |
        | item without an id           | Character.Equipped.Slots[0].ItemId        |
        | third talent group           | Character.Talents.ActiveGroup             |
        | two talent trees             | Character.Talents.Groups[0].Tabs          |
        | ranks with a letter          | Character.Talents.Groups[0].Tabs[0].Ranks |
        | scan without completeness    | Character.Lockouts.Complete               |
        | reset in 61 days             | Character.Lockouts.Items[0].ResetSeconds  |
