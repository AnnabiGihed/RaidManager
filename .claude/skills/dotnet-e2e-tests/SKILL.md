---
name: dotnet-e2e-tests
description: 'Write .NET end-to-end and integration tests against real dependencies: C# (not shell) automation, real services
  via containers, fixtures held to production code quality, and responsibilities split across dedicated support classes. Use
  for full-stack verification, not isolated unit tests.'
---

Use this skill for tests that exercise a service against real dependencies — database, message broker, identity provider — verifying observable end-to-end behavior. For isolated, fast tests of a single unit, use `dotnet-unit-tests`. The defining trait here is real infrastructure and real outcomes, not mocks.


## Tooling (organization standard)

**Selenium** is the organization's tool for automated end-to-end UI tests — use it for browser-driven journeys, and not for technical layers, which belong in xUnit tests. API-level integration tests use xUnit against real dependencies in containers; in-memory database providers are forbidden.

## Real dependencies of a Pivot.Framework service

| Dependency | Container (Testcontainers) | What to configure |
|---|---|---|
| Database | `MsSqlBuilder` (SQL Server, default) or `PostgreSqlBuilder` (when using `Pivot.Framework.Infrastructure.Persistence.PostgreSQL`) | connection string → `ConnectionStrings:Default`; apply the service's EF migrations in the fixture |
| Broker | `RabbitMqBuilder` | `RabbitMQ:*` section incl. a 32-char test `EncryptionKey`, `Queue`, `ClientProvidedName`; declare topology via `AddRabbitMQTopology` or in the fixture |
| Identity provider | Keycloak container (`quay.io/keycloak/keycloak`, e.g. `Testcontainers.Keycloak`) with a realm import | `Keycloak:BaseUrl`, `Realm`, `ClientId`, **`Audience`** (add an audience mapper or every call is 401), `RequireHttpsMetadata: false` |
| Cache (if used) | `RedisBuilder` | `ConnectionStrings:Redis` (needed by `WithRedisTokenCaching` and Blazor sessions) |

