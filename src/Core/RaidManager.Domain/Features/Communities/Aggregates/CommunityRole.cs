using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Aggregates;

/// <summary>Represents a RaidManager role in a community: a name and the permissions it allows.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Lets each community shape its own roles (story #308). Officer and Raid leader are the presets every
/// community starts with; Discord roles mapped to a role give its permissions. Administrator and Member aren't roles
/// here: the Administrator is who added the bot and has every permission, and everyone else in the server is a Member.
/// </remarks>
public sealed class CommunityRole : Entity<CommunityRoleId>
{
    #region Constants
    /// <summary>Defines the longest role name.</summary>
    public const int MaximumNameLength = 50;

    /// <summary>Defines the name of the Officer preset.</summary>
    public const string OfficerName = "Officer";

    /// <summary>Defines the name of the Raid leader preset.</summary>
    public const string RaidLeaderName = "Raid leader";

    /// <summary>Defines what the Officer preset allows: every raid permission, not managing roles (owner decision on #308).</summary>
    public const CommunityPermissions OfficerPermissions =
        CommunityPermissions.ManageRaids | CommunityPermissions.BuildRosters | CommunityPermissions.RunRaidNight | CommunityPermissions.ReviewConflicts;

    /// <summary>Defines what the Raid leader preset allows (owner decision on #308).</summary>
    public const CommunityPermissions RaidLeaderPermissions =
        CommunityPermissions.ManageRaids | CommunityPermissions.BuildRosters | CommunityPermissions.RunRaidNight;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private CommunityRole()
        : base(new CommunityRoleId(Guid.NewGuid()))
    {
        Name = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="CommunityRole"/> class.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What the role allows.</param>
    /// <param name="position">The role's place in the community's list.</param>
    private CommunityRole(CommunityRoleId id, string name, CommunityPermissions permissions, int position)
        : base(id)
    {
        Name = name;
        Permissions = permissions;
        Position = position;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the role name, such as <c>Officer</c> or <c>Veteran</c>.</summary>
    public string Name { get; private set; }

    /// <summary>Gets what the role allows.</summary>
    public CommunityPermissions Permissions { get; private set; }

    /// <summary>Gets the role's place in the community's list: the presets first, then roles in the order they were created.</summary>
    public int Position { get; private set; }
    #endregion Properties

    #region Domain Behavior
    /// <summary>Tells whether the role allows a permission.</summary>
    /// <param name="permission">The permission.</param>
    /// <returns><see langword="true"/> when the role allows it.</returns>
    public bool Allows(CommunityPermissions permission) => (Permissions & permission) == permission;
    #endregion Domain Behavior

    #region Factory Methods
    /// <summary>Creates a role.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What the role allows.</param>
    /// <param name="position">The role's place in the community's list.</param>
    /// <returns>The role.</returns>
    /// <exception cref="DomainException">Thrown when the name is blank or too long, or the permissions aren't known.</exception>
    internal static CommunityRole Create(string name, CommunityPermissions permissions, int position)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new UnknownDomainException("A role needs a name.");
        }

        if (trimmed.Length > MaximumNameLength)
        {
            throw new UnknownDomainException($"A role name cannot be longer than {MaximumNameLength} characters.");
        }

        if ((permissions & ~CommunityPermissions.All) != CommunityPermissions.None)
        {
            throw new UnknownDomainException("A role can only allow the known permissions.");
        }

        return new CommunityRole(new CommunityRoleId(Guid.NewGuid()), trimmed, permissions, position);
    }
    #endregion Factory Methods
}
