using System.Net;
using System.Net.Http.Json;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.Web.Features.Characters;

/// <summary>Calls the API's website-only character profile endpoints with the shared website key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Implements the website side of the profile API (story #19). The user id comes from the session, never from the browser (ADR-0011).
/// </remarks>
internal sealed class CharacterProfilesApiClient : ICharacterProfilesApiClient
{
    #region Fields
    /// <summary>Stores the HTTP client, addressed to the API through service discovery.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterProfilesApiClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API and carrying the website key.</param>
    public CharacterProfilesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var characters = await _httpClient.GetFromJsonAsync<List<CharacterSummary>>(CharactersRoute(userId), cancellationToken);
        return characters ?? throw new HttpRequestException("The API returned an empty characters response.");
    }

    /// <inheritdoc />
    public async Task<CharacterProfile?> GetProfileAsync(Guid userId, Guid characterId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"{CharactersRoute(userId)}/{characterId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CharacterProfile>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty profile response.");
    }

    /// <inheritdoc />
    public async Task<int> RemoveAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(CharactersRoute(userId), cancellationToken);
        response.EnsureSuccessStatusCode();
        var removal = await response.Content.ReadFromJsonAsync<Removal>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty removal response.");
        return removal.Removed;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the route of a player's characters.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <returns>The relative route.</returns>
    private static string CharactersRoute(Guid userId) => $"internal/users/{userId}/characters";
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Reads the API's answer to a removal.</summary>
    /// <param name="Removed">The number of characters the player no longer has.</param>
    private sealed record Removal(int Removed);
    #endregion Nested Types
}
