using RaidManager.ViewModels.Features.Shared;

namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Lists a player's companions and revokes one.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Holds the state and wording of the paired companions list (companion pairing boards 2 to 4 and 14 to 16, owner decisions on #513). Before a companion's first upload, its last upload says No upload yet (#384). After a confirmation the list waits for the companion to collect its token, which creates its row (owner decision on #526).
/// </remarks>
public sealed class PairedCompanionsViewModel
{
    #region Constants
    /// <summary>Defines what the last upload column shows before a companion's first upload (owner decision on #513).</summary>
    public const string NoUploadLabel = "No upload yet";

    /// <summary>Defines the note shown instead of Revoke for a companion unused for 180 days.</summary>
    public const string ExpiredNote = "Unused for 180 days";

    /// <summary>Defines the sentence under the revocation question.</summary>
    public const string RevokeAdvice = "Pair it again to resume.";
    #endregion Constants

    #region Fields
    /// <summary>Gets how long the page waits between two reloads while the confirmed computer is missing.</summary>
    public static readonly TimeSpan ReloadInterval = TimeSpan.FromSeconds(2);

    /// <summary>Stores how long the list waits for the confirmed computer (owner decision on #526).</summary>
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(30);

    /// <summary>Stores how far back a pairing still counts as the one just confirmed, for clock differences.</summary>
    private static readonly TimeSpan PairingSlack = TimeSpan.FromMinutes(1);

    /// <summary>Stores the companions API client.</summary>
    private readonly ICompanionsApiClient _api;

    /// <summary>Stores the clock for the time labels.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>Stores the signed-in player.</summary>
    private Guid _userId;

    /// <summary>Stores when the list was first loaded, which starts the wait for a confirmed computer.</summary>
    private DateTimeOffset? _firstLoadedAt;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairedCompanionsViewModel"/> class.</summary>
    /// <param name="api">The companions API client.</param>
    /// <param name="timeProvider">The clock for the time labels.</param>
    public PairedCompanionsViewModel(ICompanionsApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the companions are loading, shown, or could not be loaded.</summary>
    public CompanionPageStatus Status { get; private set; } = CompanionPageStatus.Loading;

    /// <summary>Gets the player's companions, oldest first.</summary>
    public IReadOnlyList<PairedCompanion> Companions { get; private set; } = [];

    /// <summary>Gets the companion whose revocation is being sent, if any.</summary>
    public Guid? Revoking { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Gets the notification shown when the player arrives from a confirmed pairing.</summary>
    /// <param name="label">The computer's label.</param>
    /// <returns>The notification.</returns>
    public static CompanionNotice PairedNotice(string label) =>
        new(CompanionNoticeKind.Success, $"{label} is paired", "It can upload your character data now.");

    /// <summary>Gets the title of the confirmation asked before a revocation.</summary>
    /// <param name="companion">The companion to revoke.</param>
    /// <returns>The title.</returns>
    public static string RevokeTitle(PairedCompanion companion)
    {
        ArgumentNullException.ThrowIfNull(companion);
        return $"Revoke {companion.Label}?";
    }

    /// <summary>Gets the message of the confirmation asked before a revocation.</summary>
    /// <returns>The message.</returns>
    public static string RevokeMessage() =>
        "It stops uploading at once. Characters and snapshots it already uploaded stay in RaidManager.";

    /// <summary>Loads the player's companions.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the companions are shown or the failure is recorded.</returns>
    public async Task LoadAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            Status = CompanionPageStatus.Failed;
            return;
        }

        _userId = id;
        Status = CompanionPageStatus.Loading;
        try
        {
            Companions = await _api.GetCompanionsAsync(id, cancellationToken);
            Status = CompanionPageStatus.Ready;
            _firstLoadedAt ??= _timeProvider.GetUtcNow();
        }
        catch (Exception exception) when (CompanionApiFailures.IsApiFailure(exception, cancellationToken))
        {
            Status = CompanionPageStatus.Failed;
        }
    }

    /// <summary>
    /// Determines whether the list should reload to show a computer the player just confirmed: its companion creates
    /// its row when it collects its token, a few seconds after the confirmation (#526).
    /// </summary>
    /// <param name="label">The confirmed computer's label, or <see langword="null"/> when the player didn't come from a confirmation.</param>
    /// <returns><see langword="true"/> while the computer is missing and the 30-second wait hasn't run out.</returns>
    public bool IsWaitingFor(string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || Status != CompanionPageStatus.Ready || _firstLoadedAt is not { } loadedAt)
        {
            return false;
        }

        var listed = Companions.Any(companion => companion.IsActive
                                                 && string.Equals(companion.Label, label, StringComparison.OrdinalIgnoreCase)
                                                 && companion.PairedAtUtc >= loadedAt - PairingSlack);
        return !listed && _timeProvider.GetUtcNow() < loadedAt + WaitLimit;
    }

    /// <summary>Reloads the companions without showing the loading state; a failed reload keeps the list shown.</summary>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the list is reloaded or the failure is ignored.</returns>
    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        if (Status != CompanionPageStatus.Ready)
        {
            return;
        }

        try
        {
            Companions = await _api.GetCompanionsAsync(_userId, cancellationToken);
        }
        catch (Exception exception) when (CompanionApiFailures.IsApiFailure(exception, cancellationToken))
        {
            // The list shown stays right; the next reload, or the player's refresh, tries again.
        }
    }

    /// <summary>Labels when a companion was paired.</summary>
    /// <param name="companion">The companion.</param>
    /// <returns>For example <c>Today, 14:02 UTC</c>.</returns>
    public string PairedLabel(PairedCompanion companion)
    {
        ArgumentNullException.ThrowIfNull(companion);
        return UtcTimeLabel.Format(companion.PairedAtUtc, _timeProvider.GetUtcNow());
    }

    /// <summary>Labels when a companion last uploaded a character snapshot.</summary>
    /// <param name="companion">The companion.</param>
    /// <returns>For example <c>Today, 14:05 UTC</c>, or <see cref="NoUploadLabel"/> before its first upload.</returns>
    public string LastUploadLabel(PairedCompanion companion)
    {
        ArgumentNullException.ThrowIfNull(companion);
        return companion.LastUploadAtUtc is { } uploadedAtUtc ? UtcTimeLabel.Format(uploadedAtUtc, _timeProvider.GetUtcNow()) : NoUploadLabel;
    }

    /// <summary>Labels when a companion was revoked.</summary>
    /// <param name="companion">The revoked companion.</param>
    /// <returns>For example <c>Revoked today, 17:20 UTC</c>.</returns>
    public string RevokedLabel(PairedCompanion companion)
    {
        ArgumentNullException.ThrowIfNull(companion);
        if (companion.RevokedAtUtc is not { } revokedAt)
        {
            return "Revoked";
        }

        var when = UtcTimeLabel.Format(revokedAt, _timeProvider.GetUtcNow());
        return when.StartsWith("Today", StringComparison.Ordinal) || when.StartsWith("Yesterday", StringComparison.Ordinal)
            ? $"Revoked {char.ToLowerInvariant(when[0])}{when[1..]}"
            : $"Revoked {when}";
    }

    /// <summary>Revokes a companion; a recorded revocation reloads the list.</summary>
    /// <param name="companion">The companion to revoke.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>The notification to show.</returns>
    public async Task<CompanionNotice> RevokeAsync(PairedCompanion companion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(companion);
        Revoking = companion.CompanionId;
        try
        {
            var outcome = await _api.RevokeAsync(_userId, companion.CompanionId, cancellationToken);
            await LoadAsync(_userId, cancellationToken);
            return outcome == RevokeOutcome.Revoked
                ? new CompanionNotice(CompanionNoticeKind.Success, $"{companion.Label} was revoked", "It can't upload until it's paired again.")
                : new CompanionNotice(CompanionNoticeKind.Info, $"{companion.Label} was already revoked", "The list now shows its current state.");
        }
        catch (Exception exception) when (CompanionApiFailures.IsApiFailure(exception, cancellationToken))
        {
            return new CompanionNotice(CompanionNoticeKind.Error, $"{companion.Label} wasn't revoked", PairCompanionViewModel.TryAgainDetail);
        }
        finally
        {
            Revoking = null;
        }
    }
    #endregion Public Methods
}
