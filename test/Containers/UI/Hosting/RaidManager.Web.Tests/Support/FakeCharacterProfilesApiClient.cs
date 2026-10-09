using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.Web.Tests.Support;

/// <summary>Plays the profiles API for the character page tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Returns the characters and profile a test sets, fails while told to, and counts the loads.
/// </remarks>
internal sealed class FakeCharacterProfilesApiClient : ICharacterProfilesApiClient
{
    #region Properties
    /// <summary>Gets or sets the characters returned by the list call.</summary>
    public List<CharacterSummary> Characters { get; set; } = [];

    /// <summary>Gets or sets the profile returned, or <see langword="null"/> for a character that isn't the player's.</summary>
    public CharacterProfile? Profile { get; set; }

    /// <summary>Gets or sets a value indicating whether every call fails as an unavailable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets the number of calls made.</summary>
    public int Loads { get; private set; }

    /// <summary>Gets the number of removals asked.</summary>
    public int Removals { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Builds Arthasdk's profile as board 2 shows it, with two loadouts, professions and a raid save.</summary>
    /// <param name="characterId">The character.</param>
    /// <returns>The profile.</returns>
    public static CharacterProfile Arthasdk(Guid characterId) =>
        new(
            characterId,
            "Icecrown",
            "Arthasdk",
            "DeathKnight",
            "Human",
            "Alliance",
            80,
            "Citadel Vanguard",
            "Community",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddHours(-1),
            [new CharacterProfession("Blacksmithing", 450, 450), new CharacterProfession("Mining", 450, 450)],
            [
                new CharacterLoadout(
                    "Frost DPS",
                    "MeleeDamage",
                    true,
                    5712,
                    "0/53/18",
                    "WowAddon",
                    [
                        new CharacterGearItem("Head", 51127, "|Hitem:51127|h[Sanctified Scourgelord Helmet]|h", 264),
                        new CharacterGearItem("TrinketTwo", 50343, "|Hitem:50343|h[Whispering Fanged Skull]|h", null),
                    ]),
                new CharacterLoadout("Blood Tank", "Tank", false, 5480, "51/10/10", "WowAddon", []),
            ],
            [new CharacterRaidSave("IcecrownCitadel", "TwentyFivePlayerHeroic", "43127", DateTimeOffset.UtcNow.AddDays(2), false)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(Guid userId, CancellationToken cancellationToken)
    {
        Loads++;
        return Fails
            ? Task.FromException<IReadOnlyList<CharacterSummary>>(new HttpRequestException("down"))
            : Task.FromResult<IReadOnlyList<CharacterSummary>>([.. Characters]);
    }

    /// <inheritdoc />
    public Task<CharacterProfile?> GetProfileAsync(Guid userId, Guid characterId, CancellationToken cancellationToken)
    {
        Loads++;
        return Fails ? Task.FromException<CharacterProfile?>(new HttpRequestException("down")) : Task.FromResult(Profile);
    }

    /// <inheritdoc />
    public Task<int> RemoveAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        Removals++;
        if (Fails)
        {
            return Task.FromException<int>(new HttpRequestException("down"));
        }

        var removed = Characters.Count;
        Characters = [];
        return Task.FromResult(removed);
    }
    #endregion Public Methods
}
