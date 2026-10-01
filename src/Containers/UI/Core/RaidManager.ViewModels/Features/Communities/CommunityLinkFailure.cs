namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Names why adding RaidManager to a Discord server didn't finish.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Overview explain the outcome in words instead of failing silently (task #288, criterion 2).
/// </remarks>
public enum CommunityLinkFailure
{
    /// <summary>The user cancelled on Discord's page.</summary>
    Cancelled,

    /// <summary>Discord's return couldn't be checked or exchanged, or Discord was unavailable.</summary>
    Failed,

    /// <summary>The return took longer than RaidManager waits, or came from another browser session.</summary>
    Expired,

    /// <summary>The Discord account that added the bot isn't the one signed in to RaidManager.</summary>
    OtherAccount,
}
