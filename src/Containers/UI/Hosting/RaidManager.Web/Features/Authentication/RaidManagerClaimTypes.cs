namespace RaidManager.Web.Features.Authentication;

/// <summary>Names the claims the website keeps in a signed-in player's session cookie.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The session holds the local user id and what the layout shows; Discord tokens are never kept (ADR-0011).
/// </remarks>
public static class RaidManagerClaimTypes
{
    #region Constants
    /// <summary>Defines the claim holding the local RaidManager user id.</summary>
    public const string UserId = "raidmanager:user_id";

    /// <summary>Defines the claim holding the player's Discord avatar URL.</summary>
    public const string AvatarUrl = "raidmanager:avatar_url";

    /// <summary>Defines the claim holding a community the player's Discord servers matched at sign-in, one per community.</summary>
    public const string MemberCommunityId = "raidmanager:member_community_id";

    /// <summary>Defines the claim the Discord handler fills with the player's chosen display name.</summary>
    public const string DiscordGlobalName = "urn:discord:user:global_name";

    /// <summary>Defines the claim the Discord handler fills with the avatar hash.</summary>
    public const string DiscordAvatarHash = "urn:discord:avatar:hash";
    #endregion Constants
}
