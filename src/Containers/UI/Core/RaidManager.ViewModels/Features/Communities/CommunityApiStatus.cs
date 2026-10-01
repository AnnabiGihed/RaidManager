namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Names how the API answered a community roles call.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Turns the API's statuses into what the roles card says: done, refused, Discord unavailable, or the bot removed.
/// </remarks>
public enum CommunityApiStatus
{
    /// <summary>The call succeeded.</summary>
    Succeeded,

    /// <summary>The user may not do this: not in the server, or not the Administrator (403), or the role can't be mapped (400).</summary>
    Refused,

    /// <summary>Discord couldn't answer (503).</summary>
    DiscordUnavailable,

    /// <summary>The RaidManager bot is no longer in the server (409).</summary>
    BotRemoved,
}
