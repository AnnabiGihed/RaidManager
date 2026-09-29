using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Events;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Aggregates;

/// <summary>Represents a Discord-backed raiding community operating on a Warmane realm.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Owns raid-community membership without coupling the domain to Discord bot or API client implementations.
/// </remarks>
public sealed class Community : AggregateRoot<CommunityId>
{
    #region Fields
    /// <summary>Stores platform users belonging to the community.</summary>
    private readonly List<CommunityMember> _members = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Community()
        : base(new CommunityId(Guid.NewGuid()))
    {
        DiscordGuildId = string.Empty;
        Name = string.Empty;
        OwnerId = new UserId(Guid.Empty);
    }

    /// <summary>Initializes a new instance of the <see cref="Community"/> class.</summary>
    /// <param name="id">The community identifier.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="name">The community name.</param>
    /// <param name="realm">The primary Warmane realm.</param>
    /// <param name="ownerId">The platform user creating the community.</param>
    private Community(CommunityId id, string discordGuildId, string name, WarmaneRealm realm, UserId ownerId)
        : base(id)
    {
        DiscordGuildId = discordGuildId;
        Name = name;
        Realm = realm;
        OwnerId = ownerId;
        _members.Add(CommunityMember.Create(ownerId, CommunityMemberRole.Administrator));
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Discord guild snowflake linked to the community.</summary>
    public string DiscordGuildId { get; private set; }

    /// <summary>Gets the display name of the raiding community.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the primary Warmane realm organized by the community.</summary>
    public WarmaneRealm Realm { get; private set; }

    /// <summary>Gets the user who created and owns the community.</summary>
    public UserId OwnerId { get; private set; }

    /// <summary>Gets the current platform memberships.</summary>
    public IReadOnlyCollection<CommunityMember> Members => _members.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a Discord-backed raiding community.</summary>
    /// <param name="discordGuildId">The Discord guild snowflake.</param>
    /// <param name="name">The community name.</param>
    /// <param name="realm">The primary Warmane realm.</param>
    /// <param name="ownerId">The creating platform user.</param>
    /// <returns>The created community.</returns>
    public static Community Create(string discordGuildId, string name, WarmaneRealm realm, UserId ownerId)
    {
        EnsureDiscordGuildId(discordGuildId);
        EnsureName(name);
        var community = new Community(new CommunityId(Guid.NewGuid()), discordGuildId.Trim(), name.Trim(), realm, ownerId);
        community.RaiseDomainEvent(new CommunityCreated(community.Id, community.DiscordGuildId, community.Name));
        return community;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Adds a platform user to the community.</summary>
    /// <param name="userId">The user to add.</param>
    /// <param name="role">The role to assign.</param>
    public void AddMember(UserId userId, CommunityMemberRole role)
    {
        if (_members.Any(member => member.Id == userId))
        {
            throw new UnknownDomainException("User is already a member of this community.");
        }

        _members.Add(CommunityMember.Create(userId, role));
        RaiseDomainEvent(new CommunityMemberAdded(Id, userId, role.ToString()));
    }

    /// <summary>Changes the role of an existing member.</summary>
    /// <param name="userId">The member identifier.</param>
    /// <param name="role">The replacement role.</param>
    public void ChangeMemberRole(UserId userId, CommunityMemberRole role)
    {
        var member = _members.SingleOrDefault(candidate => candidate.Id == userId)
            ?? throw new UnknownDomainException("User is not a member of this community.");
        member.ChangeRole(role);
    }
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures the Discord guild identifier is numeric.</summary>
    /// <param name="discordGuildId">The Discord guild identifier.</param>
    /// <exception cref="DomainException">Thrown when the identifier is missing or non-numeric.</exception>
    private static void EnsureDiscordGuildId(string discordGuildId)
    {
        if (string.IsNullOrWhiteSpace(discordGuildId) || !discordGuildId.All(char.IsDigit))
        {
            throw new UnknownDomainException("Discord guild identifier must be a numeric snowflake.");
        }
    }

    /// <summary>Ensures the community has a visible name.</summary>
    /// <param name="name">The community name.</param>
    /// <exception cref="DomainException">Thrown when the name is missing.</exception>
    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new UnknownDomainException("Community name is required.");
        }
    }
    #endregion Invariants
}
