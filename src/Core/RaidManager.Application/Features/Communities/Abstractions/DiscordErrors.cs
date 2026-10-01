using Pivot.Framework.Domain.Shared;

namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Defines the failures of asking Discord about a server's members.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives a Discord outage a stable code, so a role check fails closed and says why (ADR-0022).
/// </remarks>
public static class DiscordErrors
{
    #region Static Instances
    /// <summary>Gets the error returned when Discord couldn't be reached or refused the bot's request.</summary>
    public static readonly Error Unavailable = new("Discord.Unavailable", "Discord couldn't confirm your roles right now. Try again in a minute.");

    /// <summary>Gets the error returned when the RaidManager bot is no longer in the community's Discord server.</summary>
    public static readonly Error BotNotInServer = new("Discord.BotNotInServer", "The RaidManager bot isn't in this Discord server any more. Add it again.");
    #endregion Static Instances
}
