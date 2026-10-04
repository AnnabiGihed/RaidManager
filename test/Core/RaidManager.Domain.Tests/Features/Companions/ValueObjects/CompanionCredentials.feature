Feature: Companion credentials
  As a player
  I want pairing codes easy to read and secrets kept only as hashes
  So that I confirm the right computer and a database copy can't upload for me

  Rule: A pairing code is six symbols without look-alike characters

    Scenario Outline: Typed codes are normalized
      When the code "<typed>" is read
      Then the code reads as "<shown>"

      Examples:
        | typed   | shown   |
        | K7M-4QX | K7M-4QX |
        | k7m4qx  | K7M-4QX |
        | K7M 4QX | K7M-4QX |

    Scenario Outline: Other values are not pairing codes
      When the code "<typed>" is read
      Then the code is refused

      Examples:
        | typed    |
        | K7M-4Q   |
        | K7M-4QXA |
        | K0M-4QX  |
        | KLM-4QX  |
        |          |

  Rule: A secret is kept as its SHA-256 hash

    Scenario: The same secret gives the same hash
      When the secret "device-token" is hashed twice
      Then both hashes are equal
      And the hash has 64 lower-case hexadecimal characters
      And the hash is restored from its stored form

    Scenario: A stored hash is restored only when it is a SHA-256 hash
      When the stored hash "NOT-A-HASH" is restored
      Then the stored hash is refused
