using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Authenticates the website server by the shared key in <see cref="WebsiteServiceDefaults.HeaderName"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the website trust of ADR-0011. The key is compared in constant time and never logged.
/// </remarks>
internal sealed class ServiceKeyAuthenticationHandler : AuthenticationHandler<ServiceKeyAuthenticationOptions>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ServiceKeyAuthenticationHandler"/> class.</summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public ServiceKeyAuthenticationHandler(IOptionsMonitor<ServiceKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
    #endregion Constructors

    #region Overrides
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(WebsiteServiceDefaults.HeaderName, out var provided) || provided.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = Encoding.UTF8.GetBytes(provided[0] ?? string.Empty);
        if (!CryptographicOperations.FixedTimeEquals(providedKey, Encoding.UTF8.GetBytes(Options.ServiceKey)))
        {
            return Task.FromResult(AuthenticateResult.Fail("The service key is not valid."));
        }

        var identity = new ClaimsIdentity([new Claim(WebsiteServiceDefaults.ServiceClaim, WebsiteServiceDefaults.WebsiteService)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
    #endregion Overrides
}
