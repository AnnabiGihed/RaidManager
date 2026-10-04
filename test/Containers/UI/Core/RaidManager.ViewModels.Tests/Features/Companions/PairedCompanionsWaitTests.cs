using Microsoft.Extensions.Time.Testing;
using RaidManager.ViewModels.Features.Companions;
using Shouldly;
using Xunit;

namespace RaidManager.ViewModels.Tests.Features.Companions;

/// <summary>Verifies how the list waits for a computer the player just confirmed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The fix of #526: the companion creates its row when it collects its token, a few seconds after the
/// confirmation, so the list keeps reloading until the computer is listed, for up to 30 seconds, and a failed reload
/// keeps the list shown.
/// </remarks>
public sealed class PairedCompanionsWaitTests
{
    #region Constants
    /// <summary>Defines the label of the computer just confirmed.</summary>
    private const string Desktop = "BRYN-DESKTOP";
    #endregion Constants

    #region Fields
    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCompanionsApiClient _api = new();

    /// <summary>Stores the clock.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero));

    /// <summary>Stores the view model under test.</summary>
    private readonly PairedCompanionsViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairedCompanionsWaitTests"/> class.</summary>
    public PairedCompanionsWaitTests()
    {
        _viewModel = new PairedCompanionsViewModel(_api, _time);
    }
    #endregion Constructors

    #region Tests
    /// <summary>The list waits while the confirmed computer is missing, and stops once a reload lists it.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ListWaitsUntilTheConfirmedComputerAppears()
    {
        await _viewModel.LoadAsync(UserId, CancellationToken.None);
        _viewModel.IsWaitingFor(Desktop).ShouldBeTrue();

        _api.Companions = [JustPaired(Desktop.ToLowerInvariant())];
        await _viewModel.ReloadAsync(CancellationToken.None);

        _viewModel.IsWaitingFor(Desktop).ShouldBeFalse();
        _viewModel.Status.ShouldBe(CompanionPageStatus.Ready);
        _api.Calls.ShouldBe(["list", "list"]);
    }

    /// <summary>The wait gives up after 30 seconds.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task WaitEndsAfterThirtySeconds()
    {
        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _time.Advance(TimeSpan.FromSeconds(29));
        _viewModel.IsWaitingFor(Desktop).ShouldBeTrue();
        _time.Advance(TimeSpan.FromSeconds(1));
        _viewModel.IsWaitingFor(Desktop).ShouldBeFalse();
    }

    /// <summary>An older row of the same computer, or a revoked one, doesn't end the wait.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OlderOrRevokedRowsOfTheComputerDontCount()
    {
        _api.Companions =
        [
            FakeCompanionsApiClient.Companion(Desktop),
            JustPaired(Desktop) with { Status = PairedCompanion.RevokedStatus },
        ];

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.IsWaitingFor(Desktop).ShouldBeTrue();
    }

    /// <summary>Without a confirmed computer, or after a failed load, nothing is awaited.</summary>
    /// <param name="label">The confirmed computer, if any.</param>
    /// <param name="loadFails">Whether the first load fails.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(null, false)]
    [InlineData(" ", false)]
    [InlineData(Desktop, true)]
    public async Task NothingIsAwaitedWithoutAConfirmedComputerOrAList(string? label, bool loadFails)
    {
        _viewModel.IsWaitingFor(Desktop).ShouldBeFalse();
        _api.Fails = loadFails;

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.IsWaitingFor(label).ShouldBeFalse();
    }

    /// <summary>A failed reload keeps the list shown, and a reload before any list does nothing.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FailedReloadKeepsTheList()
    {
        await _viewModel.ReloadAsync(CancellationToken.None);
        _api.Calls.ShouldBeEmpty();
        _api.Companions = [FakeCompanionsApiClient.Companion("BRYN-LAPTOP")];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);
        _api.Fails = true;

        await _viewModel.ReloadAsync(CancellationToken.None);

        _viewModel.Companions.ShouldHaveSingleItem().Label.ShouldBe("BRYN-LAPTOP");
        _viewModel.Status.ShouldBe(CompanionPageStatus.Ready);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds the row a companion creates when it collects its token now.</summary>
    /// <param name="label">The computer label.</param>
    /// <returns>The companion.</returns>
    private PairedCompanion JustPaired(string label) =>
        new(Guid.NewGuid(), label, _time.GetUtcNow(), PairedCompanion.ActiveStatus, null);
    #endregion Private Helpers
}
