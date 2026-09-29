---
name: pivot-auth-hangfire
description: Pivot.Framework.Authentication.Hangfire — browser login (Cookie + Keycloak OpenID Connect) dedicated to the Hangfire dashboard, independent of the API's JWT bearer scheme. Use when protecting /hangfire with Keycloak for humans (AddHangfireKeycloakBrowserAuth, UseHangfireDashboardWithKeycloakAuth, /hangfire-login, /hangfire-logout, /hangfire-callback), adding role checks to the dashboard, debugging redirect loops, or changing Src/Containers/Authentication/Pivot.Framework.Authentication.Hangfire.
---

# Pivot.Framework.Authentication.Hangfire

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication.Hangfire`. References Authentication core,
Infrastructure.Scheduling (Hangfire), `Microsoft.AspNetCore.Authentication.OpenIdConnect`.

## Registration

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration, o => o.WithCurrentUser());  // API JWT (default scheme)
builder.Services.AddHangfireWithDashboard(builder.Configuration);                             // server + storage
builder.Services.AddHangfireKeycloakBrowserAuth(builder.Configuration);                       // cookie + OIDC schemes

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboardWithKeycloakAuth(o => o.DarkModeEnabled = true);   // maps /hangfire, /hangfire-login, /hangfire-logout
```
Do not also call `UseHangfireDashboardWithOptions` (two dashboards on `/hangfire`).

`AddHangfireKeycloakBrowserAuth(configuration)` reads `Keycloak:Realm`, `BaseUrl`, `ClientId` (required — throws), `ClientSecret` (optional),
`RequireHttpsMetadata` (**defaults to false here** when absent/unparseable — unlike `KeycloakOptions`), and adds via
`new AuthenticationBuilder(services)` (it does *not* call `AddAuthentication`, so the default scheme stays your API's JWT bearer):
- Cookie scheme **`HangfireCookie`** (`LoginPath`/`AccessDeniedPath` = `/hangfire-login`).
- OIDC scheme **`HangfireOidc`**: authority `{BaseUrl}/realms/{Realm}`, `ResponseType = code`, `SaveTokens = true`, `CallbackPath = /hangfire-callback`, `SignInScheme = HangfireCookie`, name claim `preferred_username`, Pushed Authorization Requests disabled.
Make sure `AddAuthentication(...)` is called somewhere (the JWT registration does it) so authentication core services exist.

`UseHangfireDashboardWithKeycloakAuth(this WebApplication, configure?)`:
- `GET /hangfire-login` → `Challenge(HangfireOidc, RedirectUri = "/hangfire")`.
- `GET /hangfire-logout` → sign out of `HangfireCookie` and `HangfireOidc` (Keycloak end-session).
- Dashboard at `/hangfire` with `HangfireCookieDashboardAuthorizationFilter`: authenticates `HangfireCookie`; if not authenticated it writes a **302 to `/hangfire-login` and completes the response**, returning `true` so Hangfire doesn't overwrite it with 401.

## Keycloak client
Use a dedicated client (e.g. `orders-hangfire`) or the API client if it allows Standard Flow. Valid Redirect URIs:
`https://<app>/hangfire-callback`; Post-logout redirect: `https://<app>/*` or the signed-out callback
(`/signout-callback-oidc` default). Confidential client → set `Keycloak:ClientSecret`.
Note that this package reads the same `Keycloak` section as the API — if the API client is bearer-only, you need a second
client id; since the section is shared, fork the configuration (e.g. build an in-memory `IConfiguration` with a `Keycloak` section for Hangfire) or extend the extension to accept a section name.

## Authorization beyond "logged in"
The filter only checks authentication — **any realm user can open the dashboard**. To require a role, wrap it:
```csharp
public sealed class HangfireRoleFilter(string role) : IDashboardAuthorizationFilter
{
	private readonly HangfireCookieDashboardAuthorizationFilter _inner = new();
	public bool Authorize(DashboardContext context)
	{
		if (!_inner.Authorize(context)) return false;
		var http = context.GetHttpContext();
		if (http.Response.HasStarted) return true;                         // redirect already issued
		var result = http.AuthenticateAsync("HangfireCookie").GetAwaiter().GetResult();
		KeycloakRoleHelper.FlattenRoles((ClaimsIdentity)result.Principal!.Identity!);
		return result.Principal!.IsInRole(role);
	}
}
app.UseHangfireDashboardWithKeycloakAuth(o => o.Authorization = [new HangfireRoleFilter("ops")]);
```
(OIDC cookie principals are built from the ID token/userinfo — add a Keycloak mapper that puts roles into the ID token, as `realm_access` is access-token-only by default.)

## Pitfalls
- Redirect loop to `/hangfire-login`: cookie not persisted — behind TLS-terminating proxy use `UseForwardedHeaders` so the callback is seen as HTTPS; SameSite issues on HTTP dev hosts.
- "Correlation failed" on `/hangfire-callback`: nonce/correlation cookies lost (same causes) or multiple instances without shared Data Protection keys — persist keys (Redis/DB) when scaling out.
- The login endpoint hardcodes `RedirectUri = "/hangfire"` and the filter redirects to the absolute path `/hangfire-login` — apps hosted under a path base need adjustments.

## Changing this package
Scheme names live in `Constants/HangfireAuthConstants` (internal). Keep it independent of the API's JWT configuration. No tests exist;
the filter can be unit-tested with a `DashboardContext` over a `DefaultHttpContext` with a registered `IAuthenticationService` substitute.
