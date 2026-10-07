using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Represents an account whose file needs the player's help.</summary>
/// <param name="AccountId">The account's hash.</param>
/// <param name="Kind">Why the file needs help.</param>
/// <param name="Characters">The characters it concerns, for an incomplete snapshot; empty when the file was cut before any.</param>
/// <param name="AddonVersion">The addon version that wrote the file, for an unsupported schema.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: An actionable failure the sync screens show, such as board 5's "Jainaice's snapshot is incomplete" (#550,
/// #551). The other characters keep syncing.
/// </remarks>
public sealed record AccountProblem(string AccountId, AccountProblemKind Kind, IReadOnlyList<CharacterKey> Characters, string? AddonVersion = null);
