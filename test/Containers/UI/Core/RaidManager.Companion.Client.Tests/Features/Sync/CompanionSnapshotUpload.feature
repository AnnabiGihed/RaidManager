Feature: Companion snapshot upload
  As a player
  I want waiting snapshots to upload on their own and retry until RaidManager has them
  So that no character snapshot is lost while I'm offline

  Background:
    Given this computer is paired

  Rule: A snapshot RaidManager accepts leaves the queue

    Scenario Outline: An accepted snapshot leaves the queue
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager answers uploads with "<answer>"
      When the companion uploads
      Then 0 snapshots are waiting
      And the connection is "Online"
      And the last success is recorded

      Examples:
        | answer         |
        | Imported       |
        | AlreadyCurrent |

    Scenario: Waiting snapshots upload oldest first
      Given a snapshot of "Arthasdk" is waiting
      And a snapshot of "Sylvanash" is waiting
      When the companion uploads
      Then RaidManager received "Arthasdk, Sylvanash"

  Rule: A snapshot RaidManager refuses is dropped and shown

    Scenario: A refused snapshot leaves the queue as a refusal
      Given a snapshot of "Arthasdk" is waiting
      And a snapshot of "Sylvanash" is waiting
      And RaidManager refuses "Arthasdk" with "Character.Snapshot.IdentityUnavailable"
      When the companion uploads
      Then 0 snapshots are waiting
      And the refusals are "Arthasdk: Character.Snapshot.IdentityUnavailable"

    Scenario: A later acceptance clears the character's refusal
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager refuses "Arthasdk" with "Character.Snapshot.IdentityUnavailable"
      And the companion has uploaded
      And RaidManager accepts "Arthasdk" again
      And a newer snapshot of "Arthasdk" is waiting
      When the companion uploads
      Then the refusals are "none"

  Rule: An upload that can't reach RaidManager retries with growing waits

    Scenario: An unreachable RaidManager keeps the snapshot waiting
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager can't be reached
      When the companion uploads
      Then 1 snapshots are waiting
      And the connection is "Offline"

    Scenario Outline: Each failure in a row waits longer up to five minutes
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager can't be reached
      When the companion fails to upload <failures> times in a row
      Then the next upload waits at least <least> seconds
      And the next upload waits at most <most> seconds

      Examples:
        | failures | least | most |
        | 1        | 5     | 6    |
        | 2        | 10    | 12   |
        | 3        | 20    | 24   |
        | 7        | 300   | 360  |
        | 10       | 300   | 360  |

    Scenario Outline: The snapshot uploads once the wait has passed
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager can't be reached
      And the companion has uploaded
      And RaidManager is reachable again
      When the companion uploads <seconds> seconds later
      Then <waiting> snapshots are waiting

      Examples:
        | seconds | waiting |
        | 4       | 1       |
        | 7       | 0       |

    Scenario: Retrying now uploads at once
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager can't be reached
      And the companion has uploaded
      And RaidManager is reachable again
      When the player retries now
      Then 0 snapshots are waiting

  Rule: RaidManager's request to slow down is obeyed

    Scenario Outline: A slow-down waits as long as RaidManager asks
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager asks to slow down for "<asked>"
      And the companion has uploaded
      And RaidManager is reachable again
      When the companion uploads <seconds> seconds later
      Then <waiting> snapshots are waiting

      Examples:
        | asked      | seconds | waiting |
        | 30 seconds | 29      | 1       |
        | 30 seconds | 30      | 0       |
        | nothing    | 59      | 1       |
        | nothing    | 60      | 0       |

  Rule: Uploads need a device token RaidManager accepts

    Scenario: A refused device token stops uploads
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager refuses the device token
      When the companion uploads
      Then 1 snapshots are waiting
      And the connection is "NeedsPairing"

    Scenario: The refused device token isn't sent again
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager refuses the device token
      And the companion has uploaded
      When the companion uploads
      Then RaidManager received "Arthasdk"

    Scenario: Pairing again resumes uploads
      Given a snapshot of "Arthasdk" is waiting
      And RaidManager refuses the device token
      And the companion has uploaded
      And this computer is paired again
      When the companion uploads
      Then 0 snapshots are waiting

    Scenario: A computer that isn't paired uploads nothing
      Given a snapshot of "Arthasdk" is waiting
      And the pairing is removed
      When the companion uploads
      Then RaidManager received "none"
      And the connection is "NotPaired"
