using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Shared.Discord;

namespace RaidManager.Domain.Features.Communities.ValueObjects;

/// <summary>Represents a Discord role that gives its members an Officer or Raid leader role in RaidManager.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Makes the community's permission source explicit: roles follow Discord, so RaidManager stores only which
/// Discord role means what. Administrator comes from adding the bot and Member from being in the server, so neither is mapped.
/// </remarks>
public sealed record DiscordRoleMapping
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordRoleMapping"/> class.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The RaidManager role it gives.</param>
    private DiscordRoleMapping(string discordRoleId, CommunityMemberRole role)
    {
        DiscordRoleId = discordRoleId;
        Role = role;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Discord role snowflake.</summary>
    public string DiscordRoleId { get; private init; }

    /// <summary>Gets the RaidManager role the Discord role gives: Officer or Raid leader.</summary>
    public CommunityMemberRole Role { get; private init; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a validated mapping.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The RaidManager role it gives: <see cref="CommunityMemberRole.Officer"/> or <see cref="CommunityMemberRole.RaidLeader"/>.</param>
    /// <returns>The mapping.</returns>
    /// <exception cref="DomainException">Thrown when the role id is not a snowflake or the role is not Officer or Raid leader.</exception>
    public static DiscordRoleMapping Create(string discordRoleId, CommunityMemberRole role)
    {
        if (role is not (CommunityMemberRole.Officer or CommunityMemberRole.RaidLeader))
        {
            throw new UnknownDomainException("A Discord role can only be mapped to Officer or Raid leader.");
        }

        return new DiscordRoleMapping(DiscordSnowflake.Ensure(discordRoleId, "Discord role"), role);
    }
    #endregion Factory Methods
}
