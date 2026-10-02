using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Characters;

namespace RaidManager.Web.Features.Authentication;

/// <summary>Turns a completed Discord authorization into a RaidManager session, or into a retry page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Resolves the Discord identity to its local user through the API before the cookie is issued, so a player is
/// only signed in when the API knows them. Any failure leads to the retry page instead of a half-signed-in state. A
/// player with characters awaiting their decision or an officer lands on the review page first (story #18). The
/// Discord servers the player is in are read once, with the sign-in token, to keep the linked communities they belong
/// to in the session (ADR-0023); the token itself is not kept.
/// </remarks>
internal sealed partial class DiscordSignInEvents : OAuthEvents
{
    #region Constants
    /// <summary>Defines the base URL of Discord avatar images.</summary>
    private const string AvatarBaseUrl = "https://cdn.discordapp.com/avatars";

    /// <summary>Defines Discord's list of the signed-in user's servers; 200 is both the page limit and the most servers an account can be in.</summary>
    private const string UserServersUrl = "https://discord.com/api/v10/users/@me/guilds?limit=200";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API client that resolves the local user.</summary>
    private readonly IIdentityApiClient _identityApi;

    /// <summary>Stores the API client that lists the player's character claims.</summary>
    private readonly ICharacterClaimsApiClient _claimsApi;

    /// <summary>Stores the API client that finds the communities of the player's servers.</summary>
    private readonly ICommunitiesApiClient _communitiesApi;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<DiscordSignInEvents> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordSignInEvents"/> class.</summary>
    /// <param name="identityApi">The API client that resolves the local user.</param>
    /// <param name="claimsApi">The API client that lists the player's character claims.</param>
    /// <param name="communitiesApi">The API client that finds the communities of the player's servers.</param>
    /// <param name="logger">The logger.</param>
    public DiscordSignInEvents(IIdentityApiClient identityApi, ICharacterClaimsApiClient claimsApi, ICommunitiesApiClient communitiesApi, ILogger<DiscordSignInEvents> logger)
    {
        _identityApi = identityApi;
        _claimsApi = claimsApi;
        _communitiesApi = communitiesApi;
        _logger = logger;
    }
    #endregion Constructors

    #region Overrides
    /// <inheritdoc />
    public override async Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        var identity = context.Identity ?? throw new InvalidOperationException("Discord returned no identity.");
        var discordUserId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Discord returned no user id.");
        var username = identity.FindFirst(ClaimTypes.Name)?.Value;
        var globalName = identity.FindFirst(RaidManagerClaimTypes.DiscordGlobalName)?.Value;
        var displayName = string.IsNullOrWhiteSpace(globalName) ? username ?? discordUserId : globalName;
        var avatarHash = identity.FindFirst(RaidManagerClaimTypes.DiscordAvatarHash)?.Value;
        var avatarUrl = string.IsNullOrWhiteSpace(avatarHash) ? null : $"{AvatarBaseUrl}/{discordUserId}/{avatarHash}.png";

        var userId = await _identityApi.ResolveDiscordSignInAsync(discordUserId, displayName, avatarUrl, context.HttpContext.RequestAborted);

        foreach (var nameClaim in identity.FindAll(ClaimTypes.Name).ToList())
        {
            identity.RemoveClaim(nameClaim);
        }

        identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
        identity.AddClaim(new Claim(RaidManagerClaimTypes.UserId, userId.ToString()));
        if (avatarUrl is not null)
        {
            identity.AddClaim(new Claim(RaidManagerClaimTypes.AvatarUrl, avatarUrl));
        }

        foreach (var communityId in await MemberCommunitiesAsync(context.Backchannel, context.AccessToken, context.HttpContext.RequestAborted))
        {
            identity.AddClaim(new Claim(RaidManagerClaimTypes.MemberCommunityId, communityId.ToString()));
        }

