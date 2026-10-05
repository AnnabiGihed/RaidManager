using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.ViewModels.Tests.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Companions;

/// <summary>Verifies the state and wording of the paired companions list.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Covers loading, the time labels, and each revocation outcome (companion pairing boards 2 to 4 and 14 to 16).
/// </remarks>
public sealed class PairedCompanionsViewModelTests
{
    #region Fields
    /// <summary>Stores the fixed current time.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCompanionsApiClient _api = new();

    /// <summary>Stores the view model under test.</summary>
    private readonly PairedCompanionsViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairedCompanionsViewModelTests"/> class.</summary>
    public PairedCompanionsViewModelTests()
    {
        _viewModel = new PairedCompanionsViewModel(_api, new FixedTimeProvider(Now));
    }
    #endregion Constructors

    #region Tests
    /// <summary>Loads the companions.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CompanionsAreListed()
    {
        _api.Companions = [FakeCompanionsApiClient.Companion("BRYN-DESKTOP"), FakeCompanionsApiClient.Companion("BRYN-LAPTOP", PairedCompanion.ExpiredStatus)];

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.Status.ShouldBe(CompanionPageStatus.Ready);
        _viewModel.Companions.Select(companion => (companion.Label, companion.IsActive)).ShouldBe([("BRYN-DESKTOP", true), ("BRYN-LAPTOP", false)]);
    }

    /// <summary>Fails to load, and without a signed-in player.</summary>
    /// <param name="signedIn">Whether the session holds a player.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedLoadIsRecorded(bool signedIn)
    {
        _api.Fails = true;

        await _viewModel.LoadAsync(signedIn ? UserId : Guid.Empty, CancellationToken.None);

        _viewModel.Status.ShouldBe(CompanionPageStatus.Failed);
    }

    /// <summary>Labels when companions were paired, revoked and last uploaded.</summary>
    [Fact]
    public void TimesAreLabelledInUtc()
    {
        var paired = FakeCompanionsApiClient.Companion("BRYN-LAPTOP") with { PairedAtUtc = new DateTimeOffset(2026, 9, 12, 21, 40, 0, TimeSpan.Zero) };

        _viewModel.PairedLabel(paired).ShouldBe("12 Sep, 21:40 UTC");
        _viewModel.RevokedLabel(paired with { RevokedAtUtc = Now.AddMinutes(-40) }).ShouldBe("Revoked today, 17:20 UTC");
        _viewModel.RevokedLabel(paired with { RevokedAtUtc = Now.AddDays(-1) }).ShouldBe("Revoked yesterday, 18:00 UTC");
        _viewModel.RevokedLabel(paired with { RevokedAtUtc = new DateTimeOffset(2026, 9, 28, 9, 5, 0, TimeSpan.Zero) }).ShouldBe("Revoked 28 Sep, 09:05 UTC");
        _viewModel.RevokedLabel(paired).ShouldBe("Revoked");
        _viewModel.LastUploadLabel(paired).ShouldBe(PairedCompanionsViewModel.NoUploadLabel);
        _viewModel.LastUploadLabel(paired with { LastUploadAtUtc = Now.AddMinutes(-40) }).ShouldBe("Today, 17:20 UTC");
    }

    /// <summary>Revokes a companion.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RevokingReloadsTheList()
    {
        var laptop = FakeCompanionsApiClient.Companion("BRYN-LAPTOP");
        _api.Companions = [laptop];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        var notice = await _viewModel.RevokeAsync(laptop, CancellationToken.None);

        notice.ShouldBe(new CompanionNotice(CompanionNoticeKind.Success, "BRYN-LAPTOP was revoked", "It can't upload until it's paired again."));
        _viewModel.Companions.ShouldHaveSingleItem().Status.ShouldBe(PairedCompanion.RevokedStatus);
        _viewModel.Revoking.ShouldBeNull();
        _api.Calls.ShouldBe(["list", $"revoke {laptop.CompanionId}", "list"]);
    }

    /// <summary>Revokes a companion the API refuses.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RefusedRevocationSaysItWasAlreadyRevoked()
    {
        var laptop = FakeCompanionsApiClient.Companion("BRYN-LAPTOP");
        _api.Companions = [laptop];
        _api.Revocation = RevokeOutcome.Refused;

        var notice = await _viewModel.RevokeAsync(laptop, CancellationToken.None);

        notice.Kind.ShouldBe(CompanionNoticeKind.Info);
        notice.Title.ShouldBe("BRYN-LAPTOP was already revoked");
    }

    /// <summary>Fails to send a revocation.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FailedRevocationSaysNothingChanged()
    {
        var laptop = FakeCompanionsApiClient.Companion("BRYN-LAPTOP");
        _api.ChangesFail = true;

        var notice = await _viewModel.RevokeAsync(laptop, CancellationToken.None);

        notice.ShouldBe(new CompanionNotice(CompanionNoticeKind.Error, "BRYN-LAPTOP wasn't revoked", PairCompanionViewModel.TryAgainDetail));
    }

    /// <summary>Words the arrival notification and the revocation question.</summary>
    [Fact]
    public void WordingFollowsTheMockup()
    {
        PairedCompanionsViewModel.PairedNotice("BRYN-DESKTOP").ShouldBe(new CompanionNotice(CompanionNoticeKind.Success, "BRYN-DESKTOP is paired", "It can upload your character data now."));
        PairedCompanionsViewModel.RevokeTitle(FakeCompanionsApiClient.Companion("BRYN-LAPTOP")).ShouldBe("Revoke BRYN-LAPTOP?");
        PairedCompanionsViewModel.RevokeMessage().ShouldBe("It stops uploading at once. Characters and snapshots it already uploaded stay in RaidManager.");
    }
    #endregion Tests
}
