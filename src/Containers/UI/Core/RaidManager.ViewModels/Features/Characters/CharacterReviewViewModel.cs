using System.Globalization;

namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Loads a player's character claims and records their approve and reject decisions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Holds the state and wording of the character review page (story #18). Undecided claims stay pending; a
/// claim another player owns waits for an officer and offers no decision.
/// </remarks>
public sealed class CharacterReviewViewModel
{
    #region Constants
    /// <summary>Defines the advice under the rejection message.</summary>
    public const string RejectAdvice = "Reject it only if it isn't yours.";

    /// <summary>Defines the detail shown when a decision could not be sent.</summary>
    private const string TryAgainDetail = "Nothing changed. Try again in a moment.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the claims API client.</summary>
    private readonly ICharacterClaimsApiClient _api;

    /// <summary>Stores the clock that labels when each character was found.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>Stores the player whose claims are shown.</summary>
    private Guid _userId;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterReviewViewModel"/> class.</summary>
    /// <param name="api">The claims API client.</param>
    /// <param name="timeProvider">The clock that labels when each character was found.</param>
    public CharacterReviewViewModel(ICharacterClaimsApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the claims are loading, shown, or could not be loaded.</summary>
    public CharacterReviewStatus Status { get; private set; } = CharacterReviewStatus.Loading;

    /// <summary>Gets the claims awaiting a decision or an officer, oldest first.</summary>
    public IReadOnlyList<CharacterClaim> Claims { get; private set; } = [];

    /// <summary>Gets the character whose decision is being sent, if any; its buttons are disabled meanwhile.</summary>
    public Guid? Deciding { get; private set; }

    /// <summary>Gets the number of claims the player can still decide.</summary>
    public int PendingCount => Claims.Count(claim => claim.IsPending);

    /// <summary>Gets the claims waiting for an officer.</summary>
    public IEnumerable<CharacterClaim> Conflicts => Claims.Where(claim => !claim.IsPending);

    /// <summary>Gets the title of the notice above the list.</summary>
    public string WaitingTitle => PendingCount == 1
        ? "1 character is waiting for your decision"
        : $"{PendingCount} characters are waiting for your decision";
    #endregion Properties

    #region Public Methods
    /// <summary>Gets the title of the confirmation asked before a rejection.</summary>
    /// <param name="claim">The claim to reject.</param>
    /// <returns>The title.</returns>
    public static string RejectTitle(CharacterClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return $"Reject {claim.Name}?";
    }

    /// <summary>Gets the message of the confirmation asked before a rejection.</summary>
    /// <param name="claim">The claim to reject.</param>
    /// <returns>The message.</returns>
    public static string RejectMessage(CharacterClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return $"{claim.Name} ({claim.Realm}) won't become one of your characters, so you can't sign it up for raids.";
    }

    /// <summary>Gets the reminder shown for a conflicted claim once nothing is left to decide.</summary>
    /// <param name="claim">The conflicted claim.</param>
    /// <returns>The reminder.</returns>
    public static string ConflictReminder(CharacterClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return $"{claim.Name} stays in conflict review until an officer decides.";
    }

    /// <summary>Loads the player's claims.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the claims are shown or the failure is recorded.</returns>
    public async Task LoadAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            Status = CharacterReviewStatus.Failed;
            return;
        }

        _userId = id;
        Status = CharacterReviewStatus.Loading;
        try
        {
            Claims = await _api.GetPendingAsync(id, cancellationToken);
            Status = CharacterReviewStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Status = CharacterReviewStatus.Failed;
        }
    }

    /// <summary>Approves a claim; a recorded approval removes it from the list.</summary>
    /// <param name="claim">The claim to approve.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>The notification to show.</returns>
    public async Task<ReviewNotice> ApproveAsync(CharacterClaim claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return await DecideAsync(
            claim,
            _api.ApproveAsync,
            new ReviewNotice(ReviewNoticeKind.Success, $"{claim.Name} approved", "It's now one of your characters and can sign up for raids."),
            $"{claim.Name} wasn't approved",
            cancellationToken);
    }

    /// <summary>Rejects a claim; a recorded rejection removes it from the list.</summary>
    /// <param name="claim">The claim to reject.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>The notification to show.</returns>
    public async Task<ReviewNotice> RejectAsync(CharacterClaim claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return await DecideAsync(
            claim,
            _api.RejectAsync,
            new ReviewNotice(ReviewNoticeKind.Success, $"{claim.Name} rejected", "It won't become one of your characters."),
            $"{claim.Name} wasn't rejected",
            cancellationToken);
    }

    /// <summary>Labels when the companion found a character, in UTC: today, yesterday, or the date.</summary>
    /// <param name="claim">The claim.</param>
    /// <returns>For example <c>Today, 14:05 UTC</c> or <c>28 Sep, 21:40 UTC</c>.</returns>
    public string FoundLabel(CharacterClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var found = claim.RequestedAtUtc.ToUniversalTime();
        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var time = found.ToString("HH:mm", CultureInfo.InvariantCulture);
        var day = found.UtcDateTime.Date;
        if (day == today)
        {
            return $"Today, {time} UTC";
        }

        if (day == today.AddDays(-1))
        {
            return $"Yesterday, {time} UTC";
        }

        var format = found.Year == today.Year ? "d MMM" : "d MMM yyyy";
        return $"{found.ToString(format, CultureInfo.InvariantCulture)}, {time} UTC";
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Determines whether an exception means the API failed, as opposed to the page being closed.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns><see langword="true"/> for an HTTP failure or a timeout.</returns>
    private static bool IsApiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>Sends one decision and updates the list from the API's answer.</summary>
    /// <param name="claim">The claim to decide.</param>
    /// <param name="send">The API call that records the decision.</param>
    /// <param name="recorded">The notification for a recorded decision.</param>
    /// <param name="failedTitle">The notification title for a decision that could not be sent.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>The notification to show.</returns>
    private async Task<ReviewNotice> DecideAsync(
        CharacterClaim claim,
        Func<Guid, Guid, CancellationToken, Task<ClaimDecisionOutcome>> send,
        ReviewNotice recorded,
        string failedTitle,
        CancellationToken cancellationToken)
    {
        Deciding = claim.CharacterId;
        try
        {
            var outcome = await send(_userId, claim.CharacterId, cancellationToken);
            if (outcome == ClaimDecisionOutcome.Recorded)
            {
                Claims = [.. Claims.Where(other => other.CharacterId != claim.CharacterId)];
                return recorded;
            }

            await LoadAsync(_userId, cancellationToken);
            return Claims.Any(other => other.CharacterId == claim.CharacterId && !other.IsPending)
                ? new ReviewNotice(ReviewNoticeKind.Warning, $"{claim.Name} goes to an officer", "Another player already owns it, so an officer will review your claim.")
                : new ReviewNotice(ReviewNoticeKind.Info, $"{claim.Name} was already decided", "The list now shows its current state.");
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            return new ReviewNotice(ReviewNoticeKind.Error, failedTitle, TryAgainDetail);
        }
        finally
        {
            Deciding = null;
        }
    }
    #endregion Private Helpers
}
