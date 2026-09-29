# Account and character sequences

These planned sequences show the actor and component responsibilities behind onboarding and synchronization.
Application programming interface (API) messages are conceptual operations; endpoint paths are not yet defined.

## Discord sign-in

The server validates the Discord authorization result, establishes local identity, and checks for pending claims.
Newly discovered characters lead the next website sign-in to approval.

[![Discord sign-in sequence including denied authorization and the approval redirect](../diagrams/sequence-sign-in.svg)](../diagrams/sequence-sign-in.svg)

Source: [Mermaid](../diagrams/sequence-sign-in.mmd).

## Companion pairing

The signed-in player authorizes a specific companion request and can revoke it later.
The handshake is a proposed implementation shape; short expiry and revocation are required behavior.

[![Companion pairing and revocation sequence](../diagrams/sequence-pairing.svg)](../diagrams/sequence-pairing.svg)

Source: [Mermaid](../diagrams/sequence-pairing.mmd).

No game-account password or website authentication secret belongs in the addon data file.
The companion monitors only installations and accounts selected by the player.

## Snapshot synchronization

The game must flush saved data before the companion can upload it.
Retries keep the same snapshot identity, and delayed snapshots must not overwrite newer observations.

[![Snapshot synchronization with completed-file reading, retries, deduplication, and re-evaluation](../diagrams/character-sync.svg)](../diagrams/character-sync.svg)

Source: [Mermaid](../diagrams/character-sync.mmd).

A successful upload acknowledges ingestion, not character approval.
For example, a player can sync three newly visited characters while all three remain pending.
An unavailable service leaves the snapshot pending locally with a visible retry status.

## Character approval

Approval checks the current owner; rejection removes the candidate from signup choices.
Conflicts remain unresolved until an authorized evidence-review process resolves them.

[![Character approval sequence with approval, rejection, and ownership-conflict branches](../diagrams/sequence-character-approval.svg)](../diagrams/sequence-character-approval.svg)

Source: [Mermaid](../diagrams/sequence-character-approval.mmd).

A repeated upload does not reverse a rejection or transfer ownership.
The exact conflict-review authority and ownership evidence are design details requiring validation.
