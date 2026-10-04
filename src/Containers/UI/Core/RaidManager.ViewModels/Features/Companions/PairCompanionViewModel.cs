namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Looks up a companion's code and confirms it for the signed-in player.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Holds the state and wording of the confirm page (companion pairing boards 1, 9 to 13 and 17, owner decisions on #513).
/// </remarks>
public sealed class PairCompanionViewModel
{
    #region Constants
    /// <summary>Defines the sentence shown when a call could not be sent.</summary>
    public const string TryAgainDetail = "Nothing changed. Try again in a moment.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the companions API client.</summary>
    private readonly ICompanionsApiClient _api;

    /// <summary>Stores the clock for the expiry label.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>Stores the signed-in player.</summary>
    private Guid _userId;

    /// <summary>Stores the code from the companion's link.</summary>
    private string _code = string.Empty;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairCompanionViewModel"/> class.</summary>
    /// <param name="api">The companions API client.</param>
    /// <param name="timeProvider">The clock for the expiry label.</param>
    public PairCompanionViewModel(ICompanionsApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets whether the code is loading, answered, or could not be checked.</summary>
    public CompanionPageStatus Status { get; private set; } = CompanionPageStatus.Loading;

    /// <summary>Gets what the API said about the code; <see cref="PairingCodeStatus.Unknown"/> also covers a missing code.</summary>
    public PairingCodeStatus CodeStatus { get; private set; } = PairingCodeStatus.Unknown;

    /// <summary>Gets a value indicating whether the page was opened without a code.</summary>
    public bool HasNoCode { get; private set; }

    /// <summary>Gets the pairing waiting for confirmation, once loaded.</summary>
    public PendingPairing? Pairing { get; private set; }

    /// <summary>Gets a value indicating whether the confirmation is being sent.</summary>
    public bool IsConfirming { get; private set; }

    /// <summary>Gets a value indicating whether the code can be confirmed now.</summary>
    public bool CanConfirm => Status == CompanionPageStatus.Ready && CodeStatus == PairingCodeStatus.Waiting && Pairing is not null;

    /// <summary>Gets the notice that replaces the code card when the code can't be confirmed, or <see langword="null"/>.</summary>
    public CompanionNotice? Problem => Status switch
    {
        CompanionPageStatus.Failed => new CompanionNotice(CompanionNoticeKind.Error, "We couldn't check this code", "Nothing was paired. Try again in a moment."),
        CompanionPageStatus.Ready when HasNoCode => new CompanionNotice(CompanionNoticeKind.Info, "Start pairing in the companion", "It shows a code and opens this page with it."),
        CompanionPageStatus.Ready => CodeStatus switch
        {
            PairingCodeStatus.Expired => new CompanionNotice(CompanionNoticeKind.Warning, "This code expired", "Codes last 10 minutes. Get a new code in the companion."),
            PairingCodeStatus.AlreadyConfirmed => new CompanionNotice(CompanionNoticeKind.Info, "This code was already confirmed", "If you confirmed it, the companion is paired. If not, get a new code."),
            PairingCodeStatus.Unknown => new CompanionNotice(CompanionNoticeKind.Warning, "No companion is waiting for this code", "Check the code on your companion, or get a new one there."),
            _ => null,
        },
        _ => null,
    };
    #endregion Properties

    #region Public Methods
    /// <summary>Loads what the API says about the code.</summary>
    /// <param name="userId">The signed-in player, or <see langword="null"/> when the session holds no user id.</param>
    /// <param name="pairingCode">The code from the companion's link, if any.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>A task that completes when the answer is shown or the failure is recorded.</returns>
    public async Task LoadAsync(Guid? userId, string? pairingCode, CancellationToken cancellationToken)
    {
        if (userId is not { } id || id == Guid.Empty)
        {
            Status = CompanionPageStatus.Failed;
            return;
        }

        _userId = id;
        _code = pairingCode?.Trim() ?? string.Empty;
        HasNoCode = _code.Length == 0;
        if (HasNoCode)
        {
            Status = CompanionPageStatus.Ready;
            return;
        }

        Status = CompanionPageStatus.Loading;
        try
        {
            var lookup = await _api.GetPairingAsync(_userId, _code, cancellationToken);
            CodeStatus = lookup.Status;
            Pairing = lookup.Pairing;
            Status = CompanionPageStatus.Ready;
        }
        catch (Exception exception) when (CompanionApiFailures.IsApiFailure(exception, cancellationToken))
        {
            Status = CompanionPageStatus.Failed;
        }
    }

    /// <summary>Labels how long the code still works, in whole minutes rounded up.</summary>
    /// <returns>For example <c>Expires in 9 minutes</c>, <c>Expires in 1 minute</c>, or <c>Expired</c>.</returns>
    public string ExpiryLabel()
    {
        if (Pairing is null)
        {
            return string.Empty;
        }

        var left = Pairing.ExpiresAtUtc - _timeProvider.GetUtcNow();
        if (left <= TimeSpan.Zero)
        {
            return "Expired";
        }

        var minutes = (int)Math.Ceiling(left.TotalMinutes);
        return minutes == 1 ? "Expires in 1 minute" : $"Expires in {minutes} minutes";
    }

    /// <summary>Confirms the code.</summary>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>
    /// <see langword="null"/> when the companion is paired, so the page moves on; otherwise the notification to show.
    /// A code that expired or was used meanwhile changes the page instead and returns no notification either.
    /// </returns>
    public async Task<CompanionNotice?> ConfirmAsync(CancellationToken cancellationToken)
    {
        if (!CanConfirm)
        {
            return null;
        }

        IsConfirming = true;
        try
        {
            var answer = await _api.ConfirmAsync(_userId, _code, cancellationToken);
            CodeStatus = answer;
            if (answer != PairingCodeStatus.Paired)
            {
                Pairing = null;
            }

            return null;
        }
        catch (Exception exception) when (CompanionApiFailures.IsApiFailure(exception, cancellationToken))
        {
            return new CompanionNotice(CompanionNoticeKind.Error, $"{Pairing!.ComputerLabel} wasn't paired", TryAgainDetail);
        }
        finally
        {
            IsConfirming = false;
        }
    }
    #endregion Public Methods
}
