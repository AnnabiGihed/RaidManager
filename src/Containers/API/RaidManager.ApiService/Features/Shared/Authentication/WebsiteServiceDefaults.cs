namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Names the scheme, policy, header and claim that let the website call the API.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps the website trust contract (ADR-0011) in one place for the handler, the endpoints and the OpenAPI document.
/// </remarks>
public static class WebsiteServiceDefaults
{
    #region Constants
    /// <summary>Defines the authentication scheme for calls made by the website server.</summary>
    public const string Scheme = "WebsiteService";

    /// <summary>Defines the authorization policy that admits only the website server.</summary>
    public const string Policy = "Website";

    /// <summary>Defines the request header carrying the shared website key.</summary>
    public const string HeaderName = "X-RaidManager-Service-Key";

    /// <summary>Defines the claim that identifies the calling service.</summary>
    public const string ServiceClaim = "raidmanager:service";

    /// <summary>Defines the service claim value of the website.</summary>
    public const string WebsiteService = "website";

    /// <summary>Defines the configuration key of the shared website key.</summary>
    public const string ServiceKeyConfigurationKey = "Website:ServiceKey";

    /// <summary>Defines the shortest accepted website key, in characters.</summary>
    public const int MinimumServiceKeyLength = 32;
    #endregion Constants
}
