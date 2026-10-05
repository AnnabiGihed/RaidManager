namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes one of the player's characters in the My characters list, as the API returns it.</summary>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The class name, for example <c>DeathKnight</c>.</param>
/// <param name="Level">The character level.</param>
/// <param name="PrimaryLoadout">The primary loadout, or <see langword="null"/> before any loadout was synchronized.</param>
/// <param name="CurrentRaidSaveCount">The raid saves that haven't reset yet.</param>
/// <param name="LastSynchronizedAtUtc">The latest successful sync from any source, if any.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one row of board 1 of the character profile mockup (story #19).
/// </remarks>
public sealed record CharacterSummary(
    Guid CharacterId,
    string Realm,
    string Name,
    string Class,
    int Level,
    CharacterLoadoutSummary? PrimaryLoadout,
    int CurrentRaidSaveCount,
    DateTimeOffset? LastSynchronizedAtUtc);
