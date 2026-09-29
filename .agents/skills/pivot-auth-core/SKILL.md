---
name: pivot-auth-core
description: Pivot.Framework.Authentication (core) — shared Keycloak/OIDC building blocks used by every auth package. Use when working with KeycloakOptions and the "Keycloak" config section, ICurrentUser/CurrentUser claims mapping, KeycloakRoleHelper role flattening, IKeycloakAuthService (client-side login/refresh contract) and KeycloakAuthorizationMessageHandler, KeycloakTokenSet/IKeycloakTokenStorage, the provider-neutral IdP services (IIdentityProviderAuthService, IIdentityProviderAdminService, ITokenIntrospectionService, ITokenRevocationService) and IAuthSessionStore, or when changing Src/Containers/Authentication/Pivot.Framework.Authentication.
---

# Pivot.Framework.Authentication (core)

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication`. Dependencies: `Microsoft.AspNetCore.Authentication.JwtBearer`,
`Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Options`. Has **no DI extension of its own** —
host-specific packages register its types: `pivot-auth-aspnetcore` (APIs), `pivot-auth-caching`, `pivot-auth-api`,
`pivot-auth-blazor`, `pivot-auth-maui`, `pivot-auth-hangfire`.

Namespaces: `Pivot.Framework.Authentication` (`KeycloakOptions`, `KeycloakTokenSet`), `.Models`, `.Services`
(note: `IKeycloakAuthService` physically lives in the misspelled folder `Sercvices/` but its namespace is `.Services`),
`.Handlers`, `.Helpers`, `.Storage`, `.Events`, `.Responses`.

## KeycloakOptions (section `"Keycloak"`)

```json
"Keycloak": {
  "BaseUrl": "https://auth.example.com",
  "Realm": "my-realm",
  "ClientId": "orders-api",
  "ClientSecret": null,
  "AdminClientId": null,
  "AdminClientSecret": null,
  "Audience": "orders-api",
  "Scopes": "openid profile email offline_access",
  "RequireHttpsMetadata": true
}
```
- `Validate()` requires `BaseUrl`, `Realm`, `ClientId` (throws `InvalidOperationException` naming the missing key). Called by JWT registration, Blazor/MAUI services and the admin service ctor.
- Derived URLs: `IssuerUrl` = `{BaseUrl}/realms/{Realm}`, `TokenUrl`, `AuthorizationUrl`, `LogoutUrl`, `RevocationUrl`, `UserInfoUrl`, `IntrospectionUrl`, `MetadataUrl` (`/.well-known/openid-configuration`), `AdminBaseUrl` = `{BaseUrl}/admin/realms/{Realm}`.
- `EffectiveAdminClientId/Secret` fall back to `ClientId/ClientSecret` when admin values are blank.
- `Audience` is **validated** by the API JWT bearer (`ValidateAudience = true`) — configure a Keycloak audience mapper so access tokens contain it, otherwise every request is 401. Blazor/MAUI only validate audience when it's non-empty.

## ICurrentUser (`Models/ICurrentUser`, impl `CurrentUser`)

```csharp
Guid? UserId            // ClaimTypes.NameIdentifier or "sub", parsed as Guid (null if not a Guid)
string? Email           // ClaimTypes.Email or "email"
string? Username        // "preferred_username" or ClaimTypes.Name
string? DisplayName     // "name" → "given_name family_name" → Username
bool IsAuthenticated
ClaimsPrincipal? Principal
IReadOnlyList<string> Roles   // ClaimTypes.Role values (after flattening)
bool IsInRole(string role)
```
`CurrentUser` reads `IHttpContextAccessor.HttpContext.User` — scoped, HTTP-only. It is **not** the audit actor source:
audit stamping uses `ICurrentUserProvider` (Application), whose default returns `Identity.Name` (= `preferred_username`
with the framework's JWT setup). If you want audit fields to hold the user id, register an `ICurrentUserProvider` that
wraps `ICurrentUser.UserId`.

## Role flattening (`Helpers/KeycloakRoleHelper.FlattenRoles(identity, logger?)`)
Keycloak puts roles in JSON claims `realm_access.roles` and `resource_access.{client}.roles`. The helper adds each as a
`ClaimTypes.Role` claim so `[Authorize(Roles = "admin")]`, `IsInRole` and policies work. **Client roles from all clients
are merged without a prefix** — a client role named like a realm role is indistinguishable. Invalid JSON is logged at
Debug and ignored. Used by the API JWT events, the Redis JWT events (own copy), Blazor and MAUI.

## Client-side contract: IKeycloakAuthService

```csharp
bool IsAuthenticated { get; }
ClaimsPrincipal? User { get; }
event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;   // (IsAuthenticated, User)
Task<bool> LoginAsync(CancellationToken ct = default);
Task LogoutAsync(CancellationToken ct = default);
Task<string> GetAccessTokenAsync(CancellationToken ct = default);  // refreshes if expired; throws UnauthorizedAccessException if not possible
Task<string> ForceRefreshAsync(CancellationToken ct = default);
Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
```
Implemented by the Blazor and MAUI packages. `KeycloakTokenSet` (Access/Refresh/Id tokens, `ExpiresAt`,
`RefreshTokenExpiresAt`, `IsExpired` with 30 s skew, `CanRefresh`) + `IKeycloakTokenStorage` (Get/Save/Clear) back the MAUI flow
(Blazor has a `localStorage`-based `KeycloakTokenStorage` too, unused by its default registration).

`KeycloakAuthorizationMessageHandler` (`DelegatingHandler`, register transient + `.AddKeycloakHandler()` on an `IHttpClientBuilder`):
skips requests that already have `Authorization`; otherwise attaches `Bearer {GetAccessTokenAsync()}` (sends anonymously if
that throws `UnauthorizedAccessException`); on **401** with a token it calls `ForceRefreshAsync`, clones the request (buffers the body)
and retries **once**. Not for server-to-server calls (no client-credentials support).

## Provider-neutral IdP services (server side)

Registered by `services.AddKeycloakIdentityProviderServices(configuration)` (package AspNetCore) as typed `HttpClient`s:

| Interface | Keycloak implementation | Notes |
|---|---|---|
| `IIdentityProviderAuthService` | `KeycloakIdentityProviderAuthService` | `BuildAuthorizationUrlAsync(AuthAuthorizationRequest)` (no HTTP call; uses `Scopes` if none given; PKCE params included only if `CodeChallenge` set), `ExchangeAuthorizationCodeAsync`, `RefreshTokenAsync`, `LogoutAsync` (back-channel logout with refresh token/id_token_hint), `GetUserProfileAsync(accessToken)` (userinfo → `IdentityProviderUser`, string claims only) |
| `ITokenIntrospectionService` | `KeycloakTokenIntrospectionService` | RFC 7662 with admin (or main) client credentials → `TokenIntrospectionResult { IsActive, SubjectId, Username, ClientId, ExpiresAt, Scopes, Roles = [] }` |
| `ITokenRevocationService` | `KeycloakTokenRevocationService` | RFC 7009 `RevokeTokenAsync(token, hint?)` |
| `IIdentityProviderAdminService` | `KeycloakIdentityProviderAdminService` | Admin REST: get user by id/email, list/search users, create (returns id from `Location`), update, assign/remove **realm** roles. Gets a client-credentials token **per call** (no caching). Requires a confidential client with `realm-management` roles (`view-users`, `manage-users`). |

All throw `HttpRequestException` (`EnsureSuccessStatusCode`) on IdP errors — they do not return `Result`. Wrap them in
application services that translate to `Result` where appropriate. `client_secret` is only sent when configured (public
clients work for code exchange/refresh).

Models (`Models/`): `AuthAuthorizationRequest/Result`, `AuthCodeExchangeRequest`, `AuthRefreshTokenRequest`, `AuthLogoutRequest`,
`AuthTokenResponse` (absolute `ExpiresAt`/`RefreshTokenExpiresAt`), `AuthSession`, `IdentityProviderUser/Role/Claim`,
`Create/UpdateIdentityProviderUserRequest`, `TokenIntrospectionResult`. `Responses/KeycloakTokenResponse` is the raw
snake_case token endpoint DTO used by Blazor/MAUI.

`Storage/IAuthSessionStore` (Get/Save/Remove `AuthSession` by `SessionId`) with `InMemoryAuthSessionStore`
(`AddInMemoryAuthSessions()`) — process-local, lost on restart, not shared across replicas; implement a Redis/DB store for production.

## Changing this package
- It must stay host-agnostic (no Blazor, MAUI, Swagger references). Anything touching `HttpContext` beyond `IHttpContextAccessor` belongs in AspNetCore.
- Keep `IKeycloakAuthService` stable — Blazor and MAUI both implement it; the message handler depends on its exception semantics (`UnauthorizedAccessException` = no token).
- Tests: `Tests/Pivot.Framework.Authentication.Tests` (`Services` using `TestDoubles/StubHttpMessageHandler` to fake Keycloak responses, `Storage`, `Extensions`, `API`, `Testing`).
