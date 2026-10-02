using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Infrastructure.Features.Communities;

/// <summary>Reads a Discord server's name, roles and members from Discord's REST API with the bot token.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Turns Discord's answers into the shapes the community settings need, as Discord sent them on 2026-10-01: @everyone has the server's id, roles of other bots are marked managed, members carry user.bot, and the member list pages with after. Anything unexpected fails closed.
/// </remarks>
internal sealed partial class DiscordServersClient : IDiscordServers
{
    #region Constants
    /// <summary>Defines Discord's error code for a server the bot isn't in.</summary>
    private const int UnknownGuild = 10004;

    /// <summary>Defines where Discord serves account pictures (checked on 2026-10-01).</summary>
    private const string AvatarBaseUrl = "https://cdn.discordapp.com/avatars";

    /// <summary>Defines the most members Discord returns in one page.</summary>
    private const int PageSize = 1000;
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client configured with Discord's address and the bot token.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<DiscordServersClient> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordServersClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client configured with Discord's address and the bot token.</param>
    /// <param name="logger">The logger.</param>
    public DiscordServersClient(HttpClient httpClient, ILogger<DiscordServersClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<Result<DiscordServer>> GetAsync(string discordGuildId, CancellationToken cancellationToken)
    {
        var guild = await ReadAsync<GuildAnswer>($"guilds/{Uri.EscapeDataString(discordGuildId)}", discordGuildId, cancellationToken);
        if (guild.IsFailure)
        {
            return Result.Failure<DiscordServer>(guild.Error);
        }

        var roles = await ReadAsync<List<RoleAnswer>>($"guilds/{Uri.EscapeDataString(discordGuildId)}/roles", discordGuildId, cancellationToken);
        if (roles.IsFailure)
        {
            return Result.Failure<DiscordServer>(roles.Error);
        }

        // @everyone has the server's own id; managed roles belong to integrations such as other bots.
        var mappable = roles.Value
            .Where(role => role.Id != discordGuildId && !role.Managed)
            .OrderByDescending(role => role.Position)
            .Select(role => new DiscordServerRole(role.Id, role.Name, role.Position))
            .ToList();
        return Result.Success(new DiscordServer(guild.Value.Name, mappable));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscordServerMember>>> ListMembersAsync(string discordGuildId, CancellationToken cancellationToken)
    {
        var members = new List<DiscordServerMember>();
        var after = "0";
        while (true)
        {
            var page = await ReadAsync<List<MemberAnswer>>(
                $"guilds/{Uri.EscapeDataString(discordGuildId)}/members?limit={PageSize}&after={after}",
                discordGuildId,
                cancellationToken);
            if (page.IsFailure)
            {
                return Result.Failure<IReadOnlyList<DiscordServerMember>>(page.Error);
            }

            members.AddRange(page.Value
                .Where(member => member.User.Bot != true)
                .Select(member => new DiscordServerMember(
                    member.User.Id,
                    member.Nick ?? member.User.GlobalName ?? member.User.Username,
                    member.Roles,
                    member.User.Avatar is null ? null : $"{AvatarBaseUrl}/{member.User.Id}/{member.User.Avatar}.png")));
            if (page.Value.Count < PageSize)
            {
                return Result.Success<IReadOnlyList<DiscordServerMember>>(members);
            }

            after = page.Value.Max(member => ulong.Parse(member.User.Id, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Logs that the bot is no longer in a linked server.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "The RaidManager bot isn't in Discord server {DiscordGuildId}.")]
    private static partial void LogBotNotInServer(ILogger logger, string discordGuildId);

    /// <summary>Logs an unexpected Discord status.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="statusCode">The HTTP status Discord returned.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Discord answered {StatusCode} about server {DiscordGuildId}.")]
    private static partial void LogDiscordRefused(ILogger logger, int statusCode, string discordGuildId);

    /// <summary>Logs that Discord couldn't be reached or answered unreadably.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Discord couldn't be asked about server {DiscordGuildId}.")]
    private static partial void LogDiscordUnreachable(ILogger logger, Exception exception, string discordGuildId);

    /// <summary>Reads one Discord answer, turning every unexpected answer into a failure.</summary>
    /// <typeparam name="T">The answer's shape.</typeparam>
    /// <param name="path">The path under Discord's API address.</param>
    /// <param name="discordGuildId">The server, for the logs.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The answer, or a failure.</returns>
    private async Task<Result<T>> ReadAsync<T>(string path, string discordGuildId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var answer = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return answer is null ? Result.Failure<T>(DiscordErrors.Unavailable) : Result.Success(answer);
            }

            if (response.StatusCode == HttpStatusCode.NotFound
                && (await response.Content.ReadFromJsonAsync<ErrorAnswer>(cancellationToken))?.Code == UnknownGuild)
            {
                LogBotNotInServer(_logger, discordGuildId);
                return Result.Failure<T>(DiscordErrors.BotNotInServer);
            }

            LogDiscordRefused(_logger, (int)response.StatusCode, discordGuildId);
            return Result.Failure<T>(DiscordErrors.Unavailable);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogDiscordUnreachable(_logger, exception, discordGuildId);
            return Result.Failure<T>(DiscordErrors.Unavailable);
        }
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Reads the server's name.</summary>
    /// <param name="Name">The server name.</param>
    private sealed record GuildAnswer([property: JsonPropertyName("name")] string Name);

    /// <summary>Reads a role.</summary>
    /// <param name="Id">The role snowflake.</param>
    /// <param name="Name">The role name.</param>
    /// <param name="Position">The role's position.</param>
    /// <param name="Managed">Whether an integration manages the role.</param>
    private sealed record RoleAnswer(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("position")] int Position,
        [property: JsonPropertyName("managed")] bool Managed);

    /// <summary>Reads a member.</summary>
    /// <param name="User">The member's account.</param>
    /// <param name="Nick">The server nickname, if any.</param>
    /// <param name="Roles">The member's role snowflakes.</param>
    private sealed record MemberAnswer(
        [property: JsonPropertyName("user")] UserAnswer User,
        [property: JsonPropertyName("nick")] string? Nick,
        [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles);

    /// <summary>Reads a member's account.</summary>
    /// <param name="Id">The user snowflake.</param>
    /// <param name="Username">The username.</param>
    /// <param name="GlobalName">The global display name, if any.</param>
    /// <param name="Bot">Whether the account is a bot.</param>
    /// <param name="Avatar">The account picture's hash, if any.</param>
    private sealed record UserAnswer(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("global_name")] string? GlobalName,
        [property: JsonPropertyName("bot")] bool? Bot,
        [property: JsonPropertyName("avatar")] string? Avatar);

    /// <summary>Reads Discord's error object.</summary>
    /// <param name="Code">Discord's JSON error code.</param>
    private sealed record ErrorAnswer([property: JsonPropertyName("code")] int Code);
    #endregion Nested Types
}
