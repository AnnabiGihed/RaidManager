namespace RaidManager.Web.Features.Communities;

/// <summary>Finishes adding the bot to a Discord server by asking Discord which server and which account it was.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps Discord's OAuth exchange behind one contract, so the return endpoint can be tested without Discord.
/// </remarks>
public interface IDiscordInstallClient
{
    #region Methods
    /// <summary>Exchanges the code Discord returned for the server and the account that added the bot.</summary>
    /// <param name="code">The authorization code from Discord's return.</param>
    /// <param name="redirectUri">The return address the code was issued for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The confirmed install, or <see langword="null"/> when Discord refused or couldn't answer.</returns>
    Task<DiscordInstall?> ExchangeAsync(string code, Uri redirectUri, CancellationToken cancellationToken);
    #endregion Methods
}
