using System.Globalization;
using RaidManager.ViewModels.Features.Shared;

namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Loads the characters a player owns and words each row of the My characters page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the state and wording of board 1 of the character profile mockup (story #19): primary loadout, raid saves and how fresh each character's data is.
/// </remarks>
public sealed class MyCharactersViewModel
{
    #region Constants
    /// <summary>Defines the note under the list, as board 1 words it.</summary>
    public const string FreshnessNote = "Data older than 3 days may be out of date: log in with that character so the companion syncs it.";

    /// <summary>Defines the label of a character never synchronized.</summary>
    public const string NeverSynced = "Not synced yet";
    #endregion Constants

    #region Fields
    /// <summary>Stores the age after which synchronized data may be out of date.</summary>
    public static readonly TimeSpan FreshFor = TimeSpan.FromDays(3);

    /// <summary>Stores the profiles API client.</summary>
    private readonly ICharacterProfilesApiClient _api;

    /// <summary>Stores the clock that ages each sync.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MyCharactersViewModel"/> class.</summary>
    /// <param name="api">The profiles API client.</param>
    /// <param name="timeProvider">The clock that ages each sync.</param>
    public MyCharactersViewModel(ICharacterProfilesApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the characters are loading, shown, or could not be loaded.</summary>
    public CharacterPageStatus Status { get; private set; } = CharacterPageStatus.Loading;

    /// <summary>Gets the player's characters, by realm and name.</summary>
    public IReadOnlyList<CharacterSummary> Characters { get; private set; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Gets the second line of a character's cell: its class and realm.</summary>
    /// <param name="character">The character.</param>
    /// <returns>For example <c>Death Knight · Icecrown</c>.</returns>
    public static string ClassAndRealm(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return $"{CharacterLabels.Class(character.Class)} · {character.Realm}";
    }

    /// <summary>Gets the primary loadout column.</summary>
    /// <param name="character">The character.</param>
    /// <returns>For example <c>Frost DPS · GearScore 5,712</c>, or a note before the first loadout sync.</returns>
    public static string LoadoutLabel(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return character.PrimaryLoadout is { } loadout
            ? $"{loadout.Name} · {CharacterLabels.GearScore(loadout.GearScore)}"
            : "No loadout synced yet";
    }

    /// <summary>Gets the raid saves column.</summary>
    /// <param name="character">The character.</param>
    /// <returns><c>None</c>, or the number of current saves, for example <c>2 this week</c>.</returns>
    public static string RaidSavesLabel(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return character.CurrentRaidSaveCount == 0
            ? "None"
            : string.Create(CultureInfo.InvariantCulture, $"{character.CurrentRaidSaveCount} this week");
    }

    /// <summary>Loads the player's characters.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the characters are shown or the failure is recorded.</returns>
    public async Task LoadAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            Status = CharacterPageStatus.Failed;
            return;
        }

        Status = CharacterPageStatus.Loading;
        try
        {
            Characters = await _api.GetCharactersAsync(id, cancellationToken);
            Status = CharacterPageStatus.Ready;
        }
        catch (Exception exception) when (CharacterApiFailures.IsApiFailure(exception, cancellationToken))
        {
            Status = CharacterPageStatus.Failed;
        }
    }

    /// <summary>Says how fresh a character's data is.</summary>
    /// <param name="character">The character.</param>
    /// <returns>Fresh within three days of the latest sync, stale after, never without one.</returns>
    public SyncFreshness Freshness(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LastSynchronizedAtUtc is not { } synced)
        {
            return SyncFreshness.Never;
        }

        return _timeProvider.GetUtcNow() - synced <= FreshFor ? SyncFreshness.Fresh : SyncFreshness.Stale;
    }

    /// <summary>Gets the last sync badge: the time today or yesterday, then the age in days.</summary>
    /// <param name="character">The character.</param>
    /// <returns>For example <c>Today, 14:05 UTC</c>, <c>5 days ago</c> or <c>Not synced yet</c>.</returns>
    public string SyncLabel(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.LastSynchronizedAtUtc is not { } synced)
        {
            return NeverSynced;
        }

        var now = _timeProvider.GetUtcNow();
        var days = (now.UtcDateTime.Date - synced.UtcDateTime.Date).Days;
        return days < 2
            ? UtcTimeLabel.Format(synced, now)
            : string.Create(CultureInfo.InvariantCulture, $"{days} days ago");
    }
    #endregion Public Methods
}
