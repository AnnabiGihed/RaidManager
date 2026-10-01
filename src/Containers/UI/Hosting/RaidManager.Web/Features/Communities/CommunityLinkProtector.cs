using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.Web.Features.Communities;

/// <summary>Protects what travels through the browser while a bot is being added: the state and the pending link.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The OAuth state ties Discord's return to the session that started it; the pending link carries the server
/// Discord confirmed to the realm choice. Both are encrypted, signed, bound to the user and expire, so a visitor can't
/// forge, change, replay or reuse them in another session.
/// </remarks>
public sealed class CommunityLinkProtector
{
    #region Fields
    /// <summary>Stores how long the user has to come back from Discord.</summary>
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Stores how long the user has to choose the realm.</summary>
    private static readonly TimeSpan PendingLinkLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Stores the protector of the OAuth state cookie.</summary>
    private readonly ITimeLimitedDataProtector _state;

    /// <summary>Stores the protector of the pending link.</summary>
    private readonly ITimeLimitedDataProtector _pendingLink;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityLinkProtector"/> class.</summary>
    /// <param name="provider">The data protection provider.</param>
    public CommunityLinkProtector(IDataProtectionProvider provider)
    {
        _state = provider.CreateProtector("RaidManager.CommunityLink.State").ToTimeLimitedDataProtector();
        _pendingLink = provider.CreateProtector("RaidManager.CommunityLink.Pending").ToTimeLimitedDataProtector();
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Creates a fresh OAuth state and the protected cookie value that remembers it for the user.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <returns>The state to send to Discord and the cookie value to keep.</returns>
    public (string State, string CookieValue) CreateState(Guid userId)
    {
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (state, _state.Protect($"{userId:N}|{state}", StateLifetime));
    }

    /// <summary>Tells whether Discord's returned state matches the one remembered for the user.</summary>
    /// <param name="cookieValue">The protected cookie value, if any.</param>
    /// <param name="returnedState">The state Discord returned, if any.</param>
    /// <param name="userId">The signed-in user.</param>
    /// <returns><see langword="true"/> when the cookie is valid, unexpired, the user's, and holds the same state.</returns>
    public bool IsExpectedState(string? cookieValue, string? returnedState, Guid userId)
    {
        if (string.IsNullOrEmpty(cookieValue) || string.IsNullOrEmpty(returnedState) || !TryUnprotect(_state, cookieValue, out var remembered))
        {
            return false;
        }

        var expected = $"{userId:N}|{returnedState}";
        return CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(remembered), System.Text.Encoding.UTF8.GetBytes(expected));
    }

    /// <summary>Protects a pending link for the realm choice.</summary>
    /// <param name="link">The pending link.</param>
    /// <returns>The protected value.</returns>
    public string Protect(PendingCommunityLink link) => _pendingLink.Protect(JsonSerializer.Serialize(link), PendingLinkLifetime);

    /// <summary>Reads a protected pending link back for the user who started it.</summary>
    /// <param name="protectedLink">The protected value.</param>
    /// <param name="userId">The signed-in user.</param>
    /// <returns>The pending link, or <see langword="null"/> when it is invalid, expired or another user's.</returns>
    public PendingCommunityLink? Unprotect(string? protectedLink, Guid userId)
    {
        if (string.IsNullOrEmpty(protectedLink) || !TryUnprotect(_pendingLink, protectedLink, out var json))
        {
            return null;
        }

        var link = JsonSerializer.Deserialize<PendingCommunityLink>(json);
        return link?.UserId == userId ? link : null;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Unprotects a value, treating a tampered, foreign or expired value as absent.</summary>
    /// <param name="protector">The protector.</param>
    /// <param name="value">The protected value.</param>
    /// <param name="plain">The plain text when it succeeded.</param>
    /// <returns><see langword="true"/> when the value was valid.</returns>
    private static bool TryUnprotect(ITimeLimitedDataProtector protector, string value, out string plain)
    {
        try
        {
            plain = protector.Unprotect(value, out _);
            return true;
        }
        catch (CryptographicException)
        {
            plain = string.Empty;
            return false;
        }
    }
    #endregion Private Helpers
}
