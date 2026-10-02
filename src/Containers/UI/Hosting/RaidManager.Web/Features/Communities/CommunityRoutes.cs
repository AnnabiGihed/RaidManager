using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.Web.Features.Communities;

/// <summary>Names the routes of adding RaidManager to a Discord server and of the community page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the install flow's addresses in one place. <see cref="DiscordReturn"/> is registered in the Discord
/// application as a redirect, so it must not change without updating the portal (docs/how-to/set-up-discord.md).
/// </remarks>
public static class CommunityRoutes
{
    #region Constants
    /// <summary>Defines the route that sends the user to Discord to add the bot.</summary>
    public const string AddBot = "/communities/link";

    /// <summary>Defines the route Discord returns to after the bot was added, registered in the Discord application.</summary>
    public const string DiscordReturn = "/communities/link/discord";

    /// <summary>Defines the realm choice page (board 2).</summary>
    public const string ChooseRealm = "/communities/link/realm";

    /// <summary>Defines the page for a server that is already linked (board 3).</summary>
    public const string AlreadyLinked = "/communities/link/already-linked";

    /// <summary>Defines the community page (board 4).</summary>
    public const string Settings = "/community";

    /// <summary>Defines the members page (board 5).</summary>
    public const string Members = "/community/members";

    /// <summary>Defines the query parameter that carries the protected pending link to the realm choice.</summary>
    public const string LinkParameter = "link";

    /// <summary>Defines the query parameter that names the community on the already-linked page.</summary>
    public const string CommunityParameter = "community";

    /// <summary>Defines the query parameter that tells the community page the server was just linked.</summary>
    public const string LinkedParameter = "linked";

    /// <summary>Defines the query parameter that tells the Overview why adding the bot didn't finish.</summary>
    public const string FailureParameter = "link";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds the realm choice address for a protected pending link.</summary>
    /// <param name="protectedLink">The protected pending link.</param>
    /// <returns>The address.</returns>
    public static string ChooseRealmFor(string protectedLink) => $"{ChooseRealm}?{LinkParameter}={Uri.EscapeDataString(protectedLink)}";

    /// <summary>Builds the already-linked address for a community.</summary>
    /// <param name="communityId">The community the server links to.</param>
    /// <returns>The address.</returns>
    public static string AlreadyLinkedFor(Guid communityId) => $"{AlreadyLinked}?{CommunityParameter}={communityId}";

    /// <summary>Builds the community page address shown right after linking.</summary>
    /// <returns>The address.</returns>
    public static string SettingsAfterLinking() => $"{Settings}?{LinkedParameter}=true";

    /// <summary>Builds the Overview address that explains why adding the bot didn't finish.</summary>
    /// <param name="failure">The reason.</param>
    /// <returns>The address.</returns>
    public static string OverviewAfter(CommunityLinkFailure failure) =>
        $"/?{FailureParameter}={failure.ToString().ToLowerInvariant()}";
    #endregion Public Methods
}
