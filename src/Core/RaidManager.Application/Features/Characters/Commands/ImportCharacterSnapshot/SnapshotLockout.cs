namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one saved instance.</summary>
/// <param name="Name">The localized instance name, such as <c>Icecrown Citadel</c>.</param>
/// <param name="LockoutId">The lower part of the lockout id.</param>
/// <param name="IdMostSig">The upper part of the lockout id.</param>
/// <param name="ResetSeconds">Seconds until the reset, counted from the section's observation.</param>
/// <param name="Difficulty">The game's raw difficulty: 3 and 4 are heroic.</param>
/// <param name="MaxPlayers">The instance size.</param>
/// <param name="IsRaid">Whether the instance is a raid.</param>
/// <param name="Locked">Whether the save is current; an expired save that can be extended is not.</param>
/// <param name="Extended">Whether the player extended the save.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one saved instance of the addon contract (#384).
/// </remarks>
public sealed record SnapshotLockout(
    string? Name,
    long LockoutId,
    long IdMostSig,
    long ResetSeconds,
    int Difficulty,
    int MaxPlayers,
    bool IsRaid,
    bool Locked,
    bool Extended);
