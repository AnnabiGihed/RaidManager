using System.Net;
using System.Text;

namespace RaidManager.Web.Tests.Support;

/// <summary>Answers the Discord token and user requests the OAuth handler makes during a callback.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets the tests run the complete sign-in callback without contacting Discord.
/// </remarks>
public sealed class DiscordBackchannelStub : HttpMessageHandler
{
    #region Constants
    /// <summary>Defines the Discord user id the stub returns.</summary>
    public const string DiscordUserId = "80351110224678912";

    /// <summary>Defines the Discord display name the stub returns.</summary>
    public const string GlobalName = "Arthas Menethil";

    /// <summary>Defines the avatar hash the stub returns.</summary>
    public const string AvatarHash = "a1b2c3";
    #endregion Constants

    #region Overrides
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var json = path.EndsWith("/oauth2/token", StringComparison.Ordinal)
            ? """{"access_token":"test-access-token","token_type":"Bearer","expires_in":604800,"scope":"identify"}"""
            : path.EndsWith("/users/@me", StringComparison.Ordinal)
                ? $$"""{"id":"{{DiscordUserId}}","username":"arthas","global_name":"{{GlobalName}}","avatar":"{{AvatarHash}}","discriminator":"0"}"""
                : null;
        return Task.FromResult(json is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
    #endregion Overrides
}