Pivot-specific assertions and settings:
- **Outbox → broker is asynchronous.** With `BackgroundPolling` the message appears after up to `PollingInterval`: set `o.PollingInterval = TimeSpan.FromMilliseconds(200)` in the test host and **poll with a timeout** in the broker assertion — never `Task.Delay` a fixed time. With `ImmediateAfterRequest` it is published right after a 2xx response.
- **Broker payloads are compressed then encrypted** (GZip → AES-256, IV prefixed) with the `EncryptionKey`. The broker-assertion support class must decode them with the same key — reuse `AesMessageEncryptor` + `GZipMessageCompressor` from `Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore` rather than re-implementing — and can read `BasicProperties.Type` (the event's assembly-qualified name) and the `CorrelationId`/`EventId` headers.
- **Persistence assertions** can also check `OutboxMessages` (`Processed`, `RetryCount`, `LastError`) and, with the event store, `EventHistory` rows for the aggregate.
- **Idempotency**: assert against `OutboxMessageConsumers` (inbox) that a redelivered event was processed once — and remember the event-id round-trip defect (`pivot-domain` §4) when such a test fails.
- **Auth**: mint tokens through the Keycloak container (password or client-credentials grant against `TokenUrl`), never through a test-only token endpoint in the application.

## Rules

- **Prefer C# E2E tests over shell-based automation** for long-term verification. Shell scripts drift, resist review, and aren't held to code standards; a C# test suite is maintained like the product.
- **Fixtures and support code are production code.** They follow the same standards as the app: named constants for repeated values, XML summaries, structured organization, no large unstructured helper blobs. A fixture is not a dumping ground.
- **Split responsibilities across dedicated support classes**, one concern each — environment discovery, container/infrastructure orchestration, identity-provider bootstrap, database assertions, message-broker assertions, process execution. Don't fuse them into one god-fixture.
- **Use real dependencies via containers** (Testcontainers) rather than mocking the boundary you're trying to verify. The whole point of an E2E test is that the database, broker, and auth actually work — a mocked dependency tests nothing real.
- **Separate tests by feature/use-case area**, not one monolithic scenario. Each area is independently runnable and readable.
- **Assert observable outcomes** — HTTP status and body, persisted database state, messages actually published to the broker — never internal application state.
- Tests own their lifecycle: spin up and **tear down** infrastructure deterministically (`IAsyncLifetime`), and isolate test data so runs don't contaminate each other.
- Distinguish levels and pick the lightest that proves the behavior: in-process integration (a host factory wiring the real app over a containerized DB) for most service behavior; full out-of-process E2E (containers + real network) when the deployment topology itself is under test.

## Stack

xUnit with `IAsyncLifetime` fixtures; Testcontainers for real database / broker / identity-provider instances; an in-process host factory for API integration tests where full container networking isn't needed; one assertion library matching the repo. Confirm existing choices before adding dependencies.

## Support-class responsibilities

Keep each of these in its own class with a single responsibility:

- **Environment discovery** — locate config, ports, connection strings for the spun-up infrastructure.
- **Infrastructure orchestration** — start/stop the containers (database, broker, identity provider) and wait for readiness.
- **Identity bootstrap** — provision realms/clients/users and mint tokens for authenticated calls.
- **Database assertions** — query the real database to assert persisted state.
- **Broker assertions** — assert a message landed on the expected queue/topic with the expected payload.
- **Process execution** — run the service or a CLI as a process when out-of-process E2E is required.

## What to verify end to end

- A request produces the right HTTP response **and** the right persisted state **and** the right published message — the full effect, not just the response.
- **Idempotency** holds across the wire: replaying a request with the same key yields a single effect and the cached/conflict response, asserted against real persistence.
- **Outbox/delivery** works: a state change results in the integration event actually reaching the broker (assert on the broker, not on an in-memory double).
- **Auth** is enforced: protected endpoints reject missing/invalid tokens and accept valid ones minted by the bootstrap.

## Workflow

1. **Pick the level** — in-process integration vs full out-of-process E2E — and the feature area under test (its own file).
2. **Stand up real dependencies** via the orchestration support class; wait for readiness.
3. **Bootstrap state** — auth tokens, seed data — through the dedicated support classes.
4. **Exercise the flow** through the real entry point (HTTP).
5. **Assert the full outcome** — response + database + broker — via the assertion support classes.
6. **Tear down** deterministically. **Deliver** per the output format.

## Output format

1. **Level & area** — in-process or full E2E, and the feature under test.
2. **Fixture/support** — the `IAsyncLifetime` fixture and which support classes it composes (each named, single-responsibility).
3. **Tests** — the scenarios, asserting full observable outcomes.
4. **Notes** — what real infrastructure runs, and teardown/isolation approach.

## Example

> **Level & area:** in-process integration over a containerized database; payment registration.
> ```csharp
> public sealed class PaymentApiTests : IClassFixture<ServiceFixture>
> {
>     private readonly ServiceFixture _fx;
>     public PaymentApiTests(ServiceFixture fx) => _fx = fx;
>
>     /// <summary>Verifies that registering a payment persists it and publishes the integration event.</summary>
>     [Fact]
>     public async Task RegisterPayment_ValidRequest_PersistsAndPublishes()
>     {
>         var token = await _fx.Identity.GetTokenAsync("clerk", _fx.Cancellation);
>         _fx.Client.DefaultRequestHeaders.Authorization = new("Bearer", token);
>
>         var response = await _fx.Client.PostAsJsonAsync($"/api/v1/dossiers/{KnownDossier}/payments",
>             new { amount = 100m, currency = "EUR" }, _fx.Cancellation);
>
>         response.StatusCode.ShouldBe(HttpStatusCode.OK);
>         (await _fx.Db.PaymentExistsAsync(KnownDossier, 100m, _fx.Cancellation)).ShouldBeTrue();
>         var published = await _fx.Broker.WaitForEventAsync<PaymentRegisteredIntegrationEvent>(
>             e => e.DossierId == KnownDossier, timeout: TimeSpan.FromSeconds(10), _fx.Cancellation);
>         published.Amount.ShouldBe(100m);
>     }
> }
> ```
> **Notes:** SQL Server, RabbitMQ and Keycloak run in containers via the fixture; `Identity` (Keycloak token), `Db` (SQL assertions), and `Broker` (decrypt + decompress + deserialize with Pivot's encryptor/compressor, polling with a timeout) are separate single-responsibility support classes; the test asserts response + persistence + published message. `ApiController.HandleResult` returns 200 with the value (not 201). Containers are disposed in the fixture's `DisposeAsync`.

## Completion criteria

- Verification is C#, not shell-based.
- Real dependencies run in containers; the boundary under test is not mocked away.
- Support code is split by responsibility and held to production code quality.
- Tests are separated by feature area and assert full observable outcomes (response + state + messages).
- Infrastructure is torn down deterministically and test data is isolated.
- Idempotency, delivery, and auth enforcement are verified end to end where relevant.

## Example prompts

- "Write an integration test that registers a payment and asserts it persisted and published an event."
- "Set up a Testcontainers fixture with the database, broker, and identity provider as separate support classes."
- "Replace this shell-based E2E script with a structured C# test suite."
