using RaidManager.ViewModels.Features.Companions;

namespace RaidManager.Web.Tests.Support;

/// <summary>Answers the companion routes in memory, as the API would.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the companion view models and pages be tested without a running API.
/// </remarks>
public sealed class FakeCompanionsApiClient : ICompanionsApiClient
{
    #region Fields
    /// <summary>Stores when the companions this double builds were paired: a fixed date before every test clock, so no
    /// test depends on today's date (#557: a stamp from the real clock broke a test from 2026-10-05).</summary>
    public static readonly DateTimeOffset PairedAtUtc = new(2026, 9, 12, 21, 40, 0, TimeSpan.Zero);
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the answer to a code lookup.</summary>
    public PairingLookup Lookup { get; set; } = new(PairingCodeStatus.Waiting, Pending());

    /// <summary>Gets or sets the answer to a confirmation.</summary>
    public PairingCodeStatus Confirmation { get; set; } = PairingCodeStatus.Paired;

    /// <summary>Gets or sets the companions the API lists.</summary>
    public List<PairedCompanion> Companions { get; set; } = [];

    /// <summary>Gets or sets the answer to a revocation.</summary>
    public RevokeOutcome Revocation { get; set; } = RevokeOutcome.Revoked;

    /// <summary>Gets or sets a value indicating whether every call fails, as an unavailable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets or sets a value indicating whether only confirmations and revocations fail.</summary>
    public bool ChangesFail { get; set; }

    /// <summary>Gets the calls received, such as <c>confirm K7M-4QX</c>.</summary>
    public List<string> Calls { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Builds a pairing waiting for confirmation, expiring nine minutes after <paramref name="now"/>.</summary>
    /// <param name="now">The current time; the system time by default.</param>
    /// <returns>The pairing.</returns>
    public static PendingPairing Pending(DateTimeOffset? now = null)
    {
        var at = now ?? DateTimeOffset.UtcNow;
        return new PendingPairing("K7M-4QX", "BRYN-DESKTOP", at.AddMinutes(-1), at.AddMinutes(9));
    }

    /// <summary>Builds a companion paired at <see cref="PairedAtUtc"/>.</summary>
    /// <param name="label">The computer label.</param>
    /// <param name="status">The status.</param>
    /// <param name="revokedAtUtc">When it was revoked.</param>
    /// <returns>The companion.</returns>
    public static PairedCompanion Companion(string label, string status = PairedCompanion.ActiveStatus, DateTimeOffset? revokedAtUtc = null) =>
        new(Guid.NewGuid(), label, PairedAtUtc, status, revokedAtUtc);

    /// <inheritdoc />
    public Task<PairingLookup> GetPairingAsync(Guid userId, string pairingCode, CancellationToken cancellationToken)
    {
        Record($"lookup {pairingCode}", Fails);
        return Task.FromResult(Lookup);
    }

    /// <inheritdoc />
    public Task<PairingCodeStatus> ConfirmAsync(Guid userId, string pairingCode, CancellationToken cancellationToken)
    {
        Record($"confirm {pairingCode}", Fails || ChangesFail);
        return Task.FromResult(Confirmation);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PairedCompanion>> GetCompanionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        Record("list", Fails);
        return Task.FromResult<IReadOnlyList<PairedCompanion>>([.. Companions]);
    }

    /// <inheritdoc />
    public Task<RevokeOutcome> RevokeAsync(Guid userId, Guid companionId, CancellationToken cancellationToken)
    {
        Record($"revoke {companionId}", Fails || ChangesFail);
        if (Revocation == RevokeOutcome.Revoked)
        {
            Companions = [.. Companions.Select(companion => companion.CompanionId == companionId
                ? companion with { Status = PairedCompanion.RevokedStatus, RevokedAtUtc = DateTimeOffset.UtcNow }
                : companion)];
        }

        return Task.FromResult(Revocation);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Records a call, and fails it when asked to.</summary>
    /// <param name="call">The call.</param>
    /// <param name="fail">Whether the call fails.</param>
    /// <exception cref="HttpRequestException">Thrown when the call fails.</exception>
    private void Record(string call, bool fail)
    {
        Calls.Add(call);
        if (fail)
        {
            throw new HttpRequestException("The API is unavailable.");
        }
    }
    #endregion Private Helpers
}
