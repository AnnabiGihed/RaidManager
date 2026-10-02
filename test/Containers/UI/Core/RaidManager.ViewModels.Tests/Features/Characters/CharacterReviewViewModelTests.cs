using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Verifies the state, decisions and wording of the character review page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Covers story #18's review: loading, approve and reject, a refused decision, failures, and the labels.
/// </remarks>
public sealed class CharacterReviewViewModelTests
{
    #region Fields
    /// <summary>Stores the instant the tests treat as now: Thursday 1 October 2026, 18:00 UTC.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the signed-in player.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores a pending claim.</summary>
    private static readonly CharacterClaim Arthasdk = Claim("Arthasdk", "Icecrown", CharacterClaim.PendingState);

    /// <summary>Stores another pending claim.</summary>
    private static readonly CharacterClaim Jainaice = Claim("Jainaice", "Lordaeron", CharacterClaim.PendingState);

    /// <summary>Stores a conflicted claim.</summary>
    private static readonly CharacterClaim Sylvanash = Claim("Sylvanash", "Icecrown", CharacterClaim.ConflictState);

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCharacterClaimsApi _api = new();

    /// <summary>Stores the view model under test.</summary>
    private readonly CharacterReviewViewModel _viewModel;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterReviewViewModelTests"/> class.</summary>
    public CharacterReviewViewModelTests()
    {
        _viewModel = new CharacterReviewViewModel(_api, new FixedTimeProvider(Now));
    }
    #endregion Constructors

