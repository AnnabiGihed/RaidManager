using MediatR;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;
using RaidManager.Application.Features.Characters.Commands.RejectCharacterClaim;
using RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Maps the character claim review endpoints the website uses.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets the website list a signed-in player's claims awaiting a decision and approve or reject one. The
/// website passes the user from its session (ADR-0011); endpoints send the query and commands and never touch aggregates.
/// </remarks>
public static class CharacterClaimEndpoints
{
    #region Constants
    /// <summary>Defines the route prefix of a player's character claims.</summary>
    public const string ClaimsRoute = "/internal/users/{userId:guid}/character-claims";

    /// <summary>Defines the OpenAPI tag of the endpoints.</summary>
    private const string Tag = "Character claims";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the pending-claims query and the approve and reject commands, restricted to the website.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCharacterClaimEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var claims = endpoints.MapGroup(ClaimsRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        claims.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingCharacterClaims")
            .WithSummary("List a player's character claims awaiting a decision")
            .WithDescription("Returns the pending and conflicted claims of the player, oldest first. The website shows them on the review page after sign-in.")
            .Produces<IReadOnlyList<PendingCharacterClaim>>()
            .ProducesValidationProblem();

        claims.MapPost("/{characterId:guid}/approve", ApproveAsync)
            .WithName("ApproveCharacterClaim")
            .WithSummary("Approve a player's claim on a character")
            .WithDescription("Makes the character the player's. A character another player already owns is not transferred: the claim moves to conflict review and the call returns 409.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        claims.MapPost("/{characterId:guid}/reject", RejectAsync)
            .WithName("RejectCharacterClaim")
            .WithSummary("Reject a player's claim on a character")
            .WithDescription("Records that the character isn't the player's, so it never becomes one of their signup options.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the pending-claims query.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the claims, or ProblemDetails.</returns>
    private static async Task<IResult> GetPendingAsync(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingCharacterClaimsQuery(userId), cancellationToken);
        return result.ToHttpResult(claims => TypedResults.Ok(claims.Select(PendingCharacterClaim.From).ToList()));
    }

    /// <summary>Sends the approve command.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The claimed character.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>204, or ProblemDetails.</returns>
    private static async Task<IResult> ApproveAsync(Guid userId, Guid characterId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveCharacterClaimCommand(characterId, userId), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }

    /// <summary>Sends the reject command.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The claimed character.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>204, or ProblemDetails.</returns>
    private static async Task<IResult> RejectAsync(Guid userId, Guid characterId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RejectCharacterClaimCommand(characterId, userId), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }
    #endregion Private Helpers
}
