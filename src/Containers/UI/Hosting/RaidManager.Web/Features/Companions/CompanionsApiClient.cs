using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaidManager.ViewModels.Features.Companions;

namespace RaidManager.Web.Features.Companions;

/// <summary>Calls the API's website-only companion routes with the shared website key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the website side of the companion routes of #382. The user id comes from the session, never from the browser (ADR-0011); 429 and server errors become failures the pages show.
/// </remarks>
internal sealed class CompanionsApiClient : ICompanionsApiClient
{
    #region Constants
    /// <summary>Defines the API's error code of an expired pairing code.</summary>
    private const string ExpiredCode = "CompanionPairing.Expired";
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client, addressed to the API through service discovery.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionsApiClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API and carrying the website key.</param>
    public CompanionsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<PairingLookup> GetPairingAsync(Guid userId, string pairingCode, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(PairingRoute(userId, pairingCode), cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var pairing = await response.Content.ReadFromJsonAsync<PendingPairing>(cancellationToken);
            return new PairingLookup(PairingCodeStatus.Waiting, pairing ?? throw new HttpRequestException("The API returned an empty pairing."));
        }

        return new PairingLookup(await RefusalAsync(response, cancellationToken), null);
    }

    /// <inheritdoc />
    public async Task<PairingCodeStatus> ConfirmAsync(Guid userId, string pairingCode, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"{PairingRoute(userId, pairingCode)}/confirm", content: null, cancellationToken);
        return response.IsSuccessStatusCode ? PairingCodeStatus.Paired : await RefusalAsync(response, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PairedCompanion>> GetCompanionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var companions = await _httpClient.GetFromJsonAsync<List<PairedCompanion>>(CompanionsRoute(userId), cancellationToken);
        return companions ?? throw new HttpRequestException("The API returned an empty companions response.");
    }

    /// <inheritdoc />
    public async Task<RevokeOutcome> RevokeAsync(Guid userId, Guid companionId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync($"{CompanionsRoute(userId)}/{companionId}/revoke", content: null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            return RevokeOutcome.Refused;
        }

        response.EnsureSuccessStatusCode();
        return RevokeOutcome.Revoked;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the route of a code.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="pairingCode">The code.</param>
    /// <returns>The relative route.</returns>
    private static string PairingRoute(Guid userId, string pairingCode) =>
        $"internal/users/{userId}/companion-pairings/{Uri.EscapeDataString(pairingCode)}";

    /// <summary>Builds the route of a player's companions.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <returns>The relative route.</returns>
    private static string CompanionsRoute(Guid userId) => $"internal/users/{userId}/companions";

    /// <summary>Reads why the API refused a code: 400 and 404 are unknown, 409 expired or already confirmed, anything else fails.</summary>
    /// <param name="response">The refusal.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The code's status.</returns>
    /// <exception cref="HttpRequestException">Thrown for any other answer, such as 429 or 500.</exception>
    private static async Task<PairingCodeStatus> RefusalAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest or HttpStatusCode.NotFound:
                return PairingCodeStatus.Unknown;
            case HttpStatusCode.Conflict:
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                return problem?.Title == ExpiredCode ? PairingCodeStatus.Expired : PairingCodeStatus.AlreadyConfirmed;
            default:
                response.EnsureSuccessStatusCode();
                throw new HttpRequestException($"The API answered {(int)response.StatusCode}.");
        }
    }
    #endregion Private Helpers
}
