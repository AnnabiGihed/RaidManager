using RaidManager.Domain.Features.Communities.Enums;
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
/// Purpose: Keeps what RaidManager owns about a community: its server, realm, Administrator and the Discord roles that
/// give RaidManager permissions. Membership and each member's Discord roles stay in Discord and are read at check time
/// (ADR-0022), so the community gives a role from them rather than storing members.
/// </remarks>
public sealed class Community : AggregateRoot<CommunityId>
{
    #region Constants
    /// <summary>Defines the longest stored community name; Discord limits server names to 100 characters.</summary>
    public const int MaximumNameLength = 100;
    #endregion Constants

    #region Fields
    /// <summary>Stores the Discord roles mapped to an Officer or Raid leader role.</summary>
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

    /// <summary>Gets the Discord roles that give an Officer or Raid leader role.</summary>
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
        community.RaiseDomainEvent(new CommunityCreated(community.Id, community.DiscordGuildId, community.Name));
        return community;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Maps a Discord role to Officer or Raid leader, replacing any earlier mapping of that Discord role.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The RaidManager role it gives: <see cref="CommunityMemberRole.Officer"/> or <see cref="CommunityMemberRole.RaidLeader"/>.</param>
    /// <exception cref="DomainException">Thrown when the role id is not a snowflake or the role can't be mapped.</exception>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/> unless the mapping already existed.</remarks>
    public void MapDiscordRole(string discordRoleId, CommunityMemberRole role)
    {
        var mapping = DiscordRoleMapping.Create(discordRoleId, role);
        if (_roleMappings.Contains(mapping))
        {
            return;
        }

        _roleMappings.RemoveAll(existing => existing.DiscordRoleId == mapping.DiscordRoleId);
        _roleMappings.Add(mapping);
        RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
    }

    /// <summary>Removes a Discord role's mapping, so it no longer gives a RaidManager role.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <remarks>Raises <see cref="CommunityRoleMappingsChanged"/> only when the role was mapped.</remarks>
    public void UnmapDiscordRole(string discordRoleId)
    {
        if (_roleMappings.RemoveAll(existing => existing.DiscordRoleId == discordRoleId) > 0)
        {
            RaiseDomainEvent(new CommunityRoleMappingsChanged(Id));
        }
    }

    /// <summary>Gives the RaidManager role of a member of the Discord server from their current Discord roles.</summary>
    /// <param name="userId">The member.</param>
    /// <param name="discordRoleIds">The member's current Discord role snowflakes, as Discord reports them.</param>
    /// <returns>
    /// <see cref="CommunityMemberRole.Administrator"/> for the Administrator, otherwise the highest mapped role among the
    /// member's Discord roles, otherwise <see cref="CommunityMemberRole.Member"/>.
    /// </returns>
    /// <remarks>Whether the user is in the server at all is checked with Discord before asking for a role.</remarks>
    public CommunityMemberRole RoleFor(UserId userId, IEnumerable<string> discordRoleIds)
    {
        if (userId == AdministratorId)
        {
            return CommunityMemberRole.Administrator;
        }

        var memberRoles = discordRoleIds.ToHashSet(StringComparer.Ordinal);
        return _roleMappings
            .Where(mapping => memberRoles.Contains(mapping.DiscordRoleId))
            .Select(mapping => mapping.Role)
            .DefaultIfEmpty(CommunityMemberRole.Member)
            .Max();
    }
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
    #endregion Invariants
}
