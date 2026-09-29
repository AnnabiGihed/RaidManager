---
name: pivot-auth-blazor
description: Pivot.Framework.Authentication.Blazor — Keycloak Authorization Code + PKCE login for Blazor Server with tokens kept server-side in Redis and an HttpOnly kc_session cookie. Use when adding login/logout to a Blazor Server app (AddKeycloakBlazor, IBlazorKeycloakAuthService, InitialiseFromCookieAsync, the shipped /auth/callback page, AuthorizeView/[Authorize]), calling APIs with the user's token (AddKeycloakHandler), debugging session restore/refresh/state-mismatch issues, or changing Src/Containers/Authentication/Pivot.Framework.Authentication.Blazor.
---

# Pivot.Framework.Authentication.Blazor

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication.Blazor` (Microsoft.NET.Sdk with a `.razor` page).
References Authentication core + `Microsoft.AspNetCore.Components.Authorization`.

## Registration

```csharp
builder.Services.AddAuthentication();                               // required by the host pipeline
builder.Services.AddStackExchangeRedisCache(o =>                    // REQUIRED: IDistributedCache for the session store
	o.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddKeycloakBlazor(builder.Configuration);           // section "Keycloak"
builder.Services.AddHttpClient("OrdersApi", c => c.BaseAddress = new Uri("https://orders.example.com"))
	.AddKeycloakHandler();                                          // Bearer token from the user's session
```
`AddKeycloakBlazor` registers: `KeycloakOptions`, `IHttpContextAccessor`, named `HttpClient` `"KeycloakAuthService"`,
scoped `IBlazorTokenSessionStore → RedisBlazorTokenSessionStore`, scoped `KeycloakAuthService` exposed as both
`IBlazorKeycloakAuthService` and `IKeycloakAuthService`, scoped `AuthenticationStateProvider → KeycloakAuthStateProvider`,
transient `KeycloakAuthorizationMessageHandler`, `AddAuthorizationCore()`.
(Both Blazor and MAUI packages define `AddKeycloakHandler` — in namespace `…Blazor.Extensions` vs `…Maui.Extensions`; import only one.)

Keycloak client: **public** client (no secret is sent), Standard Flow + PKCE S256, Valid Redirect URI
`https://<app>/auth/callback`, Valid Post Logout Redirect URI `https://<app>` (base URI), web origin.
Set `Keycloak:Audience` only if access tokens carry that audience (validated when non-empty).

## UI wiring

`Routes.razor`: wrap in `<CascadingAuthenticationState>` (or `AddCascadingAuthenticationState()`).

`MainLayout.razor` — restore the session for every circuit:
```razor
@inject IBlazorKeycloakAuthService Auth
@code {
	protected override async Task OnInitializedAsync() => await Auth.InitialiseFromCookieAsync();
}
```
Login / logout:
```razor
<AuthorizeView>
	<Authorized>Hello @context.User.Identity?.Name <button @onclick="Logout">Log out</button></Authorized>
	<NotAuthorized><button @onclick="Login">Log in</button></NotAuthorized>
</AuthorizeView>
@code {
	Task Login()  => Auth.LoginAsync();    // no returnUrl parameter: the current Nav.Uri is used
	Task Logout() => Auth.LogoutAsync();
}
```
**You must create the `/auth/callback` page in the app.** The repo contains `KeycloakCallback.razor`, but the project uses
`Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Razor`), so the page is **not compiled into the package** (verified by building
it — the DLL has no `KeycloakCallback` type). Use it as a template:
```razor
@page "/auth/callback"
@inject IBlazorKeycloakAuthService Auth
@inject NavigationManager Nav
@code {
	[SupplyParameterFromQuery(Name = "code")]  private string? Code  { get; set; }
	[SupplyParameterFromQuery(Name = "state")] private string? State { get; set; }

	protected override async Task OnInitializedAsync()
	{
		if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(State)) { Nav.NavigateTo("/", forceLoad: true); return; }
		var returnUrl = await Auth.HandleCallbackAsync(Code, State);   // null on failure
		Nav.NavigateTo(returnUrl ?? "/auth/login-failed", forceLoad: true);
	}
}
```
Also add `/auth/login-failed` (and optionally `/auth/logout` calling `Auth.LogoutAsync()`). If you fix the package by switching
its SDK to `Microsoft.NET.Sdk.Razor`, consumers must then add the assembly to the router's `AdditionalAssemblies` and remove
their own `/auth/callback` page to avoid an ambiguous route — treat that as a breaking change.

