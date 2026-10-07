using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Represents one row of the sync's recent activity.</summary>
/// <param name="Character">The character.</param>
/// <param name="State">What the sync is doing with it.</param>
/// <param name="At">When it was uploaded, for <see cref="CharacterActivityState.Uploaded"/>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: A row of board 2's "Recent activity" (#550, #551).
/// </remarks>
public sealed record CharacterActivity(CharacterKey Character, CharacterActivityState State, DateTimeOffset? At = null);
