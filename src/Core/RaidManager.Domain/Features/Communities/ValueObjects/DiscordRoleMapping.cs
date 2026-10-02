using RaidManager.Domain.Features.Shared.Discord;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.ValueObjects;

/// <summary>Represents a Discord role that gives its members one of the community's RaidManager roles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Makes the community's permission source explicit: roles follow Discord, so RaidManager stores only which
/// Discord role gives which of its roles. Administrator comes from adding the bot and Member from being in the server,
/// so neither is mapped.
/// </remarks>
public sealed record DiscordRoleMapping
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordRoleMapping"/> class.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role it gives.</param>
    private DiscordRoleMapping(string discordRoleId, CommunityRoleId roleId)
    {
        DiscordRoleId = discordRoleId;
        RoleId = roleId;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Discord role snowflake.</summary>
    public string DiscordRoleId { get; private init; }

    /// <summary>Gets the community role the Discord role gives.</summary>
    public CommunityRoleId RoleId { get; private init; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a validated mapping.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="roleId">The community role it gives.</param>
    /// <returns>The mapping.</returns>
    /// <exception cref="DomainException">Thrown when the role id is not a snowflake.</exception>
    public static DiscordRoleMapping Create(string discordRoleId, CommunityRoleId roleId) =>
        new(DiscordSnowflake.Ensure(discordRoleId, "Discord role"), roleId);
    #endregion Factory Methods
}