## How the flow works (`Services/KeycloakAuthService`)

- `LoginAsync()`: generates a 32-byte session id, PKCE verifier/challenge, `state`, `nonce`; saves a *flow* `BlazorTokenSession` in Redis (TTL 10 min) with `ReturnUrl = Nav.Uri`; sets cookie `kc_session` (session cookie) **if the response hasn't started** (in an interactive circuit it usually has — that's why the session id is also embedded in `state` as `"{state}.{sessionId}"`); `NavigateTo(authUrl, forceLoad: true)`.
- `HandleCallbackAsync(code, returnedState)` → `string?` return URL or `null` on any failure: splits state, loads the flow session, compares `state` (mismatch → removes session, logs possible CSRF), exchanges the code (public client + verifier), validates the **ID token signature + nonce** and the **access token signature/issuer/lifetime (+audience if set)** against JWKS from the discovery document, stores tokens, clears flow fields, sets a **persistent** `kc_session` cookie (1 day, Secure, HttpOnly, SameSite=Lax), raises `AuthStateChanged`.
- `InitialiseFromCookieAsync()` / `TryRestoreSessionAsync()`: reads `kc_session` → loads session → if refreshable, **forces a refresh** (detects sessions ended in Keycloak; on failure clears Redis + cookie) → else if unexpired, trusts it → else clears.
- `GetAccessTokenAsync()` returns the cached token or refreshes (serialized by a semaphore); throws `UnauthorizedAccessException` when not logged in / not refreshable.
- `LogoutAsync()`: clears state, removes the Redis session, revokes the refresh token (errors ignored), deletes the cookie if possible, then redirects to Keycloak end-session with `id_token_hint` and `post_logout_redirect_uri = BaseUri` (or `/`).
- Redis session TTL: until refresh-token expiry if known, else access-token expiry + 1 h, else 10 min. Keys: `blazor:session:{id}` (no instance prefix unless you set one on `AddStackExchangeRedisCache`).

`KeycloakAuthStateProvider` (namespace oddly `Pivot.Framework.Authentication.Maui.AuthenticationStateProvider`) mirrors
`AuthStateChanged` into Blazor's `AuthenticationState`.

## Pitfalls
- Cookies can only be written during the initial HTTP request (prerender), not over the SignalR circuit, and `IHttpContextAccessor.HttpContext` is only reliable during that request — in an interactive circuit it can be null, and then `InitialiseFromCookieAsync` finds no session. Make sure it runs during prerender/static render (or capture the `kc_session` value there and pass it into the circuit). Token refreshes are persisted to Redis, so no cookie write is needed after login.
- Behind a reverse proxy, `NavigationManager.BaseUri` must be the public URL (use `UseForwardedHeaders`) or the redirect URI won't match Keycloak's allowed list.
- Multiple server instances: all must share the same Redis and Data Protection is not involved (cookie is an opaque id) — just ensure sticky sessions for Blazor Server circuits.
- `RequireHttpsMetadata: false` needed for a local HTTP Keycloak.
- `KeycloakTokenStorage` (browser `localStorage` via JS interop) exists in `Storage/` but is not registered — don't use it for Blazor Server (tokens would leave the server).

## Changing this package
Security-sensitive: keep state/nonce/PKCE validation, token signature validation and HttpOnly/Secure cookie flags. Any change to
`IKeycloakAuthService` must be mirrored in the MAUI implementation. No test project exists — if you add one, abstract
`NavigationManager` navigation and the token endpoint via a stub `HttpMessageHandler` (see `Tests/Pivot.Framework.Authentication.Tests/TestDoubles`).
