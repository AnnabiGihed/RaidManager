using Microsoft.AspNetCore.Authentication;

namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Configures the shared key a calling service must present.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Carries the website key from configuration to the authentication handler without exposing it elsewhere.
/// </remarks>
public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    #region Properties
    /// <summary>Gets or sets the shared key the calling service must send.</summary>
    public string ServiceKey { get; set; } = string.Empty;
    #endregion Properties
}
