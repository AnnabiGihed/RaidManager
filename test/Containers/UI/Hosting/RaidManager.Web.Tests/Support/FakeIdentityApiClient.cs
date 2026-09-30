using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Tests.Support;

/// <summary>Stands in for the API during website tests and records what the website sent.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets the tests check the call to the API and simulate the API being unavailable.
/// </remarks>
public sealed class FakeIdentityApiClient : IIdentityApiClient
{
    #region Properties
    /// <summary>Gets the user id the fake returns.</summary>
    public Guid UserId { get; } = Guid.NewGuid();

    /// <summary>Gets or sets a value indicating whether the next call fails as an unavailable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets the Discord id of the latest call.</summary>
    public string? LastDiscordUserId { get; private set; }

    /// <summary>Gets the display name of the latest call.</summary>
    public string? LastDisplayName { get; private set; }

    /// <summary>Gets the avatar URL of the latest call.</summary>
    public string? LastAvatarUrl { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<Guid> ResolveDiscordSignInAsync(string discordUserId, string displayName, string? avatarUrl, CancellationToken cancellationToken)
    {
        if (Fails)
        {
            throw new HttpRequestException("The API is unavailable.");
        }

        LastDiscordUserId = discordUserId;
        LastDisplayName = displayName;
        LastAvatarUrl = avatarUrl;
        return Task.FromResult(UserId);
    }
    #endregion Public Methods
}
