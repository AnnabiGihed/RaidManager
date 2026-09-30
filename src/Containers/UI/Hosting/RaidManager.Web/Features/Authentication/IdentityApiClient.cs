using System.Net.Http.Json;

namespace RaidManager.Web.Features.Authentication;

/// <summary>Calls the API's website-only identity endpoint with the shared website key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the website side of ADR-0011: the key travels in a header and never leaves the server.
/// </remarks>
internal sealed class IdentityApiClient : IIdentityApiClient
{
    #region Constants
    /// <summary>Defines the API route that resolves a Discord sign-in.</summary>
    private const string DiscordSignInRoute = "internal/identity/discord-sign-in";
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client, addressed to the API through service discovery.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="IdentityApiClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API and carrying the website key.</param>
    public IdentityApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<Guid> ResolveDiscordSignInAsync(string discordUserId, string displayName, string? avatarUrl, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            DiscordSignInRoute,
            new DiscordSignInRequest(discordUserId, displayName, avatarUrl),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DiscordSignInResponse>(cancellationToken)
            ?? throw new HttpRequestException("The API returned an empty sign-in response.");
        return body.UserId;
    }
    #endregion Public Methods

    #region Nested Types
    /// <summary>Mirrors the API's sign-in request body.</summary>
    /// <param name="DiscordUserId">The Discord account identifier.</param>
    /// <param name="DisplayName">The player's Discord display name.</param>
    /// <param name="AvatarUrl">The player's Discord avatar URL, if any.</param>
    private sealed record DiscordSignInRequest(string DiscordUserId, string DisplayName, string? AvatarUrl);

    /// <summary>Mirrors the API's sign-in response body.</summary>
    /// <param name="UserId">The local user id.</param>
    private sealed record DiscordSignInResponse(Guid UserId);
    #endregion Nested Types
}
