---
name: pivot-caching-redis
description: Pivot.Framework.Infrastructure.Caching — the ICacheService abstraction over Redis IDistributedCache. Use when adding cache-aside reads, caching DTOs in Redis, configuring AddRedisCache (connection string, instance-name key prefix), choosing cache keys/TTLs, or modifying Src/Infrastructure/Pivot.Framework.Infrastructure.Caching (which the Keycloak token cache also builds on).
---

# Pivot.Framework.Infrastructure.Caching

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Caching`. Dependencies: `Microsoft.Extensions.Caching.StackExchangeRedis`,
`Microsoft.Extensions.Options.ConfigurationExtensions`. No other Pivot reference — it is reused by `pivot-auth-caching`.

## API

```csharp
namespace Pivot.Framework.Infrastructure.Caching.Abstractions;
public interface ICacheService
{
	Task RemoveAsync(string key, CancellationToken ct = default);
	Task<bool> ExistsAsync(string key, CancellationToken ct = default);
	Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
	Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, CancellationToken ct = default) where T : class;
}
```
Implementation `RedisCacheService` is `internal sealed`, registered as a **singleton**:
- Serializes with System.Text.Json: camelCase, `WhenWritingNull` ignored. Types must round-trip with STJ (public setters or a constructor matching property names; `private set` properties are **not** populated unless annotated `[JsonInclude]`).
- `SetAsync` uses `AbsoluteExpirationRelativeToNow = absoluteExpiry` (no sliding expiration).
- `GetAsync` returns `null` on miss/empty; `ExistsAsync` does a full GET (no `EXISTS` command).
- `T : class` — wrap value types (`record CountDto(int Value)`).

## Registration

```csharp
builder.Services.AddRedisCache(builder.Configuration);                               // ConnectionStrings:Redis
builder.Services.AddRedisCache(builder.Configuration, "redis:6379,password=…");      // explicit
builder.Services.AddRedisCache(builder.Configuration, instanceName: "orders:");      // key prefix
```
- Connection string order: explicit argument → `ConnectionStrings:Redis` → `InvalidOperationException`.
- `instanceName` defaults to **`"TemplatesCore:"`** (legacy name) and is prefixed to every key by StackExchangeRedis. Set a service-specific prefix when several services share a Redis instance.
- Calls `AddStackExchangeRedisCache`, so `IDistributedCache` is also available (the Blazor auth session store uses it directly).
- Calling it twice (e.g. once directly and once via `WithRedisTokenCaching()`) registers `ICacheService` twice; the last registration wins and the Redis options are configured twice (last instance name wins) — call it once or make sure the parameters agree.

## Cache-aside pattern

```csharp
public sealed class GetProductQueryHandler(ICacheService cache, IReadModelRepository<ProductDto, Guid> repo)
	: IQueryHandler<GetProductQuery, ProductDto>
{
	public async Task<Result<ProductDto>> Handle(GetProductQuery q, CancellationToken ct)
	{
		var key = $"product:{q.Id:N}";
		var cached = await cache.GetAsync<ProductDto>(key, ct);
		if (cached is not null)
			return cached;

		var product = await repo.GetByIdAsync(q.Id, ct);
		if (product is null)
			return Result.Failure<ProductDto>(new Error("Product.NotFound", "Product not found."), ResultExceptionType.NotFound);

		await cache.SetAsync(key, product, TimeSpan.FromMinutes(10), ct);
		return product;
	}
}
```
Invalidation: remove keys in the projection/event handler that changes the data (`ProjectionHandler`, integration
event handler) — not in the command handler before the outbox has been drained.

Key conventions: `entity:{id}` lower-case, colon-separated; never put secrets/tokens in keys (the auth cache hashes tokens with SHA-256 for this reason).

## Changing this package

- Keep it provider-thin and dependency-free of other Pivot packages (auth caching depends on it, not the reverse).
- If you add features (sliding expiry, `GetOrSetAsync`, bulk ops), extend the interface carefully — `pivot-auth-caching` and consumers implement/mock it. Prefer default interface methods or a new interface.
- `RedisCacheService` is internal; tests would need `InternalsVisibleTo` or go through DI. No test project exists yet.
