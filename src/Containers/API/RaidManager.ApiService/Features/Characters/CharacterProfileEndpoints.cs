using MediatR;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;
using RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

namespace RaidManager.ApiService.Features.Characters;

/// <summary>Maps the character profile endpoints the website uses.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets the website list a signed-in player's characters and show one profile (story #19). The website passes the user from its session (ADR-0011); a character that isn't the player's answers 404 (owner decision on #19, 2026-10-05).
/// </remarks>
public static class CharacterProfileEndpoints
{
    #region Constants
    /// <summary>Defines the route prefix of a player's characters.</summary>
    public const string CharactersRoute = "/internal/users/{userId:guid}/characters";

    /// <summary>Defines the OpenAPI tag of the endpoints.</summary>
    private const string Tag = "Character profiles";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the characters list and the profile queries, restricted to the website.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCharacterProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var characters = endpoints.MapGroup(CharactersRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        characters.MapGet("/", ListAsync)
            .WithName("GetMyCharacters")
            .WithSummary("List a player's characters")
            .WithDescription("Returns the characters the player owns through an approved claim, by realm and name, with the primary loadout, the current raid saves and the latest sync.")
            .Produces<IReadOnlyList<CharacterSummary>>()
            .ProducesValidationProblem();

        characters.MapGet("/{characterId:guid}", GetAsync)
            .WithName("GetCharacterProfile")
            .WithSummary("Get the profile of one of a player's characters")
            .WithDescription("Returns realm, class, data sources, professions, loadouts with their equipment, and current raid saves. A character the player doesn't own returns 404.")
            .Produces<CharacterProfile>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Sends the characters list query.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the characters, or ProblemDetails.</returns>
    private static async Task<IResult> ListAsync(Guid userId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyCharactersQuery(userId), cancellationToken);
        return result.ToHttpResult(characters => TypedResults.Ok(characters.Select(CharacterSummary.From).ToList()));
    }

    /// <summary>Sends the profile query.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The character.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>200 with the profile, or ProblemDetails.</returns>
    private static async Task<IResult> GetAsync(Guid userId, Guid characterId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCharacterProfileQuery(userId, characterId), cancellationToken);
        return result.ToHttpResult(profile => TypedResults.Ok(CharacterProfile.From(profile)));
    }
    #endregion Private Helpers
}
