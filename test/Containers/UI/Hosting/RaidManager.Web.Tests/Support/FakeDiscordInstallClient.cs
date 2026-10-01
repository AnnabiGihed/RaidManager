using RaidManager.Web.Features.Communities;

namespace RaidManager.Web.Tests.Support;

/// <summary>Stands in for Discord's exchange when the bot is added, in website tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets tests choose what Discord confirms, or that it refuses, and see the return address it was asked for.
/// </remarks>
public sealed class FakeDiscordInstallClient : IDiscordInstallClient
{
    #region Constants
    /// <summary>Defines the server the fake confirms by default.</summary>
    public const string GuildId = "987654321098765432";

    /// <summary>Defines the server name the fake confirms by default.</summary>
    public const string GuildName = "Dark Templars";
    #endregion Constants

    #region Properties
    /// <summary>Gets or sets what Discord confirms; <see langword="null"/> makes the exchange fail.</summary>
    public DiscordInstall? Install { get; set; } = new(GuildId, GuildName, DiscordBackchannelStub.DiscordUserId);

    /// <summary>Gets the codes and return addresses the website exchanged.</summary>
    public List<(string Code, Uri RedirectUri)> Exchanges { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<DiscordInstall?> ExchangeAsync(string code, Uri redirectUri, CancellationToken cancellationToken)
    {
        Exchanges.Add((code, redirectUri));
        return Task.FromResult(Install);
    }
    #endregion Public Methods
}
