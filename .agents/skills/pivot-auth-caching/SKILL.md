---
name: pivot-auth-caching
description: Pivot.Framework.Authentication.Caching — Redis-backed JWT claims cache and token revocation for Keycloak-protected APIs. Use when enabling WithRedisTokenCaching(), implementing logout/immediate token revocation or "log out everywhere" (ITokenRevocationCache.RevokeAsync/RevokeAllForUserAsync), configuring TokenRevocation:RevokeAllTtl, understanding KeycloakRedisJwtEvents, or changing Src/Containers/Authentication/Pivot.Framework.Authentication.Caching.
---

# Pivot.Framework.Authentication.Caching

Location: `Src/Containers/Authentication/Pivot.Framework.Authentication.Caching`. References Authentication core,
Authentication.AspNetCore, Infrastructure.Caching (`ICacheService`), `StackExchangeRedis`, `JwtBearer`.

## Enabling

```csharp
builder.Services.AddKeycloakAuthentication(builder.Configuration, o => o
	.WithCurrentUser()
	.WithRedisTokenCaching());
```
```json
"ConnectionStrings": { "Redis": "localhost:6379" },
"TokenRevocation": { "RevokeAllTtl": "30.00:00:00" }
```
`WithRedisTokenCaching()` (public, on `KeycloakAuthenticationOptions`) adds a registration hook that runs the internal
`AddKeycloakAuthenticationCaching(configuration)`:
- `AddRedisCache(configuration, null, "TemplatesCore:")` → `ICacheService` (see `pivot-caching-redis`; key prefix `TemplatesCore:`).
- `TokenRevocationOptions` from `"TokenRevocation"`: only `RevokeAllTtl` (TimeSpan, default 30 days; must be > 0, validated when the revocation cache is first resolved). Set it to the realm's **SSO Session Max** so a "revoke all" outlives every token issued before it. (README's `DefaultTtlDays` does not exist.)
- Singletons `IDistributedTokenCache → RedisDistributedTokenCache`, `ITokenRevocationCache → RedisTokenRevocationCache`, `KeycloakRedisJwtEvents`.
- `ICurrentUser`, `IHttpContextAccessor`, `KeycloakOptions`, `AddAuthorization()`.
- `PostConfigure<JwtBearerOptions>(Bearer, o => o.EventsType = typeof(KeycloakRedisJwtEvents))` — **replaces** the core `OnTokenValidated`/`OnAuthenticationFailed` events with this class.

## What happens per request (`KeycloakRedisJwtEvents.TokenValidated`)
Runs only after the JWT signature/issuer/audience/lifetime were validated by the bearer handler.
1. Raw token from `JsonWebToken.EncodedToken` (or the `Authorization` header).
2. `IsRevokedAsync(token)` → fail "Token has been revoked."
3. If `sub` is a Guid and `iat` known: `IsIssuedBeforeRevocationAsync(sub, iat)` → fail "All tokens for this user have been revoked."
4. Claims cache hit → principal **rebuilt from cache** (identity type `keycloak`, name `preferred_username`, role `ClaimTypes.Role`): `NameIdentifier`, `sub`, `preferred_username`, `Email`, roles, plus all other original claim types.
5. Miss → flatten roles on the validated identity, snapshot to `CachedTokenClaims { UserId, Username, Email, Roles, AllClaims }` with TTL = token expiry.

Cost trade-off: it still does signature validation every request; the cache saves claim parsing/flattening and gives
revocation. Revocation checks add 1–2 Redis round-trips per request.

## Redis keys (all behind the `TemplatesCore:` instance prefix)
| Key | Value | TTL |
|---|---|---|
| `tkn:claims:{SHA256(token)}` | `CachedTokenClaims` JSON | until token `exp` |
| `tkn:revoked:{SHA256(token)}` | sentinel | until token `exp` |
| `tkn:revoke-all:{userId:D}` | `{ RevokedAtUnix }` | `RevokeAllTtl` |
Tokens are never stored in clear text.

## Revoking tokens

```csharp
app.MapPost("/auth/logout", async (HttpContext ctx, ITokenRevocationCache revocation, CancellationToken ct) =>
{
	var token = ctx.Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim();
	var exp = new JsonWebToken(token).ValidTo;                              // Microsoft.IdentityModel.JsonWebTokens
	await revocation.RevokeAsync(token, new DateTimeOffset(exp, TimeSpan.Zero), ct);
	return Results.NoContent();
}).RequireAuthorization();

// "log out of all devices" / account compromised:
await revocation.RevokeAllForUserAsync(userId, ct);   // rejects every token with iat < now for this sub
```
- `RevokeAsync(token, tokenExpiresAt)` is a no-op for already-expired tokens and also deletes the cached claims.
- Revocation is local to services sharing the same Redis (and prefix). It does not revoke the session in Keycloak — also call `ITokenRevocationService`/Keycloak logout for refresh tokens (see `pivot-auth-api`).
- `IDistributedTokenCache.InvalidateAsync(token)` drops only the cached claims (e.g. after role changes, forcing re-parse on next request).
- `RevokeAllForUserAsync` compares whole seconds with a strict `iat < revokedAt`, so a token whose `iat` falls in the same second as the revocation is **not** rejected.

## Changing this package
- It plugs into AspNetCore only through `KeycloakAuthenticationOptions.AddRegistration` — keep that direction (AspNetCore must not reference Caching).
- `KeycloakRedisJwtEvents` duplicates role-flattening logic from `KeycloakRoleHelper`; prefer calling the helper if you touch it.
- Redis classes are `internal sealed`; no dedicated tests exist — add them via `InternalsVisibleTo` with a substituted `ICacheService`.