        context.Properties.RedirectUri = await RedirectAfterSignInAsync(userId, context.Properties.RedirectUri, context.HttpContext.RequestAborted);
    }

    /// <inheritdoc />
    public override Task AccessDenied(AccessDeniedContext context)
    {
        context.Response.Redirect(AuthenticationRoutes.SignInFailedFor("denied"));
        context.HandleResponse();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RemoteFailure(RemoteFailureContext context)
    {
        LogSignInFailed(_logger, context.Failure);
        context.Response.Redirect(AuthenticationRoutes.SignInFailedFor("failed"));
        context.HandleResponse();
        return Task.CompletedTask;
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Logs a pending-claims check that failed during sign-in; the review page shows the error.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="failure">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Checking the player's pending character claims failed during sign-in.")]
    private static partial void LogPendingCheckFailed(ILogger logger, Exception failure);

    /// <summary>Logs that Discord refused to list the player's servers; they see no community until the next sign-in.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="statusCode">The HTTP status Discord returned.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Discord answered {StatusCode} when listing the player's servers at sign-in.")]
    private static partial void LogServersRefused(ILogger logger, int statusCode);

    /// <summary>Logs that the player's communities couldn't be found; they see no community until the next sign-in.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="failure">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Finding the player's communities from their Discord servers failed at sign-in.")]
    private static partial void LogServersFailed(ILogger logger, Exception failure);

    /// <summary>Logs how many communities the player's servers matched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="communities">The number of matched communities.</param>
    /// <param name="servers">The number of servers Discord listed.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "The player's {Servers} Discord servers matched {Communities} communities at sign-in.")]
    private static partial void LogServersMatched(ILogger logger, int communities, int servers);

    /// <summary>Logs a sign-in that could not complete; the redirect sends the player to the retry page.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="failure">The failure, if the handler reported one.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Discord sign-in failed.")]
    private static partial void LogSignInFailed(ILogger logger, Exception? failure);

    /// <summary>Finds the linked communities of the Discord servers the player is in.</summary>
    /// <param name="discord">The sign-in handler's client for Discord.</param>
    /// <param name="accessToken">The sign-in token, used for this call only.</param>
    /// <param name="cancellationToken">A token to cancel the calls.</param>
    /// <returns>The communities; empty when there are none or a call failed, which never stops the sign-in.</returns>
    private async Task<IReadOnlyList<Guid>> MemberCommunitiesAsync(HttpClient discord, string? accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserServersUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await discord.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogServersRefused(_logger, (int)response.StatusCode);
                return [];
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            var servers = await JsonSerializer.DeserializeAsync<List<ServerAnswer>>(body, cancellationToken: cancellationToken) ?? [];
            var serverIds = servers.Select(server => server.Id).OfType<string>().ToList();
            var communities = serverIds.Count == 0 ? [] : await _communitiesApi.FindByDiscordServersAsync(serverIds, cancellationToken);
            LogServersMatched(_logger, communities.Count, serverIds.Count);
            return [.. communities.Select(community => community.CommunityId)];
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogServersFailed(_logger, exception);
            return [];
        }
    }

    /// <summary>Sends a player with claims awaiting a decision or an officer to the review page, keeping the requested page.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="requested">The page the player asked for.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The review page when a claim is pending or in conflict, or the check fails; otherwise the requested page.</returns>
    /// <remarks>A failed check still opens the review page, which shows the error and a retry, so no review is skipped.</remarks>
    private async Task<string?> RedirectAfterSignInAsync(Guid userId, string? requested, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _claimsApi.GetPendingAsync(userId, cancellationToken);
            if (claims.Count == 0)
            {
                return requested;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogPendingCheckFailed(_logger, exception);
        }

        return CharacterRoutes.ReviewFor(requested);
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Reads one server of Discord's list.</summary>
    /// <param name="Id">The server snowflake.</param>
    private sealed record ServerAnswer([property: JsonPropertyName("id")] string? Id);
    #endregion Nested Types
}
