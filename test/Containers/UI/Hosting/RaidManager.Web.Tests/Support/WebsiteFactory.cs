using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Tests.Support;

/// <summary>Hosts the real website with test credentials, a stubbed Discord, and a fake API.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Tests the sign-in flow end to end through HTTP, as a browser drives it.
/// </remarks>
public sealed class WebsiteFactory : WebApplicationFactory<Program>
{
    #region Constants
    /// <summary>Defines the Discord client id configured for the tests.</summary>
    public const string ClientId = "test-client-id";
    #endregion Constants

    #region Properties
    /// <summary>Gets the fake API the website calls.</summary>
    public FakeIdentityApiClient IdentityApi { get; } = new();

    /// <summary>Gets the fake character claims API the website calls.</summary>
    public FakeCharacterClaimsApiClient ClaimsApi { get; } = new();
    #endregion Properties

    #region Public Methods
    /// <summary>Creates a browser-like client: HTTPS, cookies kept, redirects not followed.</summary>
    /// <returns>The HTTP client.</returns>
    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(RaidManagerAuthenticationExtensions.ClientIdKey, ClientId);
        builder.UseSetting(RaidManagerAuthenticationExtensions.ClientSecretKey, "test-client-secret");
        builder.UseSetting(RaidManagerAuthenticationExtensions.ServiceKeyKey, "test-website-service-key-0123456789abcdefghijkl");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityApiClient>();
            services.AddSingleton<IIdentityApiClient>(IdentityApi);
            services.RemoveAll<ICharacterClaimsApiClient>();
            services.AddSingleton<ICharacterClaimsApiClient>(ClaimsApi);
            services.PostConfigure<DiscordAuthenticationOptions>(
                DiscordAuthenticationDefaults.AuthenticationScheme,
                options => options.Backchannel = new HttpClient(new DiscordBackchannelStub()));
        });
    }
    #endregion Overrides
}
