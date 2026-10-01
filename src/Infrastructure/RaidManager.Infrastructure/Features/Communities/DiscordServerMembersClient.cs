using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Infrastructure.Features.Communities;

/// <summary>Asks Discord's REST API, with the bot token, for a user's membership and roles in a server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Turns Discord's answers into a membership or a failure. Only Discord's "unknown member, user or server"
/// answers mean "not a member"; anything else unexpected is treated as Discord being unavailable, so the check fails
/// closed (ADR-0022).
/// </remarks>
internal sealed partial class DiscordServerMembersClient : IDiscordServerMembers
{
    #region Constants
    /// <summary>Defines Discord's error code for a server the bot isn't in.</summary>
    private const int UnknownGuild = 10004;

    /// <summary>Defines Discord's error code for a user who isn't in the server.</summary>
    private const int UnknownMember = 10007;

    /// <summary>Defines Discord's error code for a user Discord doesn't know.</summary>
    private const int UnknownUser = 10013;
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client configured with Discord's address and the bot token.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<DiscordServerMembersClient> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordServerMembersClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client configured with Discord's address and the bot token.</param>
    /// <param name="logger">The logger.</param>
    public DiscordServerMembersClient(HttpClient httpClient, ILogger<DiscordServerMembersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<Result<DiscordMembership>> FindAsync(string discordGuildId, string discordUserId, CancellationToken cancellationToken)
    {
        var path = $"guilds/{Uri.EscapeDataString(discordGuildId)}/members/{Uri.EscapeDataString(discordUserId)}";
        try
        {
            using var response = await _httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var member = await response.Content.ReadFromJsonAsync<GuildMemberResponse>(cancellationToken);
                return Result.Success(DiscordMembership.Member(member?.Roles ?? []));
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadFromJsonAsync<DiscordErrorResponse>(cancellationToken);
                if (error?.Code is UnknownMember or UnknownUser)
                {
                    return Result.Success(DiscordMembership.NotMember);
                }

                if (error?.Code is UnknownGuild)
                {
                    LogBotNotInServer(_logger, discordGuildId);
                    return Result.Success(DiscordMembership.NotMember);
                }
            }

            LogDiscordRefused(_logger, (int)response.StatusCode, discordGuildId);
            return Result.Failure<DiscordMembership>(DiscordErrors.Unavailable);
        }
        catch (Exception exception) when (IsDiscordFailure(exception, cancellationToken))
        {
            LogDiscordUnreachable(_logger, exception, discordGuildId);
            return Result.Failure<DiscordMembership>(DiscordErrors.Unavailable);
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Tells whether an exception means Discord couldn't answer, rather than the caller cancelling.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    /// <returns><see langword="true"/> for a network failure, a timeout or an unreadable answer.</returns>
    private static bool IsDiscordFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>Logs that the bot is no longer in a linked server.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The RaidManager bot isn't in Discord server {DiscordGuildId}; nobody gets a role there.")]
    private static partial void LogBotNotInServer(ILogger logger, string discordGuildId);

    /// <summary>Logs an unexpected Discord status.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="statusCode">The HTTP status Discord returned.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Discord answered {StatusCode} for a member of server {DiscordGuildId}; the role check fails closed.")]
    private static partial void LogDiscordRefused(ILogger logger, int statusCode, string discordGuildId);

    /// <summary>Logs that Discord couldn't be reached or answered unreadably.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Discord couldn't be asked about a member of server {DiscordGuildId}; the role check fails closed.")]
    private static partial void LogDiscordUnreachable(ILogger logger, Exception exception, string discordGuildId);
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Reads the part of Discord's guild member object that role checks need.</summary>
    /// <param name="Roles">The member's role snowflakes.</param>
    private sealed record GuildMemberResponse([property: JsonPropertyName("roles")] IReadOnlyList<string>? Roles);

    /// <summary>Reads Discord's error object.</summary>
    /// <param name="Code">Discord's JSON error code.</param>
    private sealed record DiscordErrorResponse([property: JsonPropertyName("code")] int Code);
    #endregion Nested Types
}
