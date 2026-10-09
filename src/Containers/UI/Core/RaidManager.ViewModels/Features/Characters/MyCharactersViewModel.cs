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

    /// <summary>Defines the line of the notice shown while characters wait for review (board 5 of character-sync).</summary>
    public const string WaitingMessage = "Approve the ones that are yours so they can sign up for raids.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the age after which synchronized data may be out of date.</summary>
    public static readonly TimeSpan FreshFor = TimeSpan.FromDays(3);

    /// <summary>Stores the profiles API client.</summary>
    private readonly ICharacterProfilesApiClient _api;

    /// <summary>Stores the claims API client, which tells how many characters wait for review.</summary>
    private readonly ICharacterClaimsApiClient _claims;

    /// <summary>Stores the clock that ages each sync.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MyCharactersViewModel"/> class.</summary>
    /// <param name="api">The profiles API client.</param>
    /// <param name="claims">The claims API client, which counts the characters waiting for review.</param>
    /// <param name="timeProvider">The clock that ages each sync.</param>
    public MyCharactersViewModel(ICharacterProfilesApiClient api, ICharacterClaimsApiClient claims, TimeProvider timeProvider)
    {
        _api = api;
        _claims = claims;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the characters are loading, shown, or could not be loaded.</summary>
    public CharacterPageStatus Status { get; private set; } = CharacterPageStatus.Loading;

    /// <summary>Gets the player's characters, by realm and name.</summary>
    public IReadOnlyList<CharacterSummary> Characters { get; private set; } = [];

    /// <summary>Gets the number of characters waiting for the player's review; the notice shows while it isn't 0.</summary>
    public int WaitingCount { get; private set; }

    /// <summary>Gets the title of the notice, such as "2 characters wait for your review".</summary>
    public string WaitingTitle => WaitingCount == 1
        ? "1 character waits for your review"
        : $"{WaitingCount} characters wait for your review";
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

    /// <summary>Counts the characters waiting for the player's review, for the notice that leads to the review page.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the count is known; a failure counts none, so no notice shows (#595).</returns>
    public async Task LoadWaitingAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            WaitingCount = 0;
            return;
        }

        try
        {
            var claims = await _claims.GetPendingAsync(id, cancellationToken);
            WaitingCount = claims.Count(claim => claim.IsPending);
        }
        catch (Exception exception) when (CharacterApiFailures.IsApiFailure(exception, cancellationToken))
        {
            WaitingCount = 0;
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
