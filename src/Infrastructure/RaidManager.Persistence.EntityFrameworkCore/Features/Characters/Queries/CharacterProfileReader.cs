using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RaidManager.Application.Features.Characters.Abstractions;
using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Queries;

/// <summary>Reads character profiles with no-tracking queries against the write database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Implements the read side of story #19 as ADR-0010 decides: always current, no projections, nothing
/// attached to the change tracker. Every query keeps to characters the player owns through an approved claim (owner
/// decision on #19, 2026-10-05).
/// </remarks>
internal sealed class CharacterProfileReader : ICharacterProfileReader
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfileReader"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CharacterProfileReader(RaidManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<IReadOnlyList<CharacterSummaryResponse>> ListOwnedAsync(UserId userId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var rows = await OwnedBy(userId)
            .OrderBy(character => character.Realm)
            .ThenBy(character => character.Name)
            .Select(character => new
            {
                character.Id,
                character.Realm,
                character.Name,
                character.Class,
                character.Level,
                Primary = character.Loadouts
                    .Where(loadout => loadout.IsPrimary)
                    .Select(loadout => new { loadout.Name, loadout.Role, loadout.GearScore })
                    .FirstOrDefault(),
                CurrentRaidSaves = character.RaidLockouts.Count(lockout => lockout.ResetsAtUtc > nowUtc),
                character.LastAddonSynchronizedAtUtc,
                character.LastArmorySynchronizedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new CharacterSummaryResponse(
            row.Id.Value,
            row.Realm,
            row.Name.Value,
            row.Class,
            row.Level,
            row.Primary is null ? null : new LoadoutSummaryResponse(row.Primary.Name, row.Primary.Role, row.Primary.GearScore?.Value),
            row.CurrentRaidSaves,
            Latest(row.LastAddonSynchronizedAtUtc, row.LastArmorySynchronizedAtUtc)));
    }

    /// <inheritdoc />
    public async Task<CharacterProfileResponse?> FindOwnedProfileAsync(UserId userId, CharacterId characterId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var character = await OwnedBy(userId).SingleOrDefaultAsync(candidate => candidate.Id == characterId, cancellationToken);
        return character is null ? null : ToProfile(character, nowUtc);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Returns the later of two optional instants.</summary>
    /// <param name="first">The first instant.</param>
    /// <param name="second">The second instant.</param>
    /// <returns>The later instant, or <see langword="null"/> when both are missing.</returns>
    private static DateTimeOffset? Latest(DateTimeOffset? first, DateTimeOffset? second) => first > second || second is null ? first : second;

    /// <summary>Maps a character loaded without tracking to its profile.</summary>
    /// <param name="character">The character, with its owned collections.</param>
    /// <param name="nowUtc">The instant before which a raid save has expired.</param>
    /// <returns>The profile.</returns>
    private static CharacterProfileResponse ToProfile(Character character, DateTimeOffset nowUtc) =>
        new(
            character.Id.Value,
            character.Realm,
            character.Name.Value,
            character.Class,
            character.Race,
            character.Faction,
            character.Level,
            character.GuildName,
            character.Visibility,
            new ProfileSyncResponse(character.LastAddonSynchronizedAtUtc, character.LastArmorySynchronizedAtUtc, character.LastCompleteRaidSaveScanAtUtc),
            [.. character.Professions.Select(profession => new ProfileProfessionResponse(profession.Name, profession.Rank, profession.MaxRank))],
            [.. character.Loadouts
                .OrderByDescending(loadout => loadout.IsPrimary)
                .ThenBy(loadout => loadout.Name, StringComparer.OrdinalIgnoreCase)
                .Select(loadout => new ProfileLoadoutResponse(
                    loadout.Name,
                    loadout.Role,
                    loadout.IsPrimary,
                    loadout.GearScore?.Value,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{loadout.TalentConfiguration.FirstTreePoints}/{loadout.TalentConfiguration.SecondTreePoints}/{loadout.TalentConfiguration.ThirdTreePoints}"),
                    loadout.Source,
                    [.. loadout.GearItems
                        .OrderBy(item => item.Slot)
                        .Select(item => new ProfileGearItemResponse(item.Slot, item.ItemId, item.ItemLink, item.ItemLevel))]))],
            [.. character.RaidLockouts
                .Where(lockout => lockout.ResetsAtUtc > nowUtc)
                .Select(lockout => new ProfileRaidSaveResponse(lockout.Instance, lockout.Difficulty, lockout.LockoutId, lockout.ResetsAtUtc, lockout.IsExtended))]);

    /// <summary>Selects the characters a player owns through an approved claim, without tracking.</summary>
    /// <param name="userId">The player.</param>
    /// <returns>The query.</returns>
    private IQueryable<Character> OwnedBy(UserId userId) =>
        _dbContext.Characters
            .AsNoTracking()
            .Where(character => !character.IsDeleted && character.IsOwnershipVerified && character.OwnerId == userId);
    #endregion Private Helpers
}
