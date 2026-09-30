using MediatR;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;

namespace RaidManager.ApiService.Features.Identity;

/// <summary>Maps the identity endpoints the website uses.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Exposes Discord sign-in resolution to the website only; endpoints send commands and never touch aggregates.
/// </remarks>
public static class IdentityEndpoints
{
    #region Constants
    /// <summary>Defines the route of the Discord sign-in resolution.</summary>
    public const string DiscordSignInRoute = "/internal/identity/discord-sign-in";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps <c>POST /internal/identity/discord-sign-in</c>, restricted to the website.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(DiscordSignInRoute, SignInWithDiscordAsync)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithName("ResolveDiscordSignIn")
            .WithTags("Identity")
            .WithSummary("Resolve a Discord sign-in to its local user")
            .WithDescription("Called by the website after a completed Discord OAuth exchange. Registers a first-time player or returns the existing user.")
            .Produces<DiscordSignInResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the sign-in command and maps its result.</summary>
    /// <param name="request">The confirmed Discord identity.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the user identifier, or ProblemDetails.</returns>
    private static async Task<IResult> SignInWithDiscordAsync(DiscordSignInRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SignInWithDiscordCommand(request.DiscordUserId, request.DisplayName, request.AvatarUrl), cancellationToken);
        return result.ToHttpResult(userId => TypedResults.Ok(new DiscordSignInResponse(userId)));
    }
    #endregion Private Helpers
}
