using System.Net;
using System.Net.Http.Json;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.Web.Features.Characters;

/// <summary>Calls the API's website-only character claim endpoints with the shared website key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements the website side of the claims API (#161). The user id comes from the session, never from the
/// browser (ADR-0011).
/// </remarks>
internal sealed class CharacterClaimsApiClient : ICharacterClaimsApiClient
{
    #region Fields
    /// <summary>Stores the HTTP client, addressed to the API through service discovery.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterClaimsApiClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API and carrying the website key.</param>
    public CharacterClaimsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<IReadOnlyList<CharacterClaim>> GetPendingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var claims = await _httpClient.GetFromJsonAsync<List<CharacterClaim>>($"{ClaimsRoute(userId)}/pending", cancellationToken);
        return claims ?? throw new HttpRequestException("The API returned an empty claims response.");
    }

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> ApproveAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        DecideAsync($"{ClaimsRoute(userId)}/{characterId}/approve", cancellationToken);

    /// <inheritdoc />
    public Task<ClaimDecisionOutcome> RejectAsync(Guid userId, Guid characterId, CancellationToken cancellationToken) =>
        DecideAsync($"{ClaimsRoute(userId)}/{characterId}/reject", cancellationToken);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the route of a player's claims.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <returns>The relative route.</returns>
    private static string ClaimsRoute(Guid userId) => $"internal/users/{userId}/character-claims";

    /// <summary>Posts a decision: 204 is recorded, 404 and 409 are refused, anything else fails.</summary>
    /// <param name="route">The decision route.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The outcome.</returns>
    private async Task<ClaimDecisionOutcome> DecideAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(route, content: null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            return ClaimDecisionOutcome.Refused;
        }

        response.EnsureSuccessStatusCode();
        return ClaimDecisionOutcome.Recorded;
    }
    #endregion Private Helpers
}
