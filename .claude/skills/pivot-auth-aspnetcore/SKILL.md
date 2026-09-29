---
name: pivot-auth-aspnetcore
description: Pivot.Framework.Authentication.AspNetCore — Keycloak JWT bearer protection for ASP.NET Core APIs. Use when securing an API with AddKeycloakAuthentication and its fluent options (WithCurrentUser, WithSwagger, WithRedisTokenCaching, AddRegistration), configuring Swagger UI OAuth2/PKCE against Keycloak, adding role/claim/scope authorization policies, registering the IdP services and in-memory auth sessions, writing tests with AuthenticationTestContextFactory, diagnosing 401/403s, or changing Src/Containers/Authentication/Pivot.Framework.Authentication.AspNetCore.
---

# Pivot.Framework.Authentication.AspNetCore

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication.AspNetCore`. References Authentication core,
`Swashbuckle.AspNetCore` 10 (Microsoft.OpenApi v2 API), `Microsoft.Extensions.Http`.

## Main entry point

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration, o => o
	.WithCurrentUser()                              // ICurrentUser + IHttpContextAccessor
	.WithSwagger("Orders API", "v1")                // AddEndpointsApiExplorer + SwaggerGen with Keycloak OAuth2
	.WithRedisTokenCaching());                      // from Pivot.Framework.Authentication.Caching (pivot-auth-caching)

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(ui => ui.UseKeycloakOAuth(app.Services));  // client id, scopes, PKCE
```
`KeycloakAuthenticationExtensions.AddKeycloakAuthentication(services, configuration, configure?)`:
1. Always — `RegisterCoreJwtBearer`: binds + validates `KeycloakOptions` (throws if section missing or invalid), `AddAuthentication(JwtBearer)` +
   `AddJwtBearer` with `Authority = IssuerUrl`, `MetadataAddress`, `RequireHttpsMetadata`, and `TokenValidationParameters`:
   issuer = `IssuerUrl`, **audience = `Keycloak:Audience` (validated)**, lifetime validated, `ClockSkew = 30 s`,
   `RoleClaimType = ClaimTypes.Role`, `NameClaimType = "preferred_username"`. Events: `OnTokenValidated` flattens Keycloak roles;
   `OnAuthenticationFailed` logs a warning. Also `AddAuthorization()`.
2. `WithCurrentUser()` → scoped `ICurrentUser → CurrentUser`.
3. `WithSwagger(title, version)` → SwaggerDoc + `AddKeycloakSecurityDefinition(configuration)` + `AddKeycloakSecurityRequirement()` (global requirement on scheme `"oauth2"`).
4. Each `AddRegistration((services, config) => …)` action — the extension point other packages use (e.g. Redis caching).

`KeycloakAuthenticationOptions` flags are `internal`; only the fluent methods are public. `AddKeycloakBackend` exists but is
**internal** (README is outdated). The Containers.API overload `AddKeycloakAuthentication(config, swaggerTitle, version)` enables
all three options at once.

To tweak `JwtBearerOptions` further, post-configure: 
```csharp
services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o => o.TokenValidationParameters.ValidateAudience = false);
```
(`WithRedisTokenCaching` replaces `Events` with `EventsType = KeycloakRedisJwtEvents`, which also flattens roles.)
For SignalR hubs, add `OnMessageReceived` reading `access_token` from the query string via PostConfigure — not built in.

## Swagger helpers (`SwaggerKeycloakExtensions`)
- `AddKeycloakSecurityDefinition(this SwaggerGenOptions, IConfiguration | IServiceProvider)` — OAuth2 authorization-code flow with `AuthorizationUrl`, `TokenUrl`, scopes from `Keycloak:Scopes`.
- `AddKeycloakSecurityRequirement()` — applies scheme `SecuritySchemeName = "oauth2"` to all operations.
- `UseKeycloakOAuth(this SwaggerUIOptions, IServiceProvider)` — sets `ClientId`, space scope separator, scopes, `UsePkceWithAuthorizationCodeGrant = true`.
Keycloak client for Swagger: public client, standard flow enabled, redirect URI `https://<api>/swagger/oauth2-redirect.html`, web origin for CORS.

## Authorization policies (`AuthorizationPolicyExtensions`)

```csharp
builder.Services.AddAuthorization(o => o
	.AddRolePolicy("Admins", "admin")                          // RequireRole(any of roles)
	.AddClaimPolicy("Premium", "subscription", "gold", "platinum")  // RequireClaim(type[, values]); no values = claim must exist
	.AddScopePolicy("OrdersWrite", "orders:write"));           // all scopes must be present in "scope"/"scp" (space-separated)
```
Use with `[Authorize(Policy = "Admins")]` or `.RequireAuthorization("OrdersWrite")`. Roles are flattened realm + client roles (see `pivot-auth-core`).

## IdP services and sessions (`KeycloakIdentityProviderExtensions`)

```csharp
services.AddKeycloakIdentityProviderServices(configuration);   // typed HttpClients for IIdentityProviderAuthService, ITokenIntrospectionService, ITokenRevocationService, IIdentityProviderAdminService
services.AddInMemoryAuthSessions();                            // IAuthSessionStore → InMemoryAuthSessionStore (singleton)
```
Used by `pivot-auth-api`, or directly (e.g. user provisioning via `IIdentityProviderAdminService`).

## Testing helpers (`Testing/AuthenticationTestContextFactory`)

```csharp
var principal = AuthenticationTestContextFactory.CreatePrincipal(
	subjectId: Guid.NewGuid().ToString(), username: "alice", email: "alice@example.com", roles: ["admin"]);
var http = AuthenticationTestContextFactory.CreateHttpContext(principal);
var accessor = Substitute.For<IHttpContextAccessor>();
accessor.HttpContext.Returns(http);
var currentUser = new CurrentUser(accessor);
currentUser.IsInRole("admin").Should().BeTrue();
```
Claims produced: `ClaimTypes.NameIdentifier`, `preferred_username`, `ClaimTypes.Email`, `ClaimTypes.Role`, plus extras; authentication type `"TestAuthentication"` (so `IsAuthenticated` is true). Ships in the production package on purpose — keep it dependency-free.

## Diagnosing 401 / 403
- 401 + log "Keycloak authentication failed: IDX10214 audience" → token lacks `aud = Keycloak:Audience`: add an *Audience* mapper to the client scope in Keycloak, or change `Audience`.
- IDX10205 issuer → `BaseUrl`/`Realm` differ from the token's `iss` (e.g. internal vs public hostname — Keycloak `KC_HOSTNAME`).
- Metadata fetch fails in dev over HTTP → `RequireHttpsMetadata: false`.
- 403 with a valid token → role not in `realm_access`/`resource_access` (check mappers), or policy requires a scope not granted.
- With Redis caching enabled: revoked tokens fail with "Token has been revoked." (see `pivot-auth-caching`).

## Changing this package
- New optional features follow the pattern: an `internal` `RegisterXxx` + a public `WithXxx()` on `KeycloakAuthenticationOptions`, or an `AddRegistration` hook from another package (that's how Caching plugs in without a circular reference).
- Tests: `Tests/Pivot.Framework.Authentication.Tests/Extensions` and `/Testing`.
