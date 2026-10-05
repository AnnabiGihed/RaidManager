using System.Globalization;
using RaidManager.ViewModels.Features.Shared;

namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Loads one of the player's characters and words its profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the state and wording of board 2 of the character profile mockup (story #19): data sources, professions, visibility, loadouts, raid saves and the primary loadout's equipment.
/// </remarks>
public sealed class CharacterProfileViewModel
{
    #region Constants
    /// <summary>Defines the words for a source that never synchronized.</summary>
    public const string NeverSynced = "Not synced yet";

    /// <summary>Defines the words for a profile without a note.</summary>
    public const string NoNote = "No note yet.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the profiles API client.</summary>
    private readonly ICharacterProfilesApiClient _api;

    /// <summary>Stores the clock that words each sync.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfileViewModel"/> class.</summary>
    /// <param name="api">The profiles API client.</param>
    /// <param name="timeProvider">The clock that words each sync.</param>
    public CharacterProfileViewModel(ICharacterProfilesApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the profile is loading, shown, missing, or could not be loaded.</summary>
    public CharacterPageStatus Status { get; private set; } = CharacterPageStatus.Loading;

    /// <summary>Gets the profile once loaded.</summary>
    public CharacterProfile? Profile { get; private set; }

    /// <summary>Gets the primary loadout, whose equipment the profile shows, if any.</summary>
    public CharacterLoadout? PrimaryLoadout => Profile?.Loadouts.FirstOrDefault(loadout => loadout.IsPrimary);

    /// <summary>Gets the eyebrow above the name.</summary>
    /// <value>For example <c>Death Knight · Level 80</c>.</value>
    public string Eyebrow => Profile is { } profile
        ? string.Create(CultureInfo.InvariantCulture, $"{CharacterLabels.Class(profile.Class)} · Level {profile.Level}")
        : string.Empty;

    /// <summary>Gets the line under the name: realm, faction and guild.</summary>
    /// <value>For example <c>Icecrown · Alliance · &lt;Citadel Vanguard&gt;</c>.</value>
    public string Subtitle => Profile is { } profile
        ? string.Join(" · ", new[] { profile.Realm, profile.Faction, profile.GuildName is null ? null : $"<{profile.GuildName}>" }.OfType<string>())
        : string.Empty;

    /// <summary>Gets who sees the profile, as the Note and visibility card writes it.</summary>
    public string VisibilityLabel => Profile?.Visibility == "OfficersOnly" ? "Officers only" : "Community";

    /// <summary>Gets the subtitle of the Equipment card.</summary>
    public string EquipmentSubtitle => PrimaryLoadout is { } loadout ? $"Primary loadout, {loadout.Name}" : "No loadout synced yet";
    #endregion Properties

    #region Public Methods
    /// <summary>Gets a profession row's title.</summary>
    /// <param name="profession">The profession.</param>
    /// <returns>For example <c>Blacksmithing 450</c>.</returns>
    public static string ProfessionLabel(CharacterProfession profession)
    {
        ArgumentNullException.ThrowIfNull(profession);
        return string.Create(CultureInfo.InvariantCulture, $"{profession.Name} {profession.Rank}");
    }

    /// <summary>Gets a loadout's role line.</summary>
    /// <param name="loadout">The loadout.</param>
    /// <returns>For example <c>Melee damage · GearScore 5,712</c>.</returns>
    public static string RoleLabel(CharacterLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        return $"{CharacterLabels.Role(loadout.Role)} · {CharacterLabels.GearScore(loadout.GearScore)}";
    }

    /// <summary>Gets a loadout's talents line.</summary>
    /// <param name="loadout">The loadout.</param>
    /// <returns>For example <c>Talents 0/53/18 · from the addon</c>.</returns>
    public static string TalentsLabel(CharacterLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        return $"Talents {loadout.Talents} · {CharacterLabels.Source(loadout.Source)}";
    }

    /// <summary>Gets a raid save row's title.</summary>
    /// <param name="save">The raid save.</param>
    /// <returns>For example <c>Icecrown Citadel · 25 players, heroic</c>.</returns>
    public static string RaidSaveTitle(CharacterRaidSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        return $"{CharacterLabels.Instance(save.Instance)} · {CharacterLabels.Difficulty(save.Difficulty)}";
    }

    /// <summary>Gets a raid save row's detail: when it resets, its identifier and whether it was extended.</summary>
    /// <param name="save">The raid save.</param>
    /// <returns>For example <c>Resets Wed 7 Oct, 04:00 UTC · ID 43127</c>.</returns>
    public static string RaidSaveDetail(CharacterRaidSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        var resets = save.ResetsAtUtc.ToUniversalTime().ToString("ddd d MMM, HH:mm", CultureInfo.InvariantCulture);
        var parts = new List<string> { $"Resets {resets} UTC" };
        if (!string.IsNullOrWhiteSpace(save.LockoutId))
        {
            parts.Add($"ID {save.LockoutId}");
        }

        if (save.IsExtended)
        {
            parts.Add("extended");
        }

        return string.Join(" · ", parts);
    }

    /// <summary>Loads the profile.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="characterId">The character.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the profile is shown or the outcome is recorded.</returns>
    public async Task LoadAsync(Guid? userId, Guid characterId, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            Status = CharacterPageStatus.Failed;
            return;
        }

        Status = CharacterPageStatus.Loading;
        try
        {
            Profile = await _api.GetProfileAsync(id, characterId, cancellationToken);
            Status = Profile is null ? CharacterPageStatus.NotFound : CharacterPageStatus.Ready;
        }
        catch (Exception exception) when (CharacterApiFailures.IsApiFailure(exception, cancellationToken))
        {
            Status = CharacterPageStatus.Failed;
        }
    }

    /// <summary>Gets the Data sources card's rows: the addon, the Warmane Armory and the raid-save scan.</summary>
    /// <returns>Each source's name and when it last succeeded.</returns>
    public IReadOnlyList<(string Source, string Detail)> DataSources()
    {
        if (Profile is not { } profile)
        {
            return [];
        }

        return
        [
            ("WoW addon", Since("Last sync", profile.AddonSynchronizedAtUtc)),
            ("Warmane Armory", Since("Last sync", profile.ArmorySynchronizedAtUtc)),
            ("Raid saves", Since("Complete scan", profile.CompleteRaidSaveScanAtUtc)),
        ];
    }

    /// <summary>Gets the subtitle of the Raid saves card: when the latest complete scan ran.</summary>
    /// <returns>For example <c>Complete scan today, 14:05 UTC</c>.</returns>
    public string RaidSavesSubtitle() => Since("Complete scan", Profile?.CompleteRaidSaveScanAtUtc);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Words when something last happened, after a lead such as <c>Last sync</c>.</summary>
    /// <param name="lead">The words before the time.</param>
    /// <param name="moment">The moment, if any.</param>
    /// <returns>For example <c>Last sync today, 14:05 UTC</c>, or <c>Not synced yet</c>.</returns>
    private string Since(string lead, DateTimeOffset? moment)
    {
        if (moment is not { } at)
        {
            return NeverSynced;
        }

        var label = UtcTimeLabel.Format(at, _timeProvider.GetUtcNow());
        return $"{lead} {char.ToLowerInvariant(label[0])}{label[1..]}";
    }
    #endregion Private Helpers
}
