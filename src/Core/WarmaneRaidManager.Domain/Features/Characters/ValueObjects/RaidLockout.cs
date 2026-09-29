using WarmaneRaidManager.Domain.Features.Characters.Enums;
using WarmaneRaidManager.Domain.Features.Shared.Enums;

namespace WarmaneRaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents an authoritative saved-instance state belonging to a character.</summary>
/// <param name="Instance">The raid instance.</param>
/// <param name="Difficulty">The raid size and difficulty.</param>
/// <param name="LockoutId">The optional game lockout identifier.</param>
/// <param name="ResetsAtUtc">The UTC instant when the lockout is expected to reset.</param>
/// <param name="IsExtended">Whether the character extended the lockout.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Models raid eligibility at character level rather than incorrectly attaching it to a specialization or loadout.
/// </remarks>
public sealed record RaidLockout(
    RaidInstance Instance,
    RaidDifficulty Difficulty,
    string? LockoutId,
    DateTimeOffset ResetsAtUtc,
    bool IsExtended);
