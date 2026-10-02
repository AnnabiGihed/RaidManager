using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Events;
using RaidManager.Domain.Features.Communities.ValueObjects;
using RaidManager.Domain.Features.Shared.Discord;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Aggregates;

/// <summary>Represents a Discord server linked to RaidManager and raiding on one Warmane realm.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps what RaidManager owns about a community: its server, realm, Administrator, its roles with what each
/// allows, and the Discord roles that give them. Membership and each member's Discord roles stay in Discord and are read
/// at check time (ADR-0022), so the community gives roles and permissions from them rather than storing members.
/// </remarks>
public sealed class Community : AggregateRoot<CommunityId>
{
    #region Constants
    /// <summary>Defines the longest stored community name; Discord limits server names to 100 characters.</summary>
    public const int MaximumNameLength = 100;
    #endregion Constants

    #region Fields
    /// <summary>Stores the community's roles.</summary>
    private readonly List<CommunityRole> _roles = [];

    /// <summary>Stores the Discord roles mapped to the community's roles.</summary>
    private readonly List<DiscordRoleMapping> _roleMappings = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Community()
        : base(new CommunityId(Guid.NewGuid()))
    {
        DiscordGuildId = string.Empty;
        Name = string.Empty;
        AdministratorId = new UserId(Guid.NewGuid());
    }

