namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Describes a mapped Discord role, which may have been deleted in Discord since.</summary>
/// <param name="DiscordRoleId">The Discord role snowflake.</param>
/// <param name="Name">The role's current name, or <see langword="null"/> when it no longer exists.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the roles card show a deleted Discord role as missing, so the Administrator can remove its mapping.
/// </remarks>
public sealed record MappedDiscordRoleResponse(string DiscordRoleId, string? Name)
{
    #region Properties
    /// <summary>Gets a value indicating whether the role no longer exists in Discord.</summary>
    public bool Missing => Name is null;
    #endregion Properties
}
