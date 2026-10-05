using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Describes one of the player's characters in the My characters list, as the website receives it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Keeps the wire format separate from domain types: realm, class and role travel as names.
/// </remarks>
/// <param name="CharacterId">The character identifier.</param>
/// <param name="Realm">The Warmane realm name, for example <c>Icecrown</c>.</param>
/// <param name="Name">The character name.</param>
/// <param name="Class">The class name, for example <c>DeathKnight</c>.</param>
/// <param name="Level">The character level.</param>
/// <param name="PrimaryLoadout">The primary loadout, or <see langword="null"/> before any loadout was synchronized.</param>
/// <param name="CurrentRaidSaveCount">The raid saves that haven't reset yet.</param>
/// <param name="LastSynchronizedAtUtc">The latest successful sync from any source, if any.</param>
public sealed record CharacterSummary(
    Guid CharacterId,
    string Realm,
    string Name,
    string Class,
    int Level,
    CharacterLoadoutSummary? PrimaryLoadout,
    int CurrentRaidSaveCount,
    DateTimeOffset? LastSynchronizedAtUtc)
{
    #region Public Methods
    /// <summary>Maps the application response to the transport record.</summary>
    /// <param name="character">The application response.</param>
    /// <returns>The transport record.</returns>
    public static CharacterSummary From(CharacterSummaryResponse character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return new CharacterSummary(
            character.CharacterId,
            character.Realm.ToString(),
            character.Name,
            character.Class.ToString(),
            character.Level,
            character.PrimaryLoadout is { } loadout ? new CharacterLoadoutSummary(loadout.Name, loadout.Role.ToString(), loadout.GearScore) : null,
            character.CurrentRaidSaveCount,
            character.LastSynchronizedAtUtc);
    }
    #endregion Public Methods
}
