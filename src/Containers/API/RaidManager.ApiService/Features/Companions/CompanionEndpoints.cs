using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;
using RaidManager.Application.Features.Companions.Commands.CollectCompanionToken;
using RaidManager.Application.Features.Companions.Commands.StartCompanionPairing;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Maps the public routes a desktop companion calls.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Serves the companion side of ADR-0030 under /companion/, the only prefix the public API hostnames forward (owner decision on #369): starting a pairing and collecting the token anonymously and rate-limited, and the routes that need the device token.
/// </remarks>
public static class CompanionEndpoints
{
    #region Constants
    /// <summary>Defines the route prefix of the companion's routes.</summary>
    public const string CompanionRoute = "/companion";

    /// <summary>Defines the OpenAPI tag of the endpoints.</summary>
    private const string Tag = "Companion";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the pairing routes and the authenticated companion routes.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCompanionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var companion = endpoints.MapGroup(CompanionRoute).WithTags(Tag);

        companion.MapPost("/pairings", StartPairingAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CompanionRateLimits.PairingStartPolicy)
            .WithName("StartCompanionPairing")
            .WithSummary("Start pairing a companion")
            .WithDescription("Creates a pairing that lasts 10 minutes. The companion shows the pairing code, opens the website for the player to confirm it, and polls POST /companion/pairings/token with the device code. Rate-limited per address.")
            .Produces<StartedCompanionPairing>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        companion.MapPost("/pairings/token", CollectTokenAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CompanionRateLimits.TokenPollPolicy)
            .WithName("CollectCompanionToken")
            .WithSummary("Collect a companion's device token")
            .WithDescription("Answers 400 `CompanionPairing.Pending` until the player confirms the code, then the device token once. Afterwards, and for an unknown device code, it answers 400 `CompanionPairing.Invalid`; after 10 minutes, 400 `CompanionPairing.Expired`. Poll every 5 seconds or more; rate-limited per address.")
            .Produces<CompanionToken>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        companion.MapGet("/me", GetCurrent)
            .RequireAuthorization(CompanionTokenDefaults.Policy)
            .WithName("GetCurrentCompanion")
            .WithSummary("Check the calling companion's pairing")
            .WithDescription("Returns the companion the device token belongs to. A missing, unknown, revoked or expired token gets 401 with `Companion.TokenUnknown`, `Companion.Revoked` or `Companion.Expired`, as every companion route does.")
            .Produces<CurrentCompanion>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        companion.MapPost("/snapshots", UploadSnapshotAsync)
            .RequireAuthorization(CompanionTokenDefaults.Policy)
            .RequireRateLimiting(CompanionRateLimits.SnapshotUploadPolicy)
            .WithName("UploadCharacterSnapshot")
            .WithSummary("Upload one character snapshot")
            .WithDescription("Imports one character of the addon's `RaidManager.lua` (schema 1, `docs/reference/addon-savedvariables.md`) as JSON. A new character gets a pending claim for the companion's player; a character another player owns keeps its owner. Answers 202 `Imported` when the snapshot was applied, 200 `AlreadyCurrent` when one captured at the same time or later was already applied: the same snapshot can be retried safely. A snapshot that breaks the contract gets 400, and a new character without its identity 400 `Character.Snapshot.IdentityUnavailable`. Rate-limited per companion.")
            .Produces<UploadedCharacterSnapshot>()
            .Produces<UploadedCharacterSnapshot>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the start command.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the device code and pairing code, or ProblemDetails.</returns>
    private static async Task<IResult> StartPairingAsync(StartCompanionPairingRequest? request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartCompanionPairingCommand(request?.ComputerLabel), cancellationToken);
        return result.ToHttpResult(started => TypedResults.Ok(StartedCompanionPairing.From(started)));
    }

    /// <summary>Sends the collect command.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the token, or ProblemDetails.</returns>
    private static async Task<IResult> CollectTokenAsync(CollectCompanionTokenRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CollectCompanionTokenCommand(request.DeviceCode), cancellationToken);
        return result.ToHttpResult(token => TypedResults.Ok(CompanionToken.From(token)));
    }

    /// <summary>Sends the import command for the authenticated companion.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="user">The authenticated companion.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>202 when the snapshot was applied, 200 when it was already current, or ProblemDetails.</returns>
    private static async Task<IResult> UploadSnapshotAsync(
        UploadCharacterSnapshotRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var companionId = Guid.Parse(user.FindFirstValue(CompanionTokenDefaults.CompanionClaim)!);
        var result = await sender.Send(new ImportCharacterSnapshotCommand(companionId, request.SchemaVersion, request.Character), cancellationToken);
        return result.ToHttpResult(outcome => outcome == SnapshotImportOutcome.Imported
            ? TypedResults.Accepted((string?)null, UploadedCharacterSnapshot.From(outcome))
            : TypedResults.Ok(UploadedCharacterSnapshot.From(outcome)));
    }

    /// <summary>Describes the authenticated companion from its claims.</summary>
    /// <param name="user">The authenticated companion.</param>
    /// <returns>200 with the companion.</returns>
    private static Ok<CurrentCompanion> GetCurrent(ClaimsPrincipal user) => TypedResults.Ok(new CurrentCompanion(
        Guid.Parse(user.FindFirstValue(CompanionTokenDefaults.CompanionClaim)!),
        user.FindFirstValue(CompanionTokenDefaults.LabelClaim)!));
    #endregion Private Helpers
}
