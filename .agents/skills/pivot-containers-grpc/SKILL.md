---
name: pivot-containers-grpc
description: Pivot.Framework.Containers.Grpc — gRPC server hosting aligned with the framework. Use when exposing a service over gRPC (AddPivotGrpc, MapPivotGrpcService), adding transactional unary/streaming calls (AddPivotGrpcTransactions<TContext>), converting Result failures to RpcException (GetValueOrThrow/ThrowIfFailure, IGrpcResultStatusMapper), customising exception→status mapping or validation trailers, configuring .proto build conventions, or changing Src/Containers/Pivot.Framework.Containers.Grpc.
---

# Pivot.Framework.Containers.Grpc

Location: `Src/Containers/Pivot.Framework.Containers.Grpc`. References Domain, Application, Infrastructure.Abstraction,
`Grpc.AspNetCore` 2.76, EF Core; `FrameworkReference Microsoft.AspNetCore.App`. Ships `buildTransitive` props/targets.

## Registration

```csharp
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>();   // provides ITransactionManager<AppDbContext>
services.AddPivotGrpcTransactions<AppDbContext>();                   // includes AddPivotGrpc()
// or, without transactions:
services.AddPivotGrpc(o => o.EnableDetailedErrors = env.IsDevelopment());

app.MapPivotGrpcService<OrdersGrpcService>();                        // = MapGrpcService<T>()
```
`AddPivotGrpc(configure?)`: `AddGrpc` with global `GrpcExceptionInterceptor` (+ your options) and TryAdd singletons
`IGrpcValidationStatusMapper`, `IGrpcResultStatusMapper`, `IGrpcExceptionStatusMapper` (defaults), scoped interceptor.
`AddPivotGrpcTransactions<T>(configure?)`: calls `AddPivotGrpc()`, TryAdd-registers the options instance (first call wins),
and appends `GrpcTransactionInterceptor<T>`. Interceptors run in registration order, so the exception interceptor is
**outermost** and the transaction interceptor sees raw exceptions. Calling `AddPivotGrpc` yourself as well adds the exception
interceptor twice (harmless but redundant) — call one or the other.

## Service implementation pattern

```csharp
public sealed class OrdersGrpcService(ISender sender, IGrpcResultStatusMapper statusMapper)
	: Orders.OrdersBase
{
	public override async Task<GetOrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
	{
		var result = await sender.Send(new GetOrderQuery(Guid.Parse(request.Id)), context.CancellationToken);
		var dto = result.GetValueOrThrow(statusMapper);        // RpcException on failure
		return new GetOrderReply { Id = dto.Id.ToString(), Status = dto.Status };
	}

	public override async Task<Empty> ShipOrder(ShipOrderRequest request, ServerCallContext context)
	{
		var result = await sender.Send(new ShipOrderCommand(Guid.Parse(request.Id)), context.CancellationToken);
		result.ThrowIfFailure(statusMapper);
		return new Empty();
	}
}
```

## Status mapping

`DefaultGrpcResultStatusMapper` (for `Result` failures, by `ResultExceptionType`):
| ResultExceptionType | StatusCode |
|---|---|
| ValidationError | InvalidArgument |
| NotFound | NotFound |
| Conflict | AlreadyExists |
| AuthenticationRequired | Unauthenticated |
| AccessDenied | PermissionDenied |
| other | FailedPrecondition |
Detail = `Error.Message` (fallback text if blank). Mapping a success throws `InvalidOperationException`.
It does **not** attach `ValidationResult.Errors` as trailers — if clients need field errors from pipeline validation,
provide a custom `IGrpcResultStatusMapper` (register before `AddPivotGrpc`, TryAdd keeps yours) that checks `result is IValidationResult v` and uses `IGrpcValidationStatusMapper.CreateTrailers(v.Errors)`.

`DefaultGrpcExceptionStatusMapper` (unhandled exceptions):
| Exception | StatusCode | Trailers |
|---|---|---|
| `Application.Exceptions.ValidationException` | InvalidArgument | `validation-errors` |
| `BadRequestException` | InvalidArgument | `validation-errors` |
| `NotFoundException` | NotFound | – |
| `RpcException` | its own status/detail/trailers (pass-through) | original |
| other | Internal — detail "An unexpected error occurred." in Production, `ex.ToString()` elsewhere | – |

`DefaultGrpcValidationStatusMapper` writes trailer `validation-errors` = JSON array of `{ "Code": "...", "Message": "..." }`
(PascalCase, System.Text.Json). Clients read it from `RpcException.Trailers`.

All three mappers are interfaces (`Abstractions/`) returning `GrpcStatusMapping(StatusCode, Detail, Metadata? Trailers)` — replace any of them via DI.

## Transactions (`GrpcTransactionInterceptor<TContext>`)

`GrpcTransactionInterceptorOptions` defaults: `InterceptUnaryCalls = true`; client/server/duplex streaming `false`
(long-lived streams would hold DB transactions); `ShouldCommitStatusCode = OK or InvalidArgument`.
Flow: `BeginTransactionAsync` → call → on success `Commit`; on exception compute the status (RpcException's own, else via
exception mapper) → commit if `ShouldCommitStatusCode(status)` else rollback → rethrow.
So a failure surfaced via `GetValueOrThrow` as `RpcException(NotFound)` rolls back, while `InvalidArgument` commits
(mirrors HTTP's "commit on 422" rule). Override with:
```csharp
services.AddPivotGrpcTransactions<AppDbContext>(o =>
{
	o.InterceptServerStreamingCalls = true;
	o.ShouldCommitStatusCode = code => code == StatusCode.OK;
});
```
Queries also run inside a transaction when unary interception is on (there's no GET/command distinction in gRPC) — cheap, but be aware for read replicas.

## Proto conventions (buildTransitive)

Consumers of the package automatically get `Protobuf` item defaults: `ProtoRoot="Protos"`, `GrpcServices="Server"`,
`Access="Public"`; the targets file sets `PivotFrameworkGrpcConventionsImported=true`. So in a service `.csproj`:
```xml
<ItemGroup>
  <Protobuf Include="Protos\orders.proto" />                           <!-- server stubs -->
  <Protobuf Include="Protos\billing.proto" GrpcServices="Client" />    <!-- override per item -->
</ItemGroup>
```
Keep `.proto` files under `Protos/` so import paths resolve against `ProtoRoot`.

## Changing this package
- Keep HTTP and gRPC mappings consistent (see `pivot-containers-api`). A new `ResultExceptionType` needs a branch in both.
- Tests: `Tests/Pivot.Framework.Containers.Grpc.Tests` (`Extensions`, `Interceptors`, `StatusMapping`, `TestDoubles`: `TestServerCallContext`, `TestHostEnvironment`, `TestDbContext`, `TestValidationResult`). Interceptor tests use EF InMemory contexts implementing `IPersistenceContext`.
- If you change the `buildTransitive` files, remember they are packed via `<None Include=… Pack="true" PackagePath="buildTransitive\…">` in the csproj.
