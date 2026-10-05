using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Companions.Enums;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Events;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Aggregates;

/// <summary>Represents a desktop companion paired with one player, identified by the hash of its device token.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Holds the token rules of ADR-0030: the token is checked on every request, a revoked companion is refused at
/// once, and one unused for <see cref="UnusedLifetime"/> expires. Last use is written at most every
/// <see cref="LastUseResolution"/> to keep writes low.
/// </remarks>
public sealed class Companion : AggregateRoot<CompanionId>
{
    #region Static Instances
    /// <summary>Gets how long a companion may go unused before its token expires (owner decision on #37).</summary>
    public static readonly TimeSpan UnusedLifetime = TimeSpan.FromDays(180);

    /// <summary>Gets how often, at most, last use is written.</summary>
    public static readonly TimeSpan LastUseResolution = TimeSpan.FromHours(1);
    #endregion Static Instances

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Companion()
        : base(new CompanionId(Guid.NewGuid()))
    {
        UserId = new UserId(Guid.NewGuid());
        Label = string.Empty;
        TokenHash = CredentialHash.Of("materialization");
    }

    /// <summary>Initializes a new instance of the <see cref="Companion"/> class.</summary>
    /// <param name="id">The companion identifier.</param>
    /// <param name="userId">The player it uploads for.</param>
    /// <param name="label">The computer's label.</param>
    /// <param name="tokenHash">The hash of its device token.</param>
    /// <param name="pairedAtUtc">When it was paired.</param>
    private Companion(CompanionId id, UserId userId, string label, CredentialHash tokenHash, DateTimeOffset pairedAtUtc)
        : base(id)
    {
        UserId = userId;
        Label = label;
        TokenHash = tokenHash;
        PairedAtUtc = pairedAtUtc;
        LastUsedAtUtc = pairedAtUtc;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the player the companion uploads for.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets the computer's label.</summary>
    public string Label { get; private set; }

    /// <summary>Gets the hash of the device token; the token itself is never kept.</summary>
    public CredentialHash TokenHash { get; private set; }

    /// <summary>Gets when the companion was paired.</summary>
    public DateTimeOffset PairedAtUtc { get; private set; }

    /// <summary>Gets when the companion last called the API, to within <see cref="LastUseResolution"/>.</summary>
    public DateTimeOffset LastUsedAtUtc { get; private set; }

    /// <summary>Gets when the player revoked the companion, if they did.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Gets when the companion last uploaded a character snapshot, if it ever did.</summary>
    public DateTimeOffset? LastUploadAtUtc { get; private set; }
    #endregion Properties

    #region Domain Behavior
    /// <summary>Tells whether the companion may call the API.</summary>
    /// <param name="nowUtc">The current time.</param>
    /// <returns>Revoked, expired after <see cref="UnusedLifetime"/> without use, or active.</returns>
    public CompanionStatus StatusAt(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return CompanionStatus.Revoked;
        }

        return nowUtc - LastUsedAtUtc > UnusedLifetime ? CompanionStatus.Expired : CompanionStatus.Active;
    }

    /// <summary>Admits a request made with this companion's token, and records its use.</summary>
    /// <param name="nowUtc">The current time.</param>
    /// <returns>
    /// <see langword="true"/> when last use changed and must be saved; or <see cref="CompanionErrors.Revoked"/> or
    /// <see cref="CompanionErrors.Expired"/> (<see cref="ResultExceptionType.AuthenticationRequired"/>).
    /// </returns>
    public Result<bool> Use(DateTimeOffset nowUtc)
    {
        switch (StatusAt(nowUtc))
        {
            case CompanionStatus.Revoked:
                return Result.Failure<bool>(CompanionErrors.Revoked, ResultExceptionType.AuthenticationRequired);
            case CompanionStatus.Expired:
                return Result.Failure<bool>(CompanionErrors.Expired, ResultExceptionType.AuthenticationRequired);
        }

        if (nowUtc - LastUsedAtUtc < LastUseResolution)
        {
            return false;
        }

        LastUsedAtUtc = nowUtc;
        return true;
    }

    /// <summary>Revokes the companion for its player; it can't call the API again.</summary>
    /// <param name="userId">The player asking.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <returns>
    /// Success; <see cref="CompanionErrors.NotFound"/> (<see cref="ResultExceptionType.NotFound"/>) when it isn't the player's,
    /// so another player's computers stay hidden; or <see cref="CompanionErrors.AlreadyRevoked"/> (<see cref="ResultExceptionType.Conflict"/>).
    /// </returns>
    public Result Revoke(UserId userId, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (UserId != userId)
        {
            return Result.Failure(CompanionErrors.NotFound, ResultExceptionType.NotFound);
        }

        if (RevokedAtUtc is not null)
        {
            return Result.Failure(CompanionErrors.AlreadyRevoked, ResultExceptionType.Conflict);
        }

        RevokedAtUtc = nowUtc;
        RaiseDomainEvent(new CompanionRevoked(Id, userId));
        return Result.Success();
    }

    /// <summary>Records that the companion uploaded a character snapshot, which the website's paired companions list shows.</summary>
    /// <param name="nowUtc">The UTC instant of the upload.</param>
    public void RecordUpload(DateTimeOffset nowUtc) => LastUploadAtUtc = LastUploadAtUtc > nowUtc ? LastUploadAtUtc : nowUtc;
    #endregion Domain Behavior

    #region Internal Methods
    /// <summary>Pairs a companion for the player who confirmed its pairing request.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="label">The computer's label.</param>
    /// <param name="tokenHash">The hash of its device token.</param>
    /// <param name="pairedAtUtc">When it was paired.</param>
    /// <returns>The paired companion.</returns>
    internal static Companion Pair(UserId userId, string label, CredentialHash tokenHash, DateTimeOffset pairedAtUtc)
    {
        var companion = new Companion(new CompanionId(Guid.NewGuid()), userId, label, tokenHash, pairedAtUtc);
        companion.RaiseDomainEvent(new CompanionPaired(companion.Id, userId, label));
        return companion;
    }
    #endregion Internal Methods
}
