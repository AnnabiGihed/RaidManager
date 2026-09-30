namespace RaidManager.Web.Features.Authentication;

/// <summary>Calls the API's identity operations on behalf of the website server.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Isolates the website-to-API call behind an interface so sign-in can be tested without a running API.
/// </remarks>
public interface IIdentityApiClient
{
    #region Methods
    /// <summary>Resolves a Discord identity confirmed by OAuth to its local user.</summary>
    /// <param name="discordUserId">The Discord account identifier.</param>
    /// <param name="displayName">The player's Discord display name.</param>
    /// <param name="avatarUrl">The player's Discord avatar URL, if any.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The local user id.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API rejects the call or cannot be reached.</exception>
    Task<Guid> ResolveDiscordSignInAsync(string discordUserId, string displayName, string? avatarUrl, CancellationToken cancellationToken);
    #endregion Methods
}
