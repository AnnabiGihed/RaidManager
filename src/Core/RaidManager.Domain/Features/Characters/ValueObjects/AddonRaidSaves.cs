namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents a saved-instance scan read by the addon.</summary>
/// <param name="IsComplete">Whether every saved instance the game counted was read.</param>
/// <param name="Lockouts">The current WotLK raid saves the scan found.</param>
/// <param name="ObservedAtUtc">The UTC instant of the game's answer.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the lockouts section of an addon snapshot: only a complete scan replaces the raid saves (#384).
/// </remarks>
public sealed record AddonRaidSaves(bool IsComplete, IReadOnlyList<RaidLockout> Lockouts, DateTimeOffset ObservedAtUtc);
