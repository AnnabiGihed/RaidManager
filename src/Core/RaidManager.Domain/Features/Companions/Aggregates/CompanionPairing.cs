using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Events;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Aggregates;

/// <summary>Represents a companion's request to be paired, confirmed once by its player and collected once by the companion.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the code flow of ADR-0030, after the OAuth 2.0 device authorization grant: the companion holds a
/// secret device code (kept here only as a hash), shows a short pairing code, and polls until the player confirms it on
/// the website. A request lasts <see cref="Lifetime"/> and pairs at most one companion.
/// </remarks>
public sealed class CompanionPairing : AggregateRoot<CompanionPairingId>
{
    #region Constants
    /// <summary>Defines the longest computer label kept; longer labels are cut.</summary>
    public const int MaximumLabelLength = 64;
    #endregion Constants

    #region Static Instances
    /// <summary>Gets how long a pairing request can be confirmed and collected.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    #endregion Static Instances

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private CompanionPairing()
        : base(new CompanionPairingId(Guid.NewGuid()))
    {
        DeviceCodeHash = CredentialHash.Of("materialization");
        Code = PairingCode.Create(PairingCode.Alphabet[..PairingCode.Length]);
        ComputerLabel = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="CompanionPairing"/> class.</summary>
    /// <param name="id">The pairing identifier.</param>
    /// <param name="deviceCodeHash">The hash of the companion's device code.</param>
    /// <param name="code">The code the companion shows.</param>
    /// <param name="computerLabel">The computer's label.</param>
    /// <param name="requestedAtUtc">When the companion asked.</param>
    private CompanionPairing(CompanionPairingId id, CredentialHash deviceCodeHash, PairingCode code, string computerLabel, DateTimeOffset requestedAtUtc)
        : base(id)
    {
        DeviceCodeHash = deviceCodeHash;
        Code = code;
        ComputerLabel = computerLabel;
        RequestedAtUtc = requestedAtUtc;
        ExpiresAtUtc = requestedAtUtc + Lifetime;
        State = CompanionPairingState.Pending;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the hash of the device code only the companion knows.</summary>
    public CredentialHash DeviceCodeHash { get; private set; }

    /// <summary>Gets the code the companion shows and the player confirms.</summary>
    public PairingCode Code { get; private set; }

    /// <summary>Gets the computer's label, as the companion sent it.</summary>
    public string ComputerLabel { get; private set; }

    /// <summary>Gets when the companion asked to be paired.</summary>
    public DateTimeOffset RequestedAtUtc { get; private set; }

    /// <summary>Gets when the request stops being usable.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Gets where the request is in the flow.</summary>
    public CompanionPairingState State { get; private set; }

    /// <summary>Gets the player who confirmed the request, once confirmed.</summary>
    public UserId? ConfirmedByUserId { get; private set; }

    /// <summary>Gets when the player confirmed the request.</summary>
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    /// <summary>Gets the companion the request paired, once completed.</summary>
    public CompanionId? CompanionId { get; private set; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Starts a pairing request for a companion.</summary>
    /// <param name="deviceCodeHash">The hash of the companion's device code.</param>
    /// <param name="code">The code the companion will show.</param>
    /// <param name="computerLabel">The computer's label; blank labels become <c>Unnamed computer</c>, long ones are cut.</param>
    /// <param name="requestedAtUtc">When the companion asked.</param>
    /// <returns>The pending request.</returns>
    public static CompanionPairing Start(CredentialHash deviceCodeHash, PairingCode code, string? computerLabel, DateTimeOffset requestedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(deviceCodeHash);
        ArgumentNullException.ThrowIfNull(code);
        return new CompanionPairing(new CompanionPairingId(Guid.NewGuid()), deviceCodeHash, code, NormalizeLabel(computerLabel), requestedAtUtc);
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Tells whether the request can no longer be used.</summary>
    /// <param name="nowUtc">The current time.</param>
    /// <returns><see langword="true"/> from <see cref="ExpiresAtUtc"/> on.</returns>
    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Records that the signed-in player confirmed the code on the website.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <returns>
    /// Success, or <see cref="CompanionErrors.PairingExpired"/> or <see cref="CompanionErrors.PairingAlreadyConfirmed"/>
    /// (<see cref="ResultExceptionType.Conflict"/>).
    /// </returns>
    public Result Confirm(UserId userId, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (State != CompanionPairingState.Pending)
        {
            return Result.Failure(CompanionErrors.PairingAlreadyConfirmed, ResultExceptionType.Conflict);
        }

        if (IsExpired(nowUtc))
        {
            return Result.Failure(CompanionErrors.PairingExpired, ResultExceptionType.Conflict);
        }

        State = CompanionPairingState.Confirmed;
        ConfirmedByUserId = userId;
        ConfirmedAtUtc = nowUtc;
        RaiseDomainEvent(new CompanionPairingConfirmed(Id, userId));
        return Result.Success();
    }

    /// <summary>Pairs the companion that polls with the request's device code, once the player confirmed it.</summary>
    /// <param name="tokenHash">The hash of the device token the companion will receive.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <returns>
    /// The paired companion; or <see cref="CompanionErrors.PairingPending"/>, <see cref="CompanionErrors.PairingExpired"/> or
    /// <see cref="CompanionErrors.PairingInvalid"/> (<see cref="ResultExceptionType.ValidationError"/>, as the device grant answers 400).
    /// </returns>
    /// <remarks>A completed request answers <see cref="CompanionErrors.PairingInvalid"/>, so a device token is handed out once.</remarks>
    public Result<Companion> Complete(CredentialHash tokenHash, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        if (State == CompanionPairingState.Completed)
        {
            return Result.Failure<Companion>(CompanionErrors.PairingInvalid);
        }

        if (IsExpired(nowUtc))
        {
            return Result.Failure<Companion>(CompanionErrors.PairingExpired);
        }

        if (State == CompanionPairingState.Pending || ConfirmedByUserId is null)
        {
            return Result.Failure<Companion>(CompanionErrors.PairingPending);
        }

        var companion = Companion.Pair(ConfirmedByUserId, ComputerLabel, tokenHash, nowUtc);
        State = CompanionPairingState.Completed;
        CompanionId = companion.Id;
        return companion;
    }
    #endregion Domain Behavior

    #region Private Helpers
    /// <summary>Trims a label, names a blank one and cuts a long one.</summary>
    /// <param name="computerLabel">The label the companion sent.</param>
    /// <returns>The label to keep.</returns>
    private static string NormalizeLabel(string? computerLabel)
    {
        var trimmed = computerLabel?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return "Unnamed computer";
        }

        return trimmed.Length <= MaximumLabelLength ? trimmed : trimmed[..MaximumLabelLength].TrimEnd();
    }
    #endregion Private Helpers
}