    /// <summary>Initializes a new instance of the <see cref="Community"/> class.</summary>
    /// <param name="id">The community identifier.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="name">The community name.</param>
    /// <param name="realm">The Warmane realm the community raids on.</param>
    /// <param name="administratorId">The user who added the bot to the server.</param>
    private Community(CommunityId id, string discordGuildId, string name, WarmaneRealm realm, UserId administratorId)
        : base(id)
    {
        DiscordGuildId = discordGuildId;
        Name = name;
        Realm = realm;
        AdministratorId = administratorId;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Discord server snowflake; one server links to one community.</summary>
    public string DiscordGuildId { get; private set; }

    /// <summary>Gets the community name, taken from the Discord server.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the Warmane realm characters, raids and lockouts are checked against.</summary>
    public WarmaneRealm Realm { get; private set; }

    /// <summary>Gets the user who added RaidManager to the server and administers the community.</summary>
    public UserId AdministratorId { get; private set; }

    /// <summary>Gets the community's roles, in their list order.</summary>
    public IReadOnlyList<CommunityRole> Roles => [.. _roles.OrderBy(role => role.Position)];

    /// <summary>Gets the Discord roles that give the community's roles.</summary>
    public IReadOnlyCollection<DiscordRoleMapping> RoleMappings => _roleMappings.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Links a Discord server to RaidManager; the user who added the bot becomes its Administrator.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="name">The server name.</param>
    /// <param name="realm">The Warmane realm the community raids on.</param>
    /// <param name="administratorId">The user who added the bot to the server.</param>
    /// <returns>The linked community.</returns>
    /// <exception cref="DomainException">Thrown when the server id is not a snowflake or the name is blank or too long.</exception>
    public static Community Link(string discordGuildId, string name, WarmaneRealm realm, UserId administratorId)
    {
        var guildId = DiscordSnowflake.Ensure(discordGuildId, "Discord server");
        var trimmedName = EnsureName(name);
        var community = new Community(new CommunityId(Guid.NewGuid()), guildId, trimmedName, realm, administratorId);

        // Every community starts with the Officer and Raid leader presets (owner decision on #308).
        community._roles.Add(CommunityRole.Create(CommunityRole.OfficerName, CommunityRole.OfficerPermissions, 1));
        community._roles.Add(CommunityRole.Create(CommunityRole.RaidLeaderName, CommunityRole.RaidLeaderPermissions, 2));
        community.RaiseDomainEvent(new CommunityCreated(community.Id, community.DiscordGuildId, community.Name));
        return community;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Tells whether someone may change a role: only the Administrator changes a role that grants Manage community roles.</summary>
    /// <param name="role">The role.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking.</param>
    /// <returns><see langword="true"/> when they may edit, delete or map it (owner decision on #308).</returns>
    public static bool CanChange(CommunityRole role, bool byAdministrator)
    {
        ArgumentNullException.ThrowIfNull(role);
        return byAdministrator || !role.Allows(CommunityPermissions.ManageCommunityRoles);
    }

    /// <summary>Takes the Discord server's current name, when it was renamed in Discord.</summary>
    /// <param name="name">The server's name as Discord reports it now.</param>
    /// <returns><see langword="true"/> when the name changed; <see langword="false"/> when it was already current.</returns>
    /// <exception cref="DomainException">Thrown when the name is blank or too long.</exception>
    /// <remarks>RaidManager keeps a copy of the name and refreshes it whenever it reads the server (owner decision on #14).</remarks>
    public bool Rename(string name)
    {
        var trimmedName = EnsureName(name);
        if (trimmedName == Name)
        {
            return false;
        }

        Name = trimmedName;
        return true;
    }

    /// <summary>Creates a role with a name and the permissions it allows, at the end of the list.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What the role allows.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking; only they can allow Manage community roles.</param>
    /// <returns>The role, or a failure: <see cref="CommunityErrors.CannotGrantRoleManagement"/>, <see cref="CommunityErrors.RoleNameInvalid"/>, <see cref="CommunityErrors.RolePermissionsInvalid"/> or <see cref="CommunityErrors.RoleNameTaken"/>.</returns>
    public Result<CommunityRole> CreateRole(string name, CommunityPermissions permissions, bool byAdministrator)
    {
        var checkedName = CheckRole(name, permissions, byAdministrator, null);
        if (checkedName.IsFailure)
        {
            return Result.Failure<CommunityRole>(checkedName.Error, checkedName.ResultExceptionType);
        }

        var role = CommunityRole.Create(checkedName.Value, permissions, _roles.Select(existing => existing.Position).DefaultIfEmpty(0).Max() + 1);
        _roles.Add(role);
        return Result.Success(role);
    }

    /// <summary>Renames one of the community's roles and changes what it allows.</summary>
    /// <param name="roleId">The role.</param>
    /// <param name="name">The new name.</param>
    /// <param name="permissions">What the role allows now.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking.</param>
    /// <returns>Success, or a failure: <see cref="CommunityErrors.RoleNotFound"/>, <see cref="CommunityErrors.RoleLocked"/>, or the failures of <see cref="CreateRole"/>.</returns>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/>, so cached checks pick up the new permissions.</remarks>
    public Result UpdateRole(CommunityRoleId roleId, string name, CommunityPermissions permissions, bool byAdministrator)
    {
        var role = ChangeableRole(roleId, byAdministrator);
        if (role.IsFailure)
        {
            return role;
        }

        var checkedName = CheckRole(name, permissions, byAdministrator, roleId);
        if (checkedName.IsFailure)
        {
            return checkedName;
        }

        role.Value.Change(checkedName.Value, permissions);
        RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
        return Result.Success();
    }

    /// <summary>Deletes one of the community's roles; the Discord roles mapped to it stop giving it.</summary>
    /// <param name="roleId">The role.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking.</param>
    /// <returns>Success, or <see cref="CommunityErrors.RoleNotFound"/> or <see cref="CommunityErrors.RoleLocked"/>.</returns>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/>, so its members lose what it allowed at their next check.</remarks>
    public Result DeleteRole(CommunityRoleId roleId, bool byAdministrator)
    {
        var role = ChangeableRole(roleId, byAdministrator);
        if (role.IsFailure)
        {
            return role;
        }

        _roles.Remove(role.Value);
        _roleMappings.RemoveAll(mapping => mapping.RoleId == roleId);
        RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
        return Result.Success();
    }

    /// <summary>Finds one of the community's roles.</summary>
    /// <param name="roleId">The role.</param>
    /// <returns>The role, or <see langword="null"/> when the community has no such role.</returns>
    public CommunityRole? FindRole(CommunityRoleId roleId) => _roles.Find(role => role.Id == roleId);

    /// <summary>Maps a Discord role to one of the community's roles; one Discord role can give several (owner decision on #289).</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role it gives.</param>
    /// <exception cref="DomainException">Thrown when the Discord role id is not a snowflake or the community has no such role.</exception>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/> unless the mapping already existed.</remarks>
    public void MapDiscordRole(string discordRoleId, CommunityRoleId roleId)
    {
        if (FindRole(roleId) is null)
        {
            throw new UnknownDomainException("The community has no such role.");
        }

        var mapping = DiscordRoleMapping.Create(discordRoleId, roleId);
        if (_roleMappings.Contains(mapping))
        {
            return;
        }

        _roleMappings.Add(mapping);
        RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
    }

    /// <summary>Stops a Discord role giving one of the community's roles; any other role it gives stays.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role it should no longer give.</param>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/> only when that mapping existed.</remarks>
    public void UnmapDiscordRole(string discordRoleId, CommunityRoleId roleId)
    {
        if (_roleMappings.RemoveAll(existing => existing.DiscordRoleId == discordRoleId && existing.RoleId == roleId) > 0)
        {
            RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
        }
    }

    /// <summary>Gives the community's roles a set of Discord roles maps to, leaving the Administrator aside.</summary>
    /// <param name="discordRoleIds">A member's current Discord role snowflakes, as Discord reports them.</param>
    /// <returns>The roles, in their list order; empty for a Member.</returns>
    public IReadOnlyList<CommunityRole> RolesFor(IEnumerable<string> discordRoleIds)
    {
        var memberRoles = discordRoleIds.ToHashSet(StringComparer.Ordinal);
        var mapped = _roleMappings
            .Where(mapping => memberRoles.Contains(mapping.DiscordRoleId))
            .Select(mapping => mapping.RoleId)
            .ToHashSet();
        return [.. Roles.Where(role => mapped.Contains(role.Id))];
    }

    /// <summary>Gives what a member of the Discord server may do, from their current Discord roles.</summary>
    /// <param name="userId">The member.</param>
    /// <param name="discordRoleIds">The member's current Discord role snowflakes, as Discord reports them.</param>
    /// <returns>
    /// <see cref="CommunityPermissions.All"/> for the Administrator, otherwise every permission of every role their
    /// Discord roles give them; <see cref="CommunityPermissions.None"/> for a Member.
    /// </returns>
    /// <remarks>Whether the user is in the server at all is checked with Discord before asking (ADR-0022).</remarks>
    public CommunityPermissions PermissionsFor(UserId userId, IEnumerable<string> discordRoleIds) =>
        userId == AdministratorId
            ? CommunityPermissions.All
            : RolesFor(discordRoleIds).Aggregate(CommunityPermissions.None, (permissions, role) => permissions | role.Permissions);
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures a community has a name of acceptable length.</summary>
    /// <param name="name">The requested name.</param>
    /// <returns>The trimmed name.</returns>
    /// <exception cref="DomainException">Thrown when the name is blank or too long.</exception>
    private static string EnsureName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new UnknownDomainException("Community name is required.");
        }

        if (trimmed.Length > MaximumNameLength)
        {
            throw new UnknownDomainException($"A community name cannot be longer than {MaximumNameLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Checks a role's name and permissions, and that the person asking may give those permissions.</summary>
    /// <param name="name">The requested name.</param>
    /// <param name="permissions">The requested permissions.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking.</param>
    /// <param name="roleId">The role being changed, which may keep its own name; <see langword="null"/> for a new role.</param>
    /// <returns>The trimmed name, or the failure.</returns>
    private Result<string> CheckRole(string name, CommunityPermissions permissions, bool byAdministrator, CommunityRoleId? roleId)
    {
        if ((permissions & ~CommunityPermissions.All) != CommunityPermissions.None)
        {
            return Result.Failure<string>(CommunityErrors.RolePermissionsInvalid);
        }

        if (!byAdministrator && (permissions & CommunityPermissions.ManageCommunityRoles) != CommunityPermissions.None)
        {
            return Result.Failure<string>(CommunityErrors.CannotGrantRoleManagement, ResultExceptionType.AccessDenied);
        }

        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > CommunityRole.MaximumNameLength)
        {
            return Result.Failure<string>(CommunityErrors.RoleNameInvalid);
        }

        return _roles.Exists(role => role.Id != roleId && string.Equals(role.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            ? Result.Failure<string>(CommunityErrors.RoleNameTaken, ResultExceptionType.Conflict)
            : Result.Success(trimmed);
    }

    /// <summary>Finds a role the person asking may change.</summary>
    /// <param name="roleId">The role.</param>
    /// <param name="byAdministrator">Whether the Administrator is asking.</param>
    /// <returns>The role, or <see cref="CommunityErrors.RoleNotFound"/> or <see cref="CommunityErrors.RoleLocked"/>.</returns>
    private Result<CommunityRole> ChangeableRole(CommunityRoleId roleId, bool byAdministrator)
    {
        var role = FindRole(roleId);
        if (role is null)
        {
            return Result.Failure<CommunityRole>(CommunityErrors.RoleNotFound, ResultExceptionType.NotFound);
        }

        return CanChange(role, byAdministrator)
            ? Result.Success(role)
            : Result.Failure<CommunityRole>(CommunityErrors.RoleLocked, ResultExceptionType.AccessDenied);
    }
    #endregion Invariants
}
