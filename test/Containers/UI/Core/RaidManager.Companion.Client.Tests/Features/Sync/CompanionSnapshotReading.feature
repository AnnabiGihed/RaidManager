Feature: Companion snapshot reading
  As a player
  I want the companion to read every character the addon saved without running the addon's file
  So that RaidManager gets my characters exactly as the game reported them

  Rule: A complete file gives every character it holds

    Scenario Outline: Each character of a complete file is read
      Given the addon file of the "<fixture>" fixture for the account "<account>"
      When the companion reads the file
      Then the file reads as "Complete"
      And the characters read are "<characters>"

      Examples:
        | fixture           | account       | characters                             |
        | one-character     | ARTHASACCOUNT | Arthasdk (Icecrown)                    |
        | raid-save         | ARTHASACCOUNT | Arthasdk (Icecrown)                    |
        | multiple-accounts | MAINACCOUNT   | Arthasdk (Icecrown), Jaína (Lordaeron) |
        | multiple-accounts | ALTACCOUNT    | Bankalt (Icecrown)                     |

    Scenario: A character keeps the capture time the addon wrote
      Given the addon file of the "one-character" fixture for the account "ARTHASACCOUNT"
      When the companion reads the file
      Then the character "Arthasdk" was captured at 1791043200
      And the character "Arthasdk" keeps its class "DEATHKNIGHT"

  Rule: A file WoW didn't finish writing keeps the characters written before the cut

    Scenario: The character cut by the end of the file is incomplete
      Given the addon file of the "multiple-accounts" fixture for the account "MAINACCOUNT"
      And the file ends inside the character "Jaína"
      When the companion reads the file
      Then the file reads as "Incomplete"
      And the characters read are "Arthasdk (Icecrown)"
      And the incomplete characters are "Jaína (Lordaeron)"

    Scenario: A file cut before its schema version gives no character
      Given the addon file of the "one-character" fixture for the account "ARTHASACCOUNT"
      And the file ends after 2 lines
      When the companion reads the file
      Then the file reads as "Incomplete"
      And the characters read are "none"

    Scenario: A character without its capture time is incomplete
      Given the addon file of the "one-character" fixture for the account "ARTHASACCOUNT"
      And the character entries lack their capture time
      When the companion reads the file
      Then the file reads as "Incomplete"
      And the incomplete characters are "Arthasdk (Icecrown)"

  Rule: Only a file of schema version 1 gives characters

    Scenario: A file of another schema version gives no character
      Given the addon file of the "one-character" fixture for the account "ARTHASACCOUNT"
      And the file's schema version is 2
      When the companion reads the file
      Then the file reads as "UnsupportedSchema"
      And the characters read are "none"

    Scenario Outline: A file that isn't the addon's is unreadable
      Given a file that holds "<content>"
      When the companion reads the file
      Then the file reads as "Unreadable"
      And the characters read are "none"

      Examples:
        | content                       |
        | OtherAddonDB = { }            |
        | RaidManagerDB = { }           |
        | RaidManagerDB = 42            |
        | <html>not a saved file</html> |
