Feature: Companion sync screens
  As a player
  I want to review the folders the companion watches and see where my uploads stand
  So that I keep characters out when I choose and fix what stops them from syncing

  Background:
    Given this computer is paired with "Bryn"
    And the companion found the accounts "ARTHASACC, JAINAACC, THRALLACC" and excluded "ALTACC"

  Rule: The player reviews the watched folders and excludes an account

    Scenario: Clearing an account waits for Save and sync
      Given the window shows the watched folders
      When the player clears "JAINAACC"
      Then the row of "JAINAACC" reads "Excluded · 2 characters"
      And the sync was asked nothing
      When the player saves and syncs
      Then the sync was asked to "exclude JAINAACC-id"
      And the window shows the "Syncing" board

    Scenario: Ticking an excluded account watches it again
      Given the window shows the watched folders
      When the player ticks "ALTACC"
      And the player saves and syncs
      Then the sync was asked to "watch ALTACC-id"

    Scenario: A folder that isn't a WoW installation shows a warning
      Given the window shows the watched folders
      And the sync refuses folders without WTF/Account
      When the player adds the folder "D:\Backups"
      Then the sync was asked to "add D:\Backups"
      And the window shows the "Watched folders" board with a warning

    Scenario: A WoW folder added is watched without a warning
      Given the window shows the watched folders
      When the player adds the folder "D:\WoW\Warmane"
      Then the window shows the "Watched folders" board without a warning

    Scenario: Finding folders again clears the warning
      Given the window shows the watched folders
      And the sync refuses folders without WTF/Account
      And the player added the folder "D:\Backups"
      When the player finds folders again
      Then the sync was asked to "add D:\Backups, find folders again"
      And the window shows the "Watched folders" board without a warning

  Rule: The window shows the board the sync's status calls for

    Scenario Outline: Each status has its board
      Given the sync is "<status>"
      When the player opens the sync
      Then the window shows the "<board>" board

      Examples:
        | status                       | board                |
        | uploading                    | Syncing              |
        | paused                       | Paused               |
        | offline                      | Offline              |
        | holding an incomplete file   | Incomplete snapshot  |
        | holding a refused snapshot   | Refused snapshot     |
        | holding an unsupported file  | Unsupported addon    |
        | holding an unreadable file   | Unreadable file      |

    Scenario: A companion that watches nothing opens on the watched folders
      Given the sync has found nothing yet
      When the player opens the sync
      Then the window shows the "Watched folders" board

  Rule: The status shows pairing, last success, queued uploads and actionable failures

    Scenario: The syncing board shows the stats and the recent activity
      Given the sync is "uploading"
      When the player opens the sync
      Then the stats read "Today, 14:05", "2 snapshots" and "3 of 4 watched"
      And the footer names "Bryn" and this computer
      And the recent activity reads:
        | name       | realm     | state                     |
        | Arthasdk   | Icecrown  | uploading                 |
        | Jainaice   | Lordaeron | waiting for WoW           |
        | Thrallsham | Icecrown  | Uploaded today, 14:05     |
        | Sylvanash  | Icecrown  | Uploaded yesterday, 14:05 |

    Scenario Outline: Each problem says what to do and offers a retry
      Given the sync is "<status>"
      When the player opens the sync
      Then the notice reads "<title>"
      And the retry button reads "<retry>"
      When the player retries
      Then the sync was asked to "read again"

      Examples:
        | status                      | title                                   | retry          |
        | holding an incomplete file  | Jainaice's snapshot is incomplete       | Retry Jainaice |
        | holding a refused snapshot  | RaidManager refused Jainaice's snapshot | Retry Jainaice |
        | holding an unsupported file | This addon version isn't supported      | Retry          |
        | holding an unreadable file  | This file isn't RaidManager's           | Retry          |

  Rule: The player pauses, resumes and retries uploads

    Scenario: Pausing and resuming
      Given the sync is "uploading"
      And the player opened the sync
      When the player pauses sync
      And the sync becomes paused
      And the player resumes sync
      Then the sync was asked to "pause, resume"

    Scenario: Retrying at once while offline
      Given the sync is "offline"
      And the player opened the sync
      When the player retries now
      Then the sync was asked to "retry now"
