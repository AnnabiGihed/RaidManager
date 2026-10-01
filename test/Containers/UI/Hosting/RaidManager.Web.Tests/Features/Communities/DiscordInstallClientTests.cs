using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities;

/// <summary>Verifies the exchange with Discord when the bot is added.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The answers have the shape Discord sent on 2026-10-01 (#288): the token answer carries the server, and the user is read with the access token. Anything else links nothing.
/// </remarks>
public sealed class DiscordInstallClientTests : IDisposable
{
    #region Constants
    /// <summary>Defines the token answer path.</summary>
    private const string TokenPath = "/api/v10/oauth2/token";

    /// <summary>Defines the current user path.</summary>
    private const string MePath = "/api/v10/users/@me";

    /// <summary>Defines a token answer with the fields Discord returned when the bot was added.</summary>
    private const string TokenAnswer = """{"token_type":"Bearer","access_token":"user-token","expires_in":604800,"refresh_token":"refresh","scope":"bot identify","guild":{"id":"987654321098765432","name":"Dark Templars","owner_id":"1"}}""";
    #endregion Constants

    #region Fields
    /// <summary>Stores the handler standing in for Discord.</summary>
    private readonly RecordingHandler _discord = new();

    /// <summary>Stores the return address the code was issued for.</summary>
    private readonly Uri _returnUri = new("https://localhost:55365/communities/link/discord");
    #endregion Fields

    #region Tests
    /// <summary>Exchanges the code with the application's credentials and reads the server and the installer.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CodeBecomesTheConfirmedServerAndInstaller()
    {
        _discord.Answer(TokenPath, HttpStatusCode.OK, TokenAnswer).Answer(MePath, HttpStatusCode.OK, """{"id":"80351110224678912","username":"anguish"}""");

        var install = await ExchangeAsync();

        install.ShouldBe(new DiscordInstall("987654321098765432", "Dark Templars", "80351110224678912"));
        var (token, body) = _discord.Requests[0];
        token.Method.ShouldBe(HttpMethod.Post);
        token.Headers.Authorization!.Scheme.ShouldBe("Basic");
        Encoding.UTF8.GetString(Convert.FromBase64String(token.Headers.Authorization.Parameter!)).ShouldBe("client-id:client-secret");
        body.ShouldBe("grant_type=authorization_code&code=discord-code&redirect_uri=https%3A%2F%2Flocalhost%3A55365%2Fcommunities%2Flink%2Fdiscord");
        var me = _discord.Requests[1].Request;
        me.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        me.Headers.Authorization.Parameter.ShouldBe("user-token");
    }

    /// <summary>Links nothing when Discord refuses, answers without a server, or can't be asked about the user.</summary>
    /// <param name="variant">What goes wrong.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("code refused")]
    [InlineData("no server")]
    [InlineData("user refused")]
    [InlineData("user without id")]
    [InlineData("unreadable answer")]
    [InlineData("unreachable")]
    public async Task AnyUnexpectedAnswerLinksNothing(string variant)
    {
        _ = variant switch
        {
            "code refused" => _discord.Answer(TokenPath, HttpStatusCode.BadRequest, """{"error":"invalid_grant"}"""),
            "no server" => _discord.Answer(TokenPath, HttpStatusCode.OK, """{"access_token":"user-token","scope":"identify"}"""),
            "user refused" => _discord.Answer(TokenPath, HttpStatusCode.OK, TokenAnswer).Answer(MePath, HttpStatusCode.Unauthorized),
            "user without id" => _discord.Answer(TokenPath, HttpStatusCode.OK, TokenAnswer).Answer(MePath, HttpStatusCode.OK, "{}"),
            "unreadable answer" => _discord.Answer(TokenPath, HttpStatusCode.OK, "not json"),
            _ => _discord,
        };
        if (variant == "unreachable")
        {
            _discord.Failure = new HttpRequestException("No route to Discord.");
        }

        (await ExchangeAsync()).ShouldBeNull();
    }
    #endregion Tests

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _discord.Dispose();
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Exchanges the test code through the stub.</summary>
    /// <returns>The confirmed install, or <see langword="null"/>.</returns>
    private async Task<DiscordInstall?> ExchangeAsync()
    {
        using var httpClient = new HttpClient(_discord, disposeHandler: false) { BaseAddress = new Uri("https://discord.com/api/v10/") };
        var client = new DiscordInstallClient(httpClient, new DiscordApplicationCredentials("client-id", "client-secret"), NullLogger<DiscordInstallClient>.Instance);
        return await client.ExchangeAsync("discord-code", _returnUri, CancellationToken.None);
    }
    #endregion Private Helpers
}
