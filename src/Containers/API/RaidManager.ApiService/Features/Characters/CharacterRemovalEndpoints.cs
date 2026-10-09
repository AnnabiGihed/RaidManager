using MediatR;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Maps the dev and test reset that removes a player's characters.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: <c>DELETE /internal/users/{userId}/characters</c> lets the website reset a tester's characters (story #597).
/// It is mapped only where <see cref="TestEnvironments.OffersTestTools"/> allows, so production never runs it; the
/// website's service key is required as for every internal route.
/// </remarks>
public static class CharacterRemovalEndpoints
{
    #region Constants
    /// <summary>Defines the OpenAPI tag.</summary>
    private const string Tag = "Character profiles";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the removal on Development, Dev and Test; elsewhere maps nothing.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCharacterRemovalEndpoints(this IEndpointRouteBuilder endpoints, IHostEnvironment environment)
    {
        if (!environment.OffersTestTools())
        {
            return endpoints;
        }

        endpoints.MapDelete(CharacterProfileEndpoints.CharactersRoute, RemoveAsync)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .WithName("RemoveMyCharacters")
            .WithSummary("Remove all of a player's characters (dev and test only)")
            .WithDescription("Deletes the characters only the player owns or claims, with their claims and loadouts, and withdraws the player's claim on any other. The companion's next sync imports them again for review. Not available in production.")
            .Produces<CharacterRemoval>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the removal command.</summary>
    /// <param name="userId">The player.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the number removed, or ProblemDetails.</returns>
    private static async Task<IResult> RemoveAsync(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveMyCharactersCommand(userId), cancellationToken);
        return result.ToHttpResult(removed => TypedResults.Ok(new CharacterRemoval(removed)));
    }
    #endregion Private Helpers
}