    #region Tests
    /// <summary>Loads pending and conflicted claims.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LoadShowsPendingAndConflictedClaims()
    {
        _api.Claims = [Arthasdk, Jainaice, Sylvanash];

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterReviewStatus.Ready);
        _viewModel.Claims.Count.ShouldBe(3);
        _viewModel.PendingCount.ShouldBe(2);
        _viewModel.Conflicts.ShouldBe([Sylvanash]);
        _viewModel.WaitingTitle.ShouldBe("2 characters are waiting for your decision");
    }

    /// <summary>Uses the singular for one waiting character.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OneWaitingCharacterUsesTheSingular()
    {
        _api.Claims = [Arthasdk, Sylvanash];

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.WaitingTitle.ShouldBe("1 character is waiting for your decision");
    }

    /// <summary>Fails the load as an unavailable or timed-out API would.</summary>
    /// <param name="timeout">Whether the API times out instead of failing.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnavailableApiFailsTheLoad(bool timeout)
    {
        _api.LoadFailure = timeout ? new TaskCanceledException("Timed out.") : new HttpRequestException("Unavailable.");

        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterReviewStatus.Failed);
    }

    /// <summary>Loads without a user id in the session.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MissingUserFailsTheLoadWithoutCallingTheApi()
    {
        await _viewModel.LoadAsync(null, CancellationToken.None);

        _viewModel.Status.ShouldBe(CharacterReviewStatus.Failed);
        _api.Loads.ShouldBe(0);
    }

    /// <summary>Closes the page while the claims load.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LeavingThePageCancelsTheLoad()
    {
        using var closed = new CancellationTokenSource();
        await closed.CancelAsync();
        _api.LoadFailure = new TaskCanceledException("Cancelled.");

        await Should.ThrowAsync<TaskCanceledException>(() => _viewModel.LoadAsync(UserId, closed.Token));
    }

    /// <summary>Approves a pending claim.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RecordedApprovalRemovesTheClaim()
    {
        _api.Claims = [Arthasdk, Jainaice];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        var notice = await _viewModel.ApproveAsync(Arthasdk, CancellationToken.None);

        notice.ShouldBe(new ReviewNotice(ReviewNoticeKind.Success, "Arthasdk approved", "It's now one of your characters and can sign up for raids."));
        _viewModel.Claims.ShouldBe([Jainaice]);
        _viewModel.Deciding.ShouldBeNull();
        _api.Decisions.ShouldBe([("approve", UserId, Arthasdk.CharacterId)]);
    }

    /// <summary>Rejects a pending claim.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RecordedRejectionRemovesTheClaim()
    {
        _api.Claims = [Arthasdk, Jainaice];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);

        var notice = await _viewModel.RejectAsync(Jainaice, CancellationToken.None);

        notice.ShouldBe(new ReviewNotice(ReviewNoticeKind.Success, "Jainaice rejected", "It won't become one of your characters."));
        _viewModel.Claims.ShouldBe([Arthasdk]);
        _api.Decisions.ShouldBe([("reject", UserId, Jainaice.CharacterId)]);
    }

    /// <summary>Approves a character another player claimed meanwhile.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ApprovalRefusedForAnotherOwnerTurnsTheClaimIntoAConflict()
    {
        _api.Claims = [Arthasdk];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);
        _api.Outcome = ClaimDecisionOutcome.Refused;
        _api.ClaimsAfterRefusal = [Arthasdk with { ClaimState = CharacterClaim.ConflictState }];

        var notice = await _viewModel.ApproveAsync(Arthasdk, CancellationToken.None);

        notice.Kind.ShouldBe(ReviewNoticeKind.Warning);
        notice.Title.ShouldBe("Arthasdk goes to an officer");
        _viewModel.PendingCount.ShouldBe(0);
        _viewModel.Conflicts.Single().CharacterId.ShouldBe(Arthasdk.CharacterId);
    }

    /// <summary>Rejects a claim that was decided elsewhere meanwhile.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task DecisionRefusedForAnAlreadyDecidedClaimReloadsTheList()
    {
        _api.Claims = [Arthasdk, Jainaice];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);
        _api.Outcome = ClaimDecisionOutcome.Refused;
        _api.ClaimsAfterRefusal = [Arthasdk];

        var notice = await _viewModel.RejectAsync(Jainaice, CancellationToken.None);

        notice.ShouldBe(new ReviewNotice(ReviewNoticeKind.Info, "Jainaice was already decided", "The list now shows its current state."));
        _viewModel.Claims.ShouldBe([Arthasdk]);
        _api.Loads.ShouldBe(2);
    }

    /// <summary>Approves while the API is unavailable.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FailedDecisionKeepsTheClaim()
    {
        _api.Claims = [Arthasdk];
        await _viewModel.LoadAsync(UserId, CancellationToken.None);
        _api.DecisionFailure = new HttpRequestException("Unavailable.");

        var approval = await _viewModel.ApproveAsync(Arthasdk, CancellationToken.None);
        var rejection = await _viewModel.RejectAsync(Arthasdk, CancellationToken.None);

        approval.ShouldBe(new ReviewNotice(ReviewNoticeKind.Error, "Arthasdk wasn't approved", "Nothing changed. Try again in a moment."));
        rejection.Title.ShouldBe("Arthasdk wasn't rejected");
        _viewModel.Claims.ShouldBe([Arthasdk]);
        _viewModel.Deciding.ShouldBeNull();
    }

    /// <summary>Labels when each character was found, relative to today in UTC.</summary>
    /// <param name="found">When the character was found, in UTC.</param>
    /// <param name="expected">The expected label.</param>
    [Theory]
    [InlineData("2026-10-01T14:05:00Z", "Today, 14:05 UTC")]
    [InlineData("2026-09-30T21:40:00Z", "Yesterday, 21:40 UTC")]
    [InlineData("2026-09-28T09:15:00Z", "28 Sep, 09:15 UTC")]
    [InlineData("2025-12-31T23:59:00Z", "31 Dec 2025, 23:59 UTC")]
    public void FoundLabelIsRelativeToToday(string found, string expected)
    {
        var claim = Arthasdk with { RequestedAtUtc = DateTimeOffset.Parse(found, System.Globalization.CultureInfo.InvariantCulture) };

        _viewModel.FoundLabel(claim).ShouldBe(expected);
    }

    /// <summary>Words the reject confirmation and the conflict reminder.</summary>
    [Fact]
    public void ConfirmationAndReminderNameTheCharacter()
    {
        CharacterReviewViewModel.RejectTitle(Jainaice).ShouldBe("Reject Jainaice?");
        CharacterReviewViewModel.RejectMessage(Jainaice).ShouldBe(
            "Jainaice (Lordaeron) won't become one of your characters, so you can't sign it up for raids.");
        CharacterReviewViewModel.RejectAdvice.ShouldBe("Reject it only if it isn't yours.");
        CharacterReviewViewModel.ConflictReminder(Sylvanash).ShouldBe("Sylvanash stays in conflict review until an officer decides.");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a claim found the day before the tests' now.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="realm">The realm.</param>
    /// <param name="state">The claim state.</param>
    /// <returns>The claim.</returns>
    private static CharacterClaim Claim(string name, string realm, string state) =>
        new(Guid.NewGuid(), realm, name, "Mage", "Human", 80, state, Now.AddDays(-1));
    #endregion Private Helpers
}
