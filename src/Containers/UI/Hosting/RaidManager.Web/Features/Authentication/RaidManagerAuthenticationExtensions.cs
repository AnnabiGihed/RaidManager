using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Characters;
using RaidManager.Web.Features.Communities;

namespace RaidManager.Web.Features.Authentication;

/// <summary>Registers Discord sign-in, the cookie session, and the API clients the website uses.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the website session of ADR-0011 and fails the host at startup when a required secret is missing.
/// </remarks>
public static class RaidManagerAuthenticationExtensions
{
    #region Constants
    /// <summary>Defines the session cookie name; the <c>__Host-</c> prefix binds it to this host over HTTPS.</summary>
    public const string SessionCookieName = "__Host-RaidManager.Session";

    /// <summary>Defines the configuration key of the Discord client id.</summary>
    public const string ClientIdKey = "Authentication:Discord:ClientId";

    /// <summary>Defines the configuration key of the Discord client secret.</summary>
    public const string ClientSecretKey = "Authentication:Discord:ClientSecret";

    /// <summary>Defines the configuration key of the shared website key.</summary>
    public const string ServiceKeyKey = "Website:ServiceKey";

    /// <summary>Defines the request header carrying the website key to the API.</summary>
    public const string ServiceKeyHeader = "X-RaidManager-Service-Key";

    /// <summary>Defines the API address, resolved by Aspire service discovery.</summary>
    private const string ApiBaseAddress = "https+http://api";

    /// <summary>Defines the address of Discord's REST API, version included.</summary>
    private const string DiscordApiBaseAddress = "https://discord.com/api/v10/";
    #endregion Constants

    #region Fields
    /// <summary>Stores how long a session stays valid without activity.</summary>
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    #endregion Fields

    #region Public Methods
    /// <summary>Adds the cookie session, Discord sign-in, and the identity and character claims API clients.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the Discord credentials and the website key.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a Discord credential or the website key is missing.</exception>
    public static IServiceCollection AddRaidManagerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var clientId = Required(configuration, ClientIdKey);
        var clientSecret = Required(configuration, ClientSecretKey);
        var serviceKey = Required(configuration, ServiceKeyKey);

        services.AddScoped<DiscordSignInEvents>();
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                // Lax lets the session cookie accompany the top-level redirect back from Discord.
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = true;
                options.LoginPath = AuthenticationRoutes.SignIn;
                options.ReturnUrlParameter = "returnUrl";
            })
            .AddDiscord(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.CallbackPath = AuthenticationRoutes.DiscordCallback;
                options.Scope.Clear();
                options.Scope.Add("identify");
                options.SaveTokens = false;
                options.ClaimActions.MapJsonKey(RaidManagerClaimTypes.DiscordGlobalName, "global_name");
                options.EventsType = typeof(DiscordSignInEvents);
            });
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        void AddressApi(HttpClient client)
        {
            client.BaseAddress = new Uri(ApiBaseAddress);
            client.DefaultRequestHeaders.Add(ServiceKeyHeader, serviceKey);
        }

        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(AddressApi);
        services.AddHttpClient<ICharacterClaimsApiClient, CharacterClaimsApiClient>(AddressApi);
        services.AddHttpClient<ICommunitiesApiClient, CommunitiesApiClient>(AddressApi);

        // Adding the bot to a server uses the same Discord application as sign-in (docs/how-to/set-up-discord.md).
        services.AddSingleton(new DiscordApplicationCredentials(clientId, clientSecret));
        services.AddSingleton<CommunityLinkProtector>();
        services.AddHttpClient<IDiscordInstallClient, DiscordInstallClient>(client => client.BaseAddress = new Uri(DiscordApiBaseAddress));
        return services;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Reads a required configuration value.</summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="key">The key to read.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing.</exception>
    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Configure '{key}'; the Aspire AppHost supplies it from its parameters.")
            : value;
    }
    #endregion Private Helpers
}
