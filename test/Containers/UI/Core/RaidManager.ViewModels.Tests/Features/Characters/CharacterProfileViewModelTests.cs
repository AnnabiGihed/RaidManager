using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Verifies the state and wording of a character's profile page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Covers board 2 of the character profile mockup (story #19): loading, a character that isn't the player's,
/// failures, and every card's wording.
/// </remarks>
public sealed class CharacterProfileViewModelTests
{
    #region Fields
    /// <summary>Stores the instant the tests treat as now: Monday 5 October 2026, 18:00 UTC.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCharacterProfilesApi _api = new();

    /// <summary>Stores the view model under test.</summary>
    private readonly CharacterProfileViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfileViewModelTests"/> class.</summary>
    public CharacterProfileViewModelTests()
    {
        _viewModel = new CharacterProfileViewModel(_api, new FixedTimeProvider(Now));
    }
    #endregion Constructors

    #region Tests
    /// <summary>Loads the profile and words its header, sources, visibility and equipment.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LoadShowsTheProfileWithTheMockupWording()
    {
        _api.Profile = Profile();
        var userId = Guid.NewGuid();

        await _viewModel.LoadAsync(userId, _api.Profile.CharacterId, CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterPageStatus.Ready);
        _api.ProfileCalls.ShouldBe([(userId, _api.Profile.CharacterId)]);
        _viewModel.Eyebrow.ShouldBe("Death Knight · Level 80");
        _viewModel.Subtitle.ShouldBe("Icecrown · Alliance · <Citadel Vanguard>");
        _viewModel.VisibilityLabel.ShouldBe("Community");
        _viewModel.PrimaryLoadout.ShouldNotBeNull().Name.ShouldBe("Frost DPS");
        _viewModel.EquipmentSubtitle.ShouldBe("Primary loadout, Frost DPS");
        _viewModel.DataSources().ShouldBe(
        [
            ("WoW addon", "Last sync today, 14:05 UTC"),
            ("Warmane Armory", "Last sync yesterday, 09:12 UTC"),
            ("Raid saves", CharacterProfileViewModel.NeverSynced),
        ]);
        _viewModel.RaidSavesSubtitle().ShouldBe(CharacterProfileViewModel.NeverSynced);
    }

    /// <summary>Words a profile without guild, primary loadout, and visible to officers only.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SparseProfileUsesItsFallbacks()
    {
        _api.Profile = Profile() with { GuildName = null, Visibility = "OfficersOnly", Loadouts = [], CompleteRaidSaveScanAtUtc = Now.AddHours(-1) };

        await _viewModel.LoadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _viewModel.Subtitle.ShouldBe("Icecrown · Alliance");
        _viewModel.VisibilityLabel.ShouldBe("Officers only");
        _viewModel.PrimaryLoadout.ShouldBeNull();
        _viewModel.EquipmentSubtitle.ShouldBe("No loadout synced yet");
        _viewModel.RaidSavesSubtitle().ShouldBe("Complete scan today, 17:00 UTC");
    }

    /// <summary>Shows not found for a character that isn't the player's.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MissingProfileIsNotFound()
    {
        await _viewModel.LoadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterPageStatus.NotFound);
        _viewModel.Eyebrow.ShouldBeEmpty();
        _viewModel.Subtitle.ShouldBeEmpty();
        _viewModel.DataSources().ShouldBeEmpty();
    }

    /// <summary>Fails the load without a user id, on an HTTP failure and on a timeout.</summary>
    /// <param name="failure">Which failure: <c>no-user</c>, <c>http</c> or <c>timeout</c>.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("no-user")]
    [InlineData("http")]
    [InlineData("timeout")]
    public async Task LoadFailures(string failure)
    {
        _api.Failure = failure == "http" ? new HttpRequestException("down") : new TaskCanceledException("slow");

        await _viewModel.LoadAsync(failure == "no-user" ? null : Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterPageStatus.Failed);
    }

    /// <summary>Lets a cancellation from the page itself through, so leaving the page shows nothing.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LeavingThePageDoesNotShowAFailure()
    {
        using var lifetime = new CancellationTokenSource();
        await lifetime.CancelAsync();
        _api.Failure = new TaskCanceledException("left");

        await Should.ThrowAsync<TaskCanceledException>(() => _viewModel.LoadAsync(Guid.NewGuid(), Guid.NewGuid(), lifetime.Token));
    }

    /// <summary>Words the professions, loadouts and raid saves as board 2 does.</summary>
    [Fact]
    public void RowsUseTheMockupWording()
    {
        var frost = Profile().Loadouts[0];

        CharacterProfileViewModel.ProfessionLabel(new CharacterProfession("Blacksmithing", 450, 450)).ShouldBe("Blacksmithing 450");
        CharacterProfileViewModel.RoleLabel(frost).ShouldBe("Melee damage · GearScore 5,712");
        CharacterProfileViewModel.TalentsLabel(frost).ShouldBe("Talents 0/53/18 · from the addon");
        var save = new CharacterRaidSave("IcecrownCitadel", "TwentyFivePlayerHeroic", "43127", new DateTimeOffset(2026, 10, 7, 4, 0, 0, TimeSpan.Zero), false);
        CharacterProfileViewModel.RaidSaveTitle(save).ShouldBe("Icecrown Citadel · 25 players, heroic");
        CharacterProfileViewModel.RaidSaveDetail(save).ShouldBe("Resets Wed 7 Oct, 04:00 UTC · ID 43127");
        CharacterProfileViewModel.RaidSaveDetail(save with { LockoutId = null, IsExtended = true }).ShouldBe("Resets Wed 7 Oct, 04:00 UTC · extended");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds Arthasdk's profile, with two loadouts and two professions.</summary>
    /// <returns>The profile.</returns>
    private static CharacterProfile Profile() =>
        new(
            Guid.NewGuid(),
            "Icecrown",
            "Arthasdk",
            "DeathKnight",
            "Human",
            "Alliance",
            80,
            "Citadel Vanguard",
            "Community",
            new DateTimeOffset(2026, 10, 5, 14, 5, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 4, 9, 12, 0, TimeSpan.Zero),
            null,
            [new CharacterProfession("Blacksmithing", 450, 450), new CharacterProfession("Mining", 450, 450)],
            [
                new CharacterLoadout("Frost DPS", "MeleeDamage", true, 5712, "0/53/18", "WowAddon", [new CharacterGearItem("MainHand", 50737, "|Hitem:50737|h[Havoc's Call]|h", 264)]),
                new CharacterLoadout("Blood Tank", "Tank", false, 5480, "51/10/10", "WowAddon", []),
            ],
            []);
    #endregion Private Helpers
}
