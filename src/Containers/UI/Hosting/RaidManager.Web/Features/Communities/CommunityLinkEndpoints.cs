using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Communities;

/// <summary>Maps the two endpoints around Discord's page for adding the bot: the way out and the way back.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Adds RaidManager to a Discord server with Discord's bot authorization plus an authorization code grant, so
/// RaidManager learns from Discord itself which server it was added to and by whom (story #14, task #288).
/// </remarks>
public static partial class CommunityLinkEndpoints
{
    #region Constants
    /// <summary>Defines the cookie that remembers the OAuth state between the way out and the way back.</summary>
    public const string StateCookieName = "__Host-RaidManager.CommunityLink";

    /// <summary>Defines Discord's page for adding a bot.</summary>
    private const string DiscordAuthorizeUrl = "https://discord.com/oauth2/authorize";

    /// <summary>Defines Discord's error for a user who cancelled.</summary>
    private const string AccessDenied = "access_denied";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the endpoints; both require a signed-in user.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCommunityLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(CommunityRoutes.AddBot, Start).RequireAuthorization();
        endpoints.MapGet(CommunityRoutes.DiscordReturn, ReturnAsync).RequireAuthorization();
        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the user to Discord to pick a server and add the bot, remembering the state for the way back.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="protector">The link protector.</param>
    /// <param name="credentials">The Discord application's credentials.</param>
    /// <returns>A redirect to Discord.</returns>
    private static IResult Start(HttpContext context, CommunityLinkProtector protector, DiscordApplicationCredentials credentials)
    {
        var (state, cookieValue) = protector.CreateState(UserIdOf(context.User));
        context.Response.Cookies.Append(StateCookieName, cookieValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,

            // Lax lets the cookie accompany Discord's top-level redirect back to the website.
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(10),
        });

        // "identify" next to "bot" makes Discord finish with an authorization code, which tells RaidManager the server.
        var url = QueryHelpers.AddQueryString(DiscordAuthorizeUrl, new Dictionary<string, string?>
        {
            ["client_id"] = credentials.ClientId,
            ["scope"] = "bot identify",
            ["permissions"] = "0",
            ["response_type"] = "code",
            ["redirect_uri"] = ReturnUri(context.Request).AbsoluteUri,
            ["state"] = state,
            ["integration_type"] = "0",
        });
        return Results.Redirect(url);
    }

    /// <summary>Checks Discord's return, learns the server from Discord, and continues to the realm or the existing community.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="code">The authorization code.</param>
    /// <param name="state">The returned OAuth state.</param>
    /// <param name="error">Discord's error, when the user cancelled or Discord refused.</param>
    /// <param name="guildId">The server Discord names in the return address.</param>
    /// <param name="services">The services the return needs.</param>
    /// <returns>A redirect to the realm choice, the already-linked page, or the Overview with the reason it stopped.</returns>
    private static async Task<IResult> ReturnAsync(
        HttpContext context,
        string? code,
        string? state,
        string? error,
        [FromQuery(Name = "guild_id")] string? guildId,
        [AsParameters] ReturnServices services)
    {
        if (services.Logger.IsEnabled(LogLevel.Information))
        {
            var parameters = string.Join(", ", context.Request.Query.Keys);
            LogReturn(services.Logger, parameters);
        }

        var userId = UserIdOf(context.User);
        var stateIsExpected = services.Protector.IsExpectedState(context.Request.Cookies[StateCookieName], state, userId);
        context.Response.Cookies.Delete(StateCookieName, new CookieOptions { Secure = true, Path = "/" });
        if (!stateIsExpected)
        {
            return Stop(CommunityLinkFailure.Expired);
        }

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return Stop(error == AccessDenied ? CommunityLinkFailure.Cancelled : CommunityLinkFailure.Failed);
        }

        var install = await services.Discord.ExchangeAsync(code, ReturnUri(context.Request), context.RequestAborted);
        if (install is null || (!string.IsNullOrEmpty(guildId) && guildId != install.GuildId))
        {
            return Stop(CommunityLinkFailure.Failed);
        }

        if (install.InstallerDiscordUserId != context.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Stop(CommunityLinkFailure.OtherAccount);
        }

        try
        {
            var existing = await services.Api.FindByDiscordServerAsync(install.GuildId, context.RequestAborted);
            return existing is not null
                ? Results.LocalRedirect(CommunityRoutes.AlreadyLinkedFor(existing.CommunityId))
                : Results.LocalRedirect(CommunityRoutes.ChooseRealmFor(
                    services.Protector.Protect(new PendingCommunityLink(install.GuildId, install.GuildName, userId))));
        }
        catch (HttpRequestException exception)
        {
            LogApiFailed(services.Logger, exception);
            return Stop(CommunityLinkFailure.Failed);
        }
    }

    /// <summary>Builds the return address registered in the Discord application, on the current host.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>The absolute return address.</returns>
    private static Uri ReturnUri(HttpRequest request) => new($"{request.Scheme}://{request.Host}{CommunityRoutes.DiscordReturn}");

    /// <summary>Reads the RaidManager user id from the session.</summary>
    /// <param name="user">The signed-in user.</param>
    /// <returns>The user id.</returns>
    private static Guid UserIdOf(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(RaidManagerClaimTypes.UserId) ?? throw new InvalidOperationException("The session has no RaidManager user id."));

    /// <summary>Returns to the Overview, which explains why adding the bot stopped.</summary>
    /// <param name="failure">The reason.</param>
    /// <returns>The redirect.</returns>
    private static IResult Stop(CommunityLinkFailure failure) => Results.LocalRedirect(CommunityRoutes.OverviewAfter(failure));

    /// <summary>Logs the names, never the values, of the parameters Discord returned with.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="parameters">The parameter names.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Discord returned from adding the bot with the parameters {Parameters}.")]
    private static partial void LogReturn(ILogger logger, string parameters);

    /// <summary>Logs that the API couldn't say whether the server is linked.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "The API couldn't be asked whether the server is already linked.")]
    private static partial void LogApiFailed(ILogger logger, Exception exception);
    #endregion Private Helpers
}
