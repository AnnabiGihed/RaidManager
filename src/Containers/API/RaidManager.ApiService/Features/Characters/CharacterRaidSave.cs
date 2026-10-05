namespace RaidManager.ApiService.Features.Characters;

/// <summary>Describes a current raid save on a profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one row of the profile's Raid saves card.
/// </remarks>
/// <param name="Instance">The instance name, for example <c>IcecrownCitadel</c>.</param>
/// <param name="Difficulty">The difficulty name, for example <c>TwentyFivePlayerHeroic</c>.</param>
/// <param name="LockoutId">The game lockout identifier, if known.</param>
/// <param name="ResetsAtUtc">When the save resets.</param>
/// <param name="IsExtended">Whether the character extended it.</param>
public sealed record CharacterRaidSave(string Instance, string Difficulty, string? LockoutId, DateTimeOffset ResetsAtUtc, bool IsExtended);
