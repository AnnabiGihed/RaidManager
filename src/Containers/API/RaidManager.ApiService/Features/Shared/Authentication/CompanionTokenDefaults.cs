namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Names the scheme, policy and claims that let a paired companion call the API.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the companion trust contract of ADR-0030 in one place for the handler, the endpoints and the OpenAPI document.
/// </remarks>
public static class CompanionTokenDefaults
{
    #region Constants
    /// <summary>Defines the authentication scheme for calls made by a companion with its device token.</summary>
    public const string Scheme = "CompanionToken";

    /// <summary>Defines the authorization policy that admits only an active companion.</summary>
    public const string Policy = "Companion";

    /// <summary>Defines the claim holding the companion's identifier.</summary>
    public const string CompanionClaim = "raidmanager:companion";

    /// <summary>Defines the claim holding the identifier of the player the companion uploads for.</summary>
    public const string UserClaim = "raidmanager:user";

    /// <summary>Defines the claim holding the computer's label.</summary>
    public const string LabelClaim = "raidmanager:companion-label";

    /// <summary>Defines the prefix of the authorization header value.</summary>
    public const string BearerPrefix = "Bearer ";
    #endregion Constants
}
