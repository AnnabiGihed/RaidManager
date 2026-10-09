Feature: Companion background sync
  As a player
  I want the companion to watch my WoW folders and upload complete snapshots on its own
  So that my characters reach RaidManager without copying files

  Background:
    Given this computer is paired

  Rule: The first start searches the drives and later starts keep what it found

    Scenario: The first start watches the accounts found
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft" with the account "ARTHASACC"
      When the companion starts syncing
      Then the watched accounts are "ARTHASACC"

    Scenario: A later start keeps the folders of the first search
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft" with the account "ARTHASACC"
      And the companion has started syncing
      And World of Warcraft is installed in "WoW/Warmane" with the account "THRALLACC"
      When the companion restarts
      Then the watched accounts are "ARTHASACC"

    Scenario: Finding folders again adds a new installation
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft" with the account "ARTHASACC"
      And the companion has started syncing
      And World of Warcraft is installed in "WoW/Warmane" with the account "THRALLACC"
      When the player finds folders again
      Then the watched accounts are "ARTHASACC, THRALLACC"

    Scenario Outline: A folder the player chooses is watched only when it holds accounts
      Given a folder "Backups/WoW" holds the account "ALTACC" without the game
      And the companion has started syncing
      When the player chooses the folder "<folder>"
      Then the folder is "<answer>"
      And the watched accounts are "<watched>"

      Examples:
        | folder      | answer   | watched |
        | Backups/WoW | accepted | ALTACC  |
        | Backups     | refused  | none    |

  Rule: A snapshot uploads once WoW has finished writing it

    Scenario: A file unchanged between two scans uploads its characters
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is the "multiple-accounts" fixture
      And the companion has started syncing
      When the companion syncs for 5 seconds
      Then RaidManager received "Arthasdk, Jaína"
      And the status shows 0 snapshots queued

    Scenario: A file written again before the next scan isn't read yet
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the companion has started syncing
      And WoW writes the addon file of "ARTHASACCOUNT" again
      When the companion syncs for 5 seconds
      Then RaidManager received "none"

    Scenario: An unchanged file uploads once
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the companion has started syncing
      When the companion syncs for 30 seconds
      Then RaidManager received "Arthasdk"
      And the activity shows "Arthasdk" as "Uploaded"

    Scenario: A file of another schema version needs the player
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is written by schema version 2
      And the companion has started syncing
      When the companion syncs for 5 seconds
      Then the problems are "UnsupportedSchema"
      And RaidManager received "none"

    Scenario: A file the addon didn't write needs the player
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" holds "OtherAddonDB = { }"
      And the companion has started syncing
      When the companion syncs for 5 seconds
      Then the problems are "UnreadableFile"

  Rule: A cut file waits for WoW then asks the player for help

    Scenario: The characters before the cut upload while the cut one waits for WoW
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is cut inside "Jaína"
      And the companion has started syncing
      When the companion syncs for 5 seconds
      Then RaidManager received "Arthasdk"
      And the activity shows "Jaína" as "WaitingForWow"

    Scenario: A file still cut after 30 seconds is an incomplete snapshot
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is cut inside "Jaína"
      And the companion has started syncing
      When the companion syncs for 35 seconds
      Then the problems are "IncompleteSnapshot: Jaína"

    Scenario: Writing the file in full clears the problem
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is cut inside "Jaína"
      And the companion has synced for 35 seconds
      And the addon file of "MAINACCOUNT" is the "multiple-accounts" fixture
      When the companion syncs for 10 seconds
      Then the problems are "none"
      And RaidManager received "Arthasdk, Jaína"

    Scenario: Reading again retries a cut file
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is cut inside "Jaína"
      And the companion has synced for 35 seconds
      When the player reads the files again
      Then the problems are "none"
      And the activity shows "Jaína" as "WaitingForWow"

  Rule: A pause stops uploads but not reading

    Scenario: A paused sync queues without uploading
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the player has paused sync
      When the companion syncs for 5 seconds
      Then RaidManager received "none"
      And the status shows 1 snapshots queued

    Scenario: Resuming uploads the waiting snapshots at once
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the player has paused sync
      And the companion has synced for 5 seconds
      When the player resumes sync
      Then RaidManager received "Arthasdk"

    Scenario: A pause outlasts a restart
      Given the player has paused sync
      When the companion restarts
      Then the sync is "paused"

  Rule: An excluded account is neither read nor uploaded

    Scenario: An excluded account isn't read
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the player has excluded the account "ARTHASACCOUNT"
      When the companion syncs for 5 seconds
      Then RaidManager received "none"
      And the status shows 0 of 1 accounts watched

    Scenario: Excluding an account drops its waiting snapshots
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the player has paused sync
      And the companion has synced for 5 seconds
      When the player excludes the account "ARTHASACCOUNT"
      Then the status shows 0 snapshots queued

    Scenario: Watching an account again reads it
      Given World of Warcraft is installed in "World of Warcraft" with the account "ARTHASACCOUNT"
      And the addon file of "ARTHASACCOUNT" is the "one-character" fixture
      And the player has excluded the account "ARTHASACCOUNT"
      And the player watches the account "ARTHASACCOUNT" again
      When the companion syncs for 5 seconds
      Then RaidManager received "Arthasdk"

  Rule: A request to sync again sends every character again, once

    Scenario: A request to sync again uploads every character again
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is the "multiple-accounts" fixture
      And the companion has started syncing
      And the companion has synced for 30 seconds
      And the player asked to send every character again
      When the companion syncs for 70 seconds
      Then RaidManager received "Arthasdk, Jaína, Arthasdk, Jaína"

    Scenario: A request is carried out once
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is the "multiple-accounts" fixture
      And the companion has started syncing
      And the companion has synced for 30 seconds
      And the player asked to send every character again
      When the companion syncs for 190 seconds
      Then RaidManager received "Arthasdk, Jaína, Arthasdk, Jaína"
      And the sync asked about a request 4 times

    Scenario: A request made while the companion was off is carried out at the next start
      Given World of Warcraft is installed in "World of Warcraft" with the account "MAINACCOUNT"
      And the addon file of "MAINACCOUNT" is the "multiple-accounts" fixture
      And the companion has started syncing
      And the companion has synced for 30 seconds
      And the player asked to send every character again
      When the companion restarts
      And the companion syncs for 10 seconds
      Then RaidManager received "Arthasdk, Jaína, Arthasdk, Jaína"
