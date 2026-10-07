Feature: Companion upload queue
  As a player
  I want snapshots waiting to upload to survive restarts and stay current
  So that RaidManager gets my latest characters without losing any

  Rule: A character has at most one waiting snapshot, the newest

    Scenario: A newer snapshot replaces the waiting one
      Given a snapshot of "Arthasdk" captured at 100 is waiting
      When a snapshot of "Arthasdk" captured at 200 is read
      Then the waiting snapshots are "Arthasdk at 200"

    Scenario: An older snapshot leaves the waiting one in place
      Given a snapshot of "Arthasdk" captured at 200 is waiting
      When a snapshot of "Arthasdk" captured at 100 is read
      Then the waiting snapshots are "Arthasdk at 200"

    Scenario: Another character's snapshot waits behind the first
      Given a snapshot of "Arthasdk" captured at 100 is waiting
      When a snapshot of "Sylvanash" captured at 100 is read
      Then the waiting snapshots are "Arthasdk at 100, Sylvanash at 100"

  Rule: A snapshot RaidManager accepted isn't queued again

    Scenario Outline: Only a snapshot newer than the accepted one is queued
      Given a snapshot of "Arthasdk" captured at 200 was accepted
      When a snapshot of "Arthasdk" captured at <capturedAt> is read
      Then the waiting snapshots are "<waiting>"

      Examples:
        | capturedAt | waiting         |
        | 100        | none            |
        | 200        | none            |
        | 300        | Arthasdk at 300 |

  Rule: The queue survives a restart of the companion

    Scenario: A waiting snapshot is still waiting after a restart
      Given a snapshot of "Arthasdk" captured at 100 is waiting
      When the companion restarts
      Then the waiting snapshots are "Arthasdk at 100"

    Scenario: An accepted snapshot is remembered after a restart
      Given a snapshot of "Arthasdk" captured at 200 was accepted
      And the companion has restarted
      When a snapshot of "Arthasdk" captured at 200 is read
      Then the waiting snapshots are "none"

  Rule: Excluding an account drops its waiting snapshots

    Scenario: Only the excluded account's snapshots are dropped
      Given a snapshot of "Arthasdk" captured at 100 is waiting from the account "MAINACCOUNT"
      And a snapshot of "Bankalt" captured at 100 is waiting from the account "ALTACCOUNT"
      When the player excludes the account "ALTACCOUNT"
      Then the waiting snapshots are "Arthasdk at 100"
