---
name: pivot-auth-maui
description: Pivot.Framework.Authentication.Maui — Keycloak Authorization Code + PKCE login for .NET MAUI / MAUI Blazor Hybrid using the system browser (WebAuthenticator) and SecureStorage. Use when adding login/logout/session restore to a MAUI app (AddKeycloakMaui, IKeycloakAuthService), registering the {ClientId}://callback URI scheme per platform, attaching tokens to HttpClients (AddKeycloakHandler), wiring AuthorizeView in Blazor Hybrid, or changing Src/Containers/Authentication/Pivot.Framework.Authentication.Maui.
---

# Pivot.Framework.Authentication.Maui

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication.Maui`. References Authentication core,
`Microsoft.Maui.Controls`, `Microsoft.AspNetCore.Components.Authorization`, `Microsoft.IdentityModel.JsonWebTokens`,
`System.IdentityModel.Tokens.Jwt`.

## Registration (`MauiProgram.cs`)

```csharp
builder.Configuration.AddJsonStream(FileSystem.OpenAppPackageFileAsync("appsettings.json").Result);  // or in-memory config
builder.Services.AddKeycloakMaui(builder.Configuration);            // section "Keycloak"
builder.Services.AddHttpClient("OrdersApi", c => c.BaseAddress = new Uri("https://orders.example.com"))
	.AddKeycloakHandler();                                        // namespace Pivot.Framework.Authentication.Maui.Extensions

// Blazor Hybrid only — AddKeycloakMaui does NOT register an AuthenticationStateProvider:
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthStateProvider>();   // see below
```
`AddKeycloakMaui` registers: `KeycloakOptions`, named `HttpClient` `"KeycloakAuthService"`, **singletons**
`IKeycloakTokenStorage → KeycloakTokenStorage` (SecureStorage key `keycloak_token_set`) and
`IKeycloakAuthService → KeycloakAuthService`, transient `KeycloakAuthorizationMessageHandler`.

The Blazor package's `KeycloakAuthStateProvider` works with any `IKeycloakAuthService`, but referencing the Blazor
package pulls Blazor-Server-specific code. A minimal equivalent for Hybrid:
```csharp
public sealed class MauiAuthStateProvider : AuthenticationStateProvider, IDisposable
{
	private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
	private readonly IKeycloakAuthService _auth;
	public MauiAuthStateProvider(IKeycloakAuthService auth) { _auth = auth; _auth.AuthStateChanged += OnChanged; }
	public override Task<AuthenticationState> GetAuthenticationStateAsync()
		=> Task.FromResult(_auth.IsAuthenticated && _auth.User is not null ? new AuthenticationState(_auth.User) : Anonymous);
	private void OnChanged(object? s, AuthStateChangedEventArgs e) => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	public void Dispose() => _auth.AuthStateChanged -= OnChanged;
}
```

## Keycloak client and platform setup

Public client, Standard Flow + PKCE. Redirect URI **`{ClientId}://callback`** and post-logout URI `{ClientId}://loggedout`
(the scheme is the client id, so the client id must be a valid URI scheme: lowercase letters, digits, `+ . -`, starting with a letter).

| Platform | Setup |
|---|---|
| Android | `WebAuthenticatorCallbackActivity` subclass with `[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "<clientid>")]`; on Android 11+ add a `<queries>` intent for `android.support.customtabs.action.CustomTabsService` |
| iOS / Mac Catalyst | `CFBundleURLSchemes` = `<clientid>` in `Info.plist` (WebAuthenticator on iOS uses ASWebAuthenticationSession) |
| Windows | Protocol `<clientid>` in `Package.appxmanifest` → Declarations; WinUI WebAuthenticator needs the app to be packaged |
Missing registration = the browser never returns to the app (`LoginAsync` hangs until the user cancels → `false`).

## Usage

```csharp
public partial class App : Application
{
	public App(IKeycloakAuthService auth)
	{
		InitializeComponent();
		_ = auth.TryRestoreSessionAsync();     // restore on start-up (refreshes if the access token expired)
	}
}
// login button
var ok = await auth.LoginAsync();          // opens the system browser
// logout
await auth.LogoutAsync();                  // clears SecureStorage, revokes refresh token, opens end-session URL in the browser
```

## Flow details (`Services/KeycloakAuthService`)
- `LoginAsync`: PKCE (S256) + random `state` + `nonce`; `WebAuthenticator.Default.AuthenticateAsync(authUrl, callback)` (`PrefersEphemeralWebBrowserSession = false`, so Keycloak SSO cookies are reused); verifies returned `state`; exchanges the code; validates the **ID token signature and nonce** and the **access token signature/issuer/lifetime (+audience when configured)** via the discovery document/JWKS; persists the `KeycloakTokenSet`; raises `AuthStateChanged`. Returns `false` on cancel/mismatch/error (logged).
- `GetAccessTokenAsync`: returns the current token or refreshes (semaphore-serialized); throws `UnauthorizedAccessException` if not logged in or refresh impossible.
- `TryRestoreSessionAsync`: loads from SecureStorage → valid → re-validate & notify; expired but refreshable → refresh; else clear.
- Tokens expire 30 s early (`KeycloakTokenSet.IsExpired`); `offline_access` scope gives long-lived refresh tokens (`RefreshTokenExpiresAt` null → treated as valid).
- Everything is a singleton — one user per app instance.

## Pitfalls
- On Android emulators `localhost` is the emulator; use `10.0.2.2` or a real host for `Keycloak:BaseUrl`, and the token `iss` must match `IssuerUrl` exactly (configure Keycloak's hostname).
- `RequireHttpsMetadata: false` is needed for plain-HTTP dev Keycloak; Android also blocks cleartext by default (network security config).
- SecureStorage can throw on some devices after restore/backup — the storage swallows read errors and returns null (forces re-login).

## Changing this package
Keep parity with the Blazor implementation of `IKeycloakAuthService` (same validation rules, exception semantics and events).
Any API change here must compile for all MAUI target frameworks the consumer uses; the project currently targets `net10.0`
via `Directory.Build.props` (MAUI platform APIs like `WebAuthenticator` resolve through `Microsoft.Maui.Controls`). No tests exist.
