using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Aggregates;

/// <summary>Represents one user's request to own a character, reviewed before the character can be offered for raids.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Records the claim lifecycle (pending, approved, rejected, conflict) owned by the character aggregate.
/// </remarks>
public sealed class CharacterClaim : Entity<CharacterClaimId>
{
    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private CharacterClaim()
        : base(new CharacterClaimId(Guid.NewGuid()))
    {
        RequestedByUserId = new UserId(Guid.NewGuid());
    }

    /// <summary>Initializes a new instance of the <see cref="CharacterClaim"/> class.</summary>
    /// <param name="id">The claim identifier.</param>
    /// <param name="requestedByUserId">The user asking to own the character.</param>
    /// <param name="state">The initial review state.</param>
    /// <param name="requestedAtUtc">The UTC instant of the request.</param>
    private CharacterClaim(CharacterClaimId id, UserId requestedByUserId, CharacterClaimState state, DateTimeOffset requestedAtUtc)
        : base(id)
    {
        RequestedByUserId = requestedByUserId;
        State = state;
        RequestedAtUtc = requestedAtUtc;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the user asking to own the character.</summary>
    public UserId RequestedByUserId { get; private set; }

    /// <summary>Gets the current review state.</summary>
    public CharacterClaimState State { get; private set; }

    /// <summary>Gets the UTC instant at which the claim was first requested.</summary>
    public DateTimeOffset RequestedAtUtc { get; private set; }

    /// <summary>Gets the UTC instant of the latest decision, or <see langword="null"/> while no decision was made.</summary>
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a claim in the given initial state.</summary>
    /// <param name="requestedByUserId">The user asking to own the character.</param>
    /// <param name="state">The initial state: pending, or conflict when another identity already owns the character.</param>
    /// <param name="requestedAtUtc">The UTC instant of the request.</param>
    /// <returns>The created claim.</returns>
    internal static CharacterClaim Create(UserId requestedByUserId, CharacterClaimState state, DateTimeOffset requestedAtUtc) =>
        new(new CharacterClaimId(Guid.NewGuid()), requestedByUserId, state, requestedAtUtc);
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Moves the claim to a decided state.</summary>
    /// <param name="state">The new state.</param>
    /// <param name="decidedAtUtc">The UTC instant of the decision.</param>
    internal void Decide(CharacterClaimState state, DateTimeOffset decidedAtUtc)
    {
        State = state;
        DecidedAtUtc = decidedAtUtc;
    }
    #endregion Domain Behavior
}
