using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Verifies the state and wording of the My characters page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Covers board 1 of the character profile mockup (story #19): loading, failures, and the loadout, raid save
/// and last sync labels with their freshness.
/// </remarks>
public sealed class MyCharactersViewModelTests
{
    #region Fields
    /// <summary>Stores the instant the tests treat as now: Monday 5 October 2026, 18:00 UTC.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCharacterProfilesApi _api = new();

    /// <summary>Stores the view model under test.</summary>
    private readonly MyCharactersViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MyCharactersViewModelTests"/> class.</summary>
    public MyCharactersViewModelTests()
    {
        _viewModel = new MyCharactersViewModel(_api, new FixedTimeProvider(Now));
    }
    #endregion Constructors

    #region Tests
    /// <summary>Loads the player's characters.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LoadShowsTheCharacters()
    {
        _api.Characters = [Character(Now.AddHours(-4))];

        await _viewModel.LoadAsync(Guid.NewGuid(), CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterPageStatus.Ready);
        _viewModel.Characters.ShouldHaveSingleItem().Name.ShouldBe("Arthasdk");
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

        await _viewModel.LoadAsync(failure == "no-user" ? null : Guid.NewGuid(), CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterPageStatus.Failed);
    }

    /// <summary>Words the loadout and raid save columns as board 1 does.</summary>
    [Fact]
    public void ColumnsUseTheMockupWording()
    {
        var character = Character(Now);
        var unsynced = character with { PrimaryLoadout = null, CurrentRaidSaveCount = 0 };

        MyCharactersViewModel.ClassAndRealm(character).ShouldBe("Death Knight · Icecrown");
        MyCharactersViewModel.LoadoutLabel(character).ShouldBe("Frost DPS · GearScore 5,712");
        MyCharactersViewModel.RaidSavesLabel(character).ShouldBe("2 this week");
        MyCharactersViewModel.LoadoutLabel(unsynced).ShouldBe("No loadout synced yet");
        MyCharactersViewModel.RaidSavesLabel(unsynced).ShouldBe("None");
    }

    /// <summary>Words the last sync and its freshness: fresh within three days, stale after, never without one.</summary>
    /// <param name="hoursAgo">Hours since the last sync, or a negative number for never.</param>
    /// <param name="label">The expected label.</param>
    /// <param name="freshness">The expected freshness.</param>
    [Theory]
    [InlineData(4, "Today, 14:00 UTC", SyncFreshness.Fresh)]
    [InlineData(24, "Yesterday, 18:00 UTC", SyncFreshness.Fresh)]
    [InlineData(72, "3 days ago", SyncFreshness.Fresh)]
    [InlineData(120, "5 days ago", SyncFreshness.Stale)]
    [InlineData(-1, MyCharactersViewModel.NeverSynced, SyncFreshness.Never)]
    public void LastSyncIsWordedByAge(int hoursAgo, string label, SyncFreshness freshness)
    {
        var character = Character(hoursAgo < 0 ? null : Now.AddHours(-hoursAgo));

        _viewModel.SyncLabel(character).ShouldBe(label);
        _viewModel.Freshness(character).ShouldBe(freshness);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds Arthasdk, a death knight with a primary loadout and two raid saves.</summary>
    /// <param name="lastSync">The last sync, if any.</param>
    /// <returns>The character.</returns>
    private static CharacterSummary Character(DateTimeOffset? lastSync) =>
        new(Guid.NewGuid(), "Icecrown", "Arthasdk", "DeathKnight", 80, new CharacterLoadoutSummary("Frost DPS", "MeleeDamage", 5712), 2, lastSync);
    #endregion Private Helpers
}
