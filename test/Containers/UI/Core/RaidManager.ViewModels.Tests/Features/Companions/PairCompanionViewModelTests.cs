using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.ViewModels.Tests.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Companions;

/// <summary>Verifies the state and wording of the confirm page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Covers each answer the confirm page shows (companion pairing boards 1, 9 to 13 and 17) and the expiry label.
/// </remarks>
public sealed class PairCompanionViewModelTests
{
    #region Fields
    /// <summary>Stores the fixed current time.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCompanionsApiClient _api = new() { Lookup = new(PairingCodeStatus.Waiting, FakeCompanionsApiClient.Pending(Now)) };

    /// <summary>Stores the view model under test.</summary>
    private readonly PairCompanionViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairCompanionViewModelTests"/> class.</summary>
    public PairCompanionViewModelTests()
    {
        _viewModel = new PairCompanionViewModel(_api, new FixedTimeProvider(Now));
    }
    #endregion Constructors

    #region Tests
    /// <summary>Loads a waiting code.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task WaitingCodeCanBeConfirmed()
    {
        await _viewModel.LoadAsync(UserId, " K7M-4QX ", CancellationToken.None);

        _viewModel.Status.ShouldBe(CompanionPageStatus.Ready);
        _viewModel.CanConfirm.ShouldBeTrue();
        _viewModel.Problem.ShouldBeNull();
        _viewModel.Pairing!.ComputerLabel.ShouldBe("BRYN-DESKTOP");
        _viewModel.ExpiryLabel().ShouldBe("Expires in 9 minutes");
        _api.Calls.ShouldBe(["lookup K7M-4QX"]);
    }

    /// <summary>Labels the expiry in whole minutes rounded up.</summary>
    /// <param name="secondsLeft">The seconds left.</param>
    /// <param name="expected">The expected label.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(61, "Expires in 2 minutes")]
    [InlineData(60, "Expires in 1 minute")]
    [InlineData(1, "Expires in 1 minute")]
    [InlineData(0, "Expired")]
    public async Task ExpiryIsLabelledInWholeMinutes(int secondsLeft, string expected)
    {
        _api.Lookup = new(PairingCodeStatus.Waiting, FakeCompanionsApiClient.Pending(Now) with { ExpiresAtUtc = Now.AddSeconds(secondsLeft) });

        await _viewModel.LoadAsync(UserId, "K7M-4QX", CancellationToken.None);

        _viewModel.ExpiryLabel().ShouldBe(expected);
    }

    /// <summary>Shows why a code can't be confirmed.</summary>
    /// <param name="status">What the API says.</param>
    /// <param name="kind">The expected notice kind.</param>
    /// <param name="title">The expected title.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(PairingCodeStatus.Expired, CompanionNoticeKind.Warning, "This code expired")]
    [InlineData(PairingCodeStatus.AlreadyConfirmed, CompanionNoticeKind.Info, "This code was already confirmed")]
    [InlineData(PairingCodeStatus.Unknown, CompanionNoticeKind.Warning, "No companion is waiting for this code")]
    public async Task RefusedCodeShowsWhy(PairingCodeStatus status, CompanionNoticeKind kind, string title)
    {
        _api.Lookup = new(status, null);

        await _viewModel.LoadAsync(UserId, "K7M-4QX", CancellationToken.None);

        _viewModel.CanConfirm.ShouldBeFalse();
        _viewModel.Problem.ShouldNotBeNull().Kind.ShouldBe(kind);
        _viewModel.Problem!.Title.ShouldBe(title);
        _viewModel.ExpiryLabel().ShouldBeEmpty();
        (await _viewModel.ConfirmAsync(CancellationToken.None)).ShouldBeNull();
        _api.Calls.ShouldBe(["lookup K7M-4QX"]);
    }

    /// <summary>Opens the page without a code.</summary>
    /// <param name="code">The missing code.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task MissingCodeAsksToStartInTheCompanion(string? code)
    {
        await _viewModel.LoadAsync(UserId, code, CancellationToken.None);

        _viewModel.HasNoCode.ShouldBeTrue();
        _viewModel.Problem.ShouldBe(new CompanionNotice(CompanionNoticeKind.Info, "Start pairing in the companion", "It shows a code and opens this page with it."));
        _api.Calls.ShouldBeEmpty();
    }

    /// <summary>Fails to check a code, and without a signed-in player.</summary>
    /// <param name="signedIn">Whether the session holds a player.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UncheckableCodeOffersToTryAgain(bool signedIn)
    {
        _api.Fails = true;

        await _viewModel.LoadAsync(signedIn ? UserId : null, "K7M-4QX", CancellationToken.None);

        _viewModel.Status.ShouldBe(CompanionPageStatus.Failed);
        _viewModel.Problem.ShouldBe(new CompanionNotice(CompanionNoticeKind.Error, "We couldn't check this code", "Nothing was paired. Try again in a moment."));
    }

    /// <summary>Confirms a waiting code.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ConfirmingPairsTheCompanion()
    {
        await _viewModel.LoadAsync(UserId, "K7M-4QX", CancellationToken.None);

        var notice = await _viewModel.ConfirmAsync(CancellationToken.None);

        notice.ShouldBeNull();
        _viewModel.CodeStatus.ShouldBe(PairingCodeStatus.Paired);
        _viewModel.IsConfirming.ShouldBeFalse();
        _api.Calls.ShouldBe(["lookup K7M-4QX", "confirm K7M-4QX"]);
    }

    /// <summary>Confirms a code that expired meanwhile.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CodeThatExpiredMeanwhileShowsTheExpiry()
    {
        _api.Confirmation = PairingCodeStatus.Expired;
        await _viewModel.LoadAsync(UserId, "K7M-4QX", CancellationToken.None);

        (await _viewModel.ConfirmAsync(CancellationToken.None)).ShouldBeNull();

        _viewModel.Pairing.ShouldBeNull();
        _viewModel.Problem!.Title.ShouldBe("This code expired");
    }

    /// <summary>Fails to send a confirmation.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FailedConfirmationKeepsTheCodeAndSaysNothingChanged()
    {
        await _viewModel.LoadAsync(UserId, "K7M-4QX", CancellationToken.None);
        _api.ChangesFail = true;

        var notice = await _viewModel.ConfirmAsync(CancellationToken.None);

        notice.ShouldBe(new CompanionNotice(CompanionNoticeKind.Error, "BRYN-DESKTOP wasn't paired", PairCompanionViewModel.TryAgainDetail));
        _viewModel.CanConfirm.ShouldBeTrue();
    }
    #endregion Tests
}
