using MediatR;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Companions.Commands.ConfirmCompanionPairing;
using RaidManager.Application.Features.Companions.Commands.RevokeCompanion;
using RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;
using RaidManager.Application.Features.Companions.Queries.GetCompanions;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Maps the website's routes to confirm pairings and to list and revoke companions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Serves the website side of ADR-0030. The website passes the signed-in player from its session (ADR-0011); code lookups and confirmations are rate-limited per player, so codes can't be guessed.
/// </remarks>
public static class CompanionManagementEndpoints
{
    #region Constants
    /// <summary>Defines the route prefix of a player's pairings.</summary>
    public const string PairingsRoute = "/internal/users/{userId:guid}/companion-pairings";

    /// <summary>Defines the route prefix of a player's companions.</summary>
    public const string CompanionsRoute = "/internal/users/{userId:guid}/companions";

    /// <summary>Defines the OpenAPI tag of the endpoints.</summary>
    private const string Tag = "Companion management";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the pairing and companion routes, restricted to the website.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCompanionManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var pairings = endpoints.MapGroup(PairingsRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .RequireRateLimiting(CompanionRateLimits.CodeCheckPolicy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        pairings.MapGet("/{pairingCode}", GetPairingAsync)
            .WithName("GetCompanionPairing")
            .WithSummary("Describe the pairing a code belongs to")
            .WithDescription("Returns the code, the computer's label, the request time and the expiry, for the player to check before confirming. 404 for an unknown code; 409 `CompanionPairing.Expired` or `CompanionPairing.AlreadyConfirmed`. Rate-limited per player.")
            .Produces<CompanionPairingDetails>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pairings.MapPost("/{pairingCode}/confirm", ConfirmAsync)
            .WithName("ConfirmCompanionPairing")
            .WithSummary("Confirm a companion's pairing code")
            .WithDescription("Binds the computer to the signed-in player. The companion receives its token at its next poll. 404 for an unknown code or player; 409 `CompanionPairing.Expired` or `CompanionPairing.AlreadyConfirmed`. Rate-limited per player.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var companions = endpoints.MapGroup(CompanionsRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        companions.MapGet("/", ListAsync)
            .WithName("GetCompanions")
            .WithSummary("List a player's companions")
            .WithDescription("Returns every companion the player paired, revoked ones included, oldest first, with status `Active`, `Revoked` or `Expired`.")
            .Produces<IReadOnlyList<PairedCompanion>>()
            .ProducesValidationProblem();

        companions.MapPost("/{companionId:guid}/revoke", RevokeAsync)
            .WithName("RevokeCompanion")
            .WithSummary("Revoke a player's companion")
            .WithDescription("Stops the companion at once: its next request gets 401 `Companion.Revoked`. Characters and snapshots it uploaded stay. 404 for a companion the player doesn't have; 409 when already revoked.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the pairing query.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="pairingCode">The code the player entered.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the pairing, or ProblemDetails.</returns>
    private static async Task<IResult> GetPairingAsync(Guid userId, string pairingCode, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCompanionPairingQuery(userId, pairingCode), cancellationToken);
        return result.ToHttpResult(pairing => TypedResults.Ok(CompanionPairingDetails.From(pairing)));
    }

    /// <summary>Sends the confirm command.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="pairingCode">The code the player confirmed.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>204, or ProblemDetails.</returns>
    private static async Task<IResult> ConfirmAsync(Guid userId, string pairingCode, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmCompanionPairingCommand(userId, pairingCode), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }

    /// <summary>Sends the list query.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the companions, or ProblemDetails.</returns>
    private static async Task<IResult> ListAsync(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCompanionsQuery(userId), cancellationToken);
        return result.ToHttpResult(companions => TypedResults.Ok(companions.Select(PairedCompanion.From).ToList()));
    }

    /// <summary>Sends the revoke command.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="companionId">The companion to revoke.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>204, or ProblemDetails.</returns>
    private static async Task<IResult> RevokeAsync(Guid userId, Guid companionId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeCompanionCommand(userId, companionId), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }
    #endregion Private Helpers
}
