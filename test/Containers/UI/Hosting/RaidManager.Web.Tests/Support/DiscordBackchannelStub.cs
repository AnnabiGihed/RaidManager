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

    /// <summary>Defines a linked server the stub lists by default; the fake API's communities use it.</summary>
    public const string LinkedServerId = "123456789012345678";

    /// <summary>Defines a server the stub lists by default that no community links to.</summary>
    public const string OtherServerId = "223456789012345678";

    /// <summary>Defines the end of Discord's code exchange path.</summary>
    private const string TokenPath = "/oauth2/token";

    /// <summary>Defines the end of Discord's profile path.</summary>
    private const string ProfilePath = "/users/@me";
    #endregion Constants

    #region Properties
    /// <summary>Gets or sets the servers Discord lists for the user, as Discord's JSON answer.</summary>
    public string ServersJson { get; set; } =
        $$"""[{"id":"{{LinkedServerId}}","name":"Dark Templars","icon":null,"banner":null,"owner":false,"permissions":"0","features":[]},{"id":"{{OtherServerId}}","name":"Other","icon":null,"banner":null,"owner":false,"permissions":"0","features":[]}]""";

    /// <summary>Gets or sets the status Discord answers the server list with.</summary>
    public HttpStatusCode ServersStatus { get; set; } = HttpStatusCode.OK;

    /// <summary>Gets or sets the status Discord answers the code exchange with.</summary>
    public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;

    /// <summary>Gets or sets the status Discord answers the profile request with.</summary>
    public HttpStatusCode ProfileStatus { get; set; } = HttpStatusCode.OK;

    /// <summary>Gets the authorization header of the last server list request.</summary>
    public string? ServersAuthorization { get; private set; }
    #endregion Properties

    #region Overrides
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (path.EndsWith("/users/@me/guilds", StringComparison.Ordinal))
        {
            ServersAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(ServersStatus) { Content = new StringContent(ServersJson, Encoding.UTF8, "application/json") });
        }

        if (path.EndsWith(TokenPath, StringComparison.Ordinal) && TokenStatus != HttpStatusCode.OK)
        {
            return Task.FromResult(Refusal(TokenStatus, """{"error":"invalid_grant"}"""));
        }

        if (path.EndsWith(ProfilePath, StringComparison.Ordinal) && ProfileStatus != HttpStatusCode.OK)
        {
            return Task.FromResult(Refusal(ProfileStatus, """{"message":"401: Unauthorized","code":0}"""));
        }

        var json = path.EndsWith(TokenPath, StringComparison.Ordinal)
            ? """{"access_token":"test-access-token","token_type":"Bearer","expires_in":604800,"scope":"identify guilds"}"""
            : path.EndsWith(ProfilePath, StringComparison.Ordinal)
                ? $$"""{"id":"{{DiscordUserId}}","username":"arthas","global_name":"{{GlobalName}}","avatar":"{{AvatarHash}}","discriminator":"0"}"""
                : null;
        return Task.FromResult(json is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Builds Discord's answer to an expired or revoked authorization.</summary>
    /// <param name="status">The status Discord answers with.</param>
    /// <param name="json">Discord's error body.</param>
    /// <returns>The answer.</returns>
    private static HttpResponseMessage Refusal(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    #endregion Private Helpers
}
