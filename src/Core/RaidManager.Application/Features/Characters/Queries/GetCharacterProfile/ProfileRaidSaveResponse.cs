using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Describes a current raid save on a profile.</summary>
/// <param name="Instance">The raid instance.</param>
/// <param name="Difficulty">The raid size and difficulty.</param>
/// <param name="LockoutId">The game lockout identifier, if known.</param>
/// <param name="ResetsAtUtc">When the save resets.</param>
/// <param name="IsExtended">Whether the character extended it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds the profile's Raid saves card with the synchronized saves only.
/// </remarks>
public sealed record ProfileRaidSaveResponse(RaidInstance Instance, RaidDifficulty Difficulty, string? LockoutId, DateTimeOffset ResetsAtUtc, bool IsExtended);
