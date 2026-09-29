using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Aggregates;

/// <summary>Represents one user's membership and authorization role inside a raiding community.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps community authorization as domain state independent from Discord role implementation details.
/// </remarks>
public sealed class CommunityMember : Entity<UserId>
{
    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private CommunityMember()
        : base(new UserId(Guid.NewGuid()))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CommunityMember"/> class.</summary>
    /// <param name="userId">The platform user identifier.</param>
    /// <param name="role">The community role.</param>
    private CommunityMember(UserId userId, CommunityMemberRole role)
        : base(userId)
    {
        Role = role;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the member's community role.</summary>
    public CommunityMemberRole Role { get; private set; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a community membership.</summary>
    /// <param name="userId">The platform user identifier.</param>
    /// <param name="role">The role to assign.</param>
    /// <returns>The community membership.</returns>
    public static CommunityMember Create(UserId userId, CommunityMemberRole role) => new(userId, role);
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Changes the authorization role assigned to this member.</summary>
    /// <param name="role">The replacement role.</param>
    public void ChangeRole(CommunityMemberRole role) => Role = role;
    #endregion Domain Behavior
}
