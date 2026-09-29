---
name: pivot-readstore-mongodb
description: Pivot.Framework.Infrastructure.ReadStore.MongoDB — MongoDB-backed IReadModelRepository/IReadModelStore for CQRS read models. Use when storing projections/read models in MongoDB, registering AddMongoReadModelStore, writing ReadModelSpecifications that run on Mongo, mapping read-model Ids/BSON, or changing Src/Infrastructure/Pivot.Framework.Infrastructure.ReadStore.MongoDB.
---

# Pivot.Framework.Infrastructure.ReadStore.MongoDB

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.ReadStore.MongoDB`. References Application + `MongoDB.Driver` 3.7.
Implements the read-model contracts from `pivot-application` (§5).

## Registration

```csharp
services.AddMongoReadModelStore(
	configuration["MongoDB:ConnectionString"]!,
	configuration["MongoDB:DatabaseName"]!);

// or with full driver settings (TLS, timeouts, …)
var settings = MongoClientSettings.FromConnectionString(cs);
settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
services.AddMongoReadModelStore(settings, "orders_read");
```
Registers: singleton `IMongoClient`, singleton `IMongoDatabase`, **scoped open generics**
`IReadModelRepository<,> → MongoReadModelRepository<,>` and `IReadModelStore<,> → MongoReadModelStore<,>`.
Throws `ArgumentException` for blank connection string / database name. There is no `IConfiguration` overload
(README's `AddMongoReadModel<T,TId>(configuration)` does not exist).

Do not combine with `AddEfCoreReadModelStore<T>()` in the same container — both register the same open generics
and the last one wins. If you need both, register closed generics explicitly for the Mongo-backed models.

## Collections and documents

- Collection name = `typeof(TReadModel).Name` (e.g. `OrderSummary`). Not configurable — subclass the repository/store and override if you need another name, or rename the type.
- Filter by id uses `Builders<T>.Filter.Eq(x => x.Id, id)` — make `Id` the BSON `_id`: a property named `Id` maps to `_id` by driver convention. For `Guid` ids configure a representation once at startup (driver 3.x requires it):
  ```csharp
  BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
  ```
- `ReadModel<TId>` has a `protected set` on `Id`; whether the driver's auto class map populates it depends on the mapping conventions in use — verify with a round-trip, or simply implement `IReadModel<TId>` with `public` setters (as in the example below).
- Create indexes yourself (e.g. an `IHostedService` calling `collection.Indexes.CreateOneAsync`) — the framework creates none.

## Behaviour

| Method | Mongo operation |
|---|---|
| `GetByIdAsync(id)` | `Find(_id == id).FirstOrDefault` |
| `GetAllAsync(predicate?)` | `Find(predicate or Empty)` → list (unbounded — use specs for paging) |
| `ExistsAsync(predicate)` | `Find(predicate).Any` |
| `CountAsync(null)` | `EstimatedDocumentCountAsync` (metadata-based, approximate) |
| `CountAsync(predicate)` | `CountDocumentsAsync` (clamped to `int.MaxValue`) |
| `ListAsync(spec)` | LINQ over `AsQueryable()` via `MongoReadModelSpecificationEvaluator` |
| `UpsertAsync(model)` | `ReplaceOneAsync(_id, model, IsUpsert = true)` — whole-document replace, last write wins |
| `DeleteAsync(id)` | `DeleteOneAsync(_id)` |

`MongoReadModelSpecificationEvaluator` applies Criteria, OrderBy/OrderByDescending, Skip/Take. **ThenBy expressions are ignored.** Criteria/order expressions must be translatable by the Mongo LINQ provider (no custom methods).

Upsert is a full replace: projections must build the complete document (load → modify → upsert), and concurrent
projections of the same id can overwrite each other. For partial updates or optimistic concurrency, subclass
`MongoReadModelStore` (the `Collection` field is `protected`) and use `UpdateOneAsync` with a version filter.

## Example projection → Mongo

```csharp
public sealed class OrderSummary : IReadModel<Guid>
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;
	public decimal Total { get; set; }
}

internal sealed class OrderShippedProjection(
	IReadModelRepository<OrderSummary, Guid> repo,
	IReadModelStore<OrderSummary, Guid> store) : ProjectionHandler<OrderShippedDomainEvent>
{
	public override async Task ProjectAsync(OrderShippedDomainEvent e, CancellationToken ct)
	{
		var summary = await repo.GetByIdAsync(e.OrderId, ct) ?? new OrderSummary { Id = e.OrderId };
		summary.Status = "Shipped";
		await store.UpsertAsync(summary, ct);
	}
}
```

## Changing this package
Keep the classes generic and virtual (all public methods are `virtual`, `Collection` is `protected`). `MongoCollectionResolver` is internal — if you make collection naming configurable, do it via an injectable naming strategy with the current behaviour as default. No test project exists; driver behaviour is best tested with Testcontainers MongoDB.
