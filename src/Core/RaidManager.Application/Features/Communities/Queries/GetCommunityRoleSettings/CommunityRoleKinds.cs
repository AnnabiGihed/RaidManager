namespace RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;

/// <summary>Names the kinds of rows on the roles card.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Administrator and Member are fixed rows; every other row is one of the community's roles, with an id.
/// </remarks>
public static class CommunityRoleKinds
{
    #region Constants
    /// <summary>Defines the Administrator row: who added the bot, with every permission.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Defines a row for one of the community's roles.</summary>
    public const string Role = "Role";

    /// <summary>Defines the Member row: everyone in the server whose Discord roles give no role.</summary>
    public const string Member = "Member";
    #endregion Constants
}
