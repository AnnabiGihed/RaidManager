Feature: Companion folder discovery
  As a player
  I want the companion to find my World of Warcraft folders on its own
  So that I don't have to look for them before my characters sync

  Rule: The search finds the game in the usual places of a fixed drive

    Scenario Outline: An installation down to three folders deep is found
      Given World of Warcraft is installed in "<folder>"
      When the companion searches the drives
      Then the installations found are "<found>"

      Examples:
        | folder                                  | found                                 |
        | World of Warcraft                       | World of Warcraft                     |
        | Program Files (x86)/World of Warcraft   | Program Files (x86)/World of Warcraft |
        | Games/Warmane/World of Warcraft         | Games/Warmane/World of Warcraft       |
        | Games/Private/Servers/World of Warcraft | none                                  |
        | Windows/World of Warcraft               | none                                  |
        | Users/Bryn/World of Warcraft            | none                                  |

    Scenario: A folder with accounts but without the game isn't found
      Given a folder "Backups/WoW" holds account folders without the game
      When the companion searches the drives
      Then the installations found are "none"

    Scenario: Two installations on one drive are both found
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft"
      And World of Warcraft is installed in "WoW/Warmane"
      When the companion searches the drives
      Then the installations found are "Games/Warmane/World of Warcraft, WoW/Warmane"

  Rule: An installation lists its accounts with their characters

    Scenario: Each account lists how many characters it has
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft"
      And the account "ARTHASACC" has the characters "Icecrown/Arthasdk, Icecrown/Sylvanash, Lordaeron/Jainaice"
      And the account "ALTACC" has the characters "Icecrown/Bankalt"
      When the companion reads the installation
      Then the accounts are "ALTACC (1), ARTHASACC (3)"

    Scenario: The shared settings folder isn't an account
      Given World of Warcraft is installed in "Games/Warmane/World of Warcraft"
      And the account "ARTHASACC" has the characters "Icecrown/Arthasdk"
      And the installation has a shared settings folder
      When the companion reads the installation
      Then the accounts are "ARTHASACC (1)"
