using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidManager.Web.Features.Communities;

/// <summary>Exchanges the code Discord returns after the bot was added for the server and the account that added it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Uses Discord's authorization code grant with the application's credentials, so the server comes from
/// Discord's own answer, never from the return address a visitor could edit. The user's token is used once, to ask who
/// they are, and not kept.
/// </remarks>
internal sealed partial class DiscordInstallClient : IDiscordInstallClient
{
    #region Fields
    /// <summary>Stores the HTTP client addressed at Discord's API.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Stores the Discord application's credentials.</summary>
    private readonly DiscordApplicationCredentials _credentials;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<DiscordInstallClient> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordInstallClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client addressed at Discord's API.</param>
    /// <param name="credentials">The Discord application's credentials.</param>
    /// <param name="logger">The logger.</param>
    public DiscordInstallClient(HttpClient httpClient, DiscordApplicationCredentials credentials, ILogger<DiscordInstallClient> logger)
    {
        _httpClient = httpClient;
        _credentials = credentials;
        _logger = logger;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<DiscordInstall?> ExchangeAsync(string code, Uri redirectUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        try
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("oauth2/token", UriKind.Relative))
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri.AbsoluteUri,
                }),
            };
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(_credentials.ClientId)}:{Uri.EscapeDataString(_credentials.ClientSecret)}")));
            using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                LogExchangeRefused(_logger, (int)tokenResponse.StatusCode);
                return null;
            }

            using var token = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var fields = string.Join(", ", token.RootElement.EnumerateObject().Select(property => property.Name));
                LogTokenFields(_logger, fields);
            }

            var guild = token.RootElement.TryGetProperty("guild", out var guildElement)
                ? guildElement.Deserialize<GuildAnswer>()
                : null;
            var accessToken = token.RootElement.TryGetProperty("access_token", out var accessElement) ? accessElement.GetString() : null;
            if (guild?.Id is null || guild.Name is null || accessToken is null)
            {
                LogIncompleteAnswer(_logger);
                return null;
            }

            using var meRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("users/@me", UriKind.Relative));
            meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var meResponse = await _httpClient.SendAsync(meRequest, cancellationToken);
            if (!meResponse.IsSuccessStatusCode)
            {
                LogExchangeRefused(_logger, (int)meResponse.StatusCode);
                return null;
            }

            var installer = await meResponse.Content.ReadFromJsonAsync<UserAnswer>(cancellationToken);
            return installer?.Id is null ? null : new DiscordInstall(guild.Id, guild.Name, installer.Id);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogDiscordUnreachable(_logger, exception);
            return null;
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Logs a refused exchange.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="statusCode">The HTTP status Discord returned.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Discord answered {StatusCode} while finishing adding the bot.")]
    private static partial void LogExchangeRefused(ILogger logger, int statusCode);

    /// <summary>Logs the names, never the values, of the fields in Discord's token answer.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fields">The field names.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Discord's answer to adding the bot had the fields {Fields}.")]
    private static partial void LogTokenFields(ILogger logger, string fields);

    /// <summary>Logs a token answer without a server or a token.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Discord's answer to adding the bot had no server or no access token.")]
    private static partial void LogIncompleteAnswer(ILogger logger);

    /// <summary>Logs that Discord couldn't be reached or answered unreadably.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Discord couldn't be asked to finish adding the bot.")]
    private static partial void LogDiscordUnreachable(ILogger logger, Exception exception);
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Reads the server in Discord's token answer.</summary>
    /// <param name="Id">The server snowflake.</param>
    /// <param name="Name">The server name.</param>
    private sealed record GuildAnswer([property: JsonPropertyName("id")] string? Id, [property: JsonPropertyName("name")] string? Name);

    /// <summary>Reads the Discord account that added the bot.</summary>
    /// <param name="Id">The user snowflake.</param>
    private sealed record UserAnswer([property: JsonPropertyName("id")] string? Id);
    #endregion Nested Types
}
