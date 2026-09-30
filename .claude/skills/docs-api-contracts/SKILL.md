---
name: docs-api-contracts
description: FAVV-AFSCA rules for API documentation. Use whenever you add, change or review a REST endpoint, controller, minimal API, gRPC service, GraphQL schema, message/event contract, public library surface or configuration options class. Covers OpenAPI 3.x as the single source of truth, contract-first vs code-first, required operation and schema content, versioning, publication, AsyncAPI, proto/SDL files, generated library references and configuration schemas.
---

# API Documentation and OpenAPI

Source: Development Standards Wiki → *API Documentation & OpenAPI*. Cite rules as `API Documentation & OpenAPI / R<n>`. Severity handling follows `docs-standards-governance`.

The contract file is the documentation. You never write an API reference by hand.

## 1. REST APIs

- 🔴 **R1.** Every REST API (public, partner or internal) exposes an **OpenAPI 3.x** specification.
- 🔴 **R2.** The specification is the single source of truth. Hand-written Markdown API references that duplicate it are FORBIDDEN; if you find one, replace it with a generated page and delete it.
- 🔴 **R3.** Commit the specification at `openapi.yaml` in the repository root or at `docs/api/openapi.yaml`. If the generator can only emit JSON, commit `openapi.json` at the same location.
- 🔴 **R4.** CI validates the specification with a schema validator (`redocly lint`, `swagger-cli validate`); failure breaks the build.
- 🟡 **R5.** CI also runs Spectral with the organizational ruleset from `favv-afsca/engineering-configs`.

## 2. Contract-first or code-first

- 🟡 **R6.** Each project uses one approach, recorded in an ADR:
  - **Contract-first**: `openapi.yaml` is written first; server stubs and clients are generated from it. For APIs with several consumers or strict contracts.
  - **Code-first**: endpoints are annotated and the specification is generated at build time. For fast-moving internal APIs.
  Check `docs/adr/` for this decision before touching an API. If there is none, ask the user which approach applies and propose the ADR.
- 🔴 **R7.** In code-first mode the generated specification is committed and regenerated in every PR that touches the API surface. CI regenerates it and fails when it differs from the committed file.

### Code-first in ASP.NET Core

Minimal APIs: every endpoint declares its operationId, summary, tags and every response:

```csharp
app.MapPost("/v1/import-jobs", StartImportJob)
	.WithName("startImportJob")
	.WithSummary("Start an attribute import job")
	.WithDescription("Uploads a CSV file and starts importing it into the given attribute template.")
	.WithTags("Import jobs")
	.Produces<ImportJobResponse>(StatusCodes.Status201Created)
	.ProducesValidationProblem(StatusCodes.Status400BadRequest)
	.ProducesProblem(StatusCodes.Status404NotFound)
	.ProducesProblem(StatusCodes.Status409Conflict);
```

Controllers: `[EndpointName]`, `[EndpointSummary]`, `[Tags]` and one `[ProducesResponseType]` per status code, plus XML doc comments on the action and its parameters.

Build-time generation (.NET 9+), so CI can diff the result:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.ApiDescription.Server" Version="<current>" PrivateAssets="all" />
</ItemGroup>
<PropertyGroup>
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../../docs/api</OpenApiDocumentsDirectory>
  <OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>
</PropertyGroup>
```

## 3. Required content

- 🔴 **R8.** Every operation declares: `summary`, `operationId` (camelCase verb + noun, e.g. `startImportJob`), `tags`, typed parameters, the request body schema when there is one, and a response schema for **every** status code it returns, including errors.
- 🔴 **R9.** Every schema declares types, `required` fields, and `format` wherever one applies (`uuid`, `date-time`, `date`, `email`, `uri`, `int64`).
- 🟡 **R10.** At least one request example and one response example per status code.
- 🟡 **R11.** Errors use RFC 9457 Problem Details (`application/problem+json`, fields `type`, `title`, `status`, `detail`, `instance`) unless a legacy contract dictates otherwise. In ASP.NET Core, register `AddProblemDetails()` and return `TypedResults.Problem(...)` / `ValidationProblem(...)`.
- 🟡 **R12.** Security schemes (OAuth2, Bearer, API key) are declared in `components.securitySchemes` and referenced by every operation that needs them.

## 4. Versioning

- 🔴 **R13.** A breaking change bumps the contract's major version. Whether the version is in the path (`/v1/`) or the `Accept` header is decided per project in an ADR; follow that ADR.
- 🔴 **R14.** No undocumented endpoints. Every endpoint served in production appears in the specification. Never use `.ExcludeFromDescription()`, `[ApiExplorerSettings(IgnoreApi = true)]` or equivalent on a production endpoint. Internal endpoints are documented with a security scheme and `x-internal: true`, or removed.
- 🟡 **R15.** Deprecated operations set `deprecated: true` and return `Deprecation` and `Sunset` headers at runtime.

A change is **breaking** when it removes or renames an endpoint, field or enum value; adds a required parameter or field to a request; changes a type or format; changes a status code for an existing outcome; or tightens validation. Breaking changes also need a `### Breaking changes` CHANGELOG entry (`docs-root-files`).

## 5. Rendering and publication

- 🔴 **R16.** Every API host serves an interactive reference page on its own OpenAPI document: **Scalar** by default or **Swagger UI**, at least in Development. It is mandatory, not an option; an API without it is incomplete (`dotnet-solution-scaffolding`). In addition, a human-readable rendering (Redoc, Swagger UI or Scalar) is published for every released version.
- 🟡 **R17.** Embed it in the project's documentation site, not on an unrelated domain.
- 🟡 **R18.** "Try it out" targets a sandbox, never production.
- 🟡 **R19.** Publish the latest specification file at a stable URL so partners can generate clients.

## 6. Event-driven APIs

- 🟡 **R20.** Message and event contracts (Service Bus, Event Grid, Kafka, SignalR, webhooks) are documented in `asyncapi.yaml` using AsyncAPI 3.x.
- 🟡 **R21.** The AsyncAPI document describes channels, messages, payload schemas (JSON Schema or Avro) and security.

```yaml
asyncapi: 3.0.0
info:
  title: SAM import events
  version: 1.0.0
channels:
  importJobCompleted:
    address: sam.import-jobs.completed
    messages:
      ImportJobCompleted:
        $ref: '#/components/messages/ImportJobCompleted'
components:
  messages:
    ImportJobCompleted:
      contentType: application/json
      payload:
        type: object
        required: [importJobId, processedBytes, occurredAt]
        properties:
          importJobId: { type: string, format: uuid }
          processedBytes: { type: integer, format: int64 }
          occurredAt: { type: string, format: date-time }
```

## 7. gRPC and GraphQL

- 🔴 **R22.** gRPC services commit their `.proto` files; they are the contract. Docs may be generated with `protoc-gen-doc`.
- 🔴 **R23.** GraphQL services commit their SDL; docs may be generated with SpectaQL or GraphDoc.

## 8. Libraries, SDKs and internal packages

- 🔴 **R24.** The reference is generated from doc comments. For .NET: XML doc comments → DocFX (or Mintlify). Follow the `csharp-xml-documentation` skill for the comments themselves.
- 🔴 **R25.** Hand-written reference pages for a library's public surface are FORBIDDEN.
- 🔴 **R26.** The build fails when a public type or member has no doc comment: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `dotnet_diagnostic.CS1591.severity = error`.
- 🟡 **R27.** Publish the generated reference with every release, versioned with the package.

## 9. Configuration

- 🟡 **R28.** Configuration documentation comes from its schema: for .NET, the options classes with data annotations and XML doc comments; otherwise JSON Schema.
- 🟡 **R29.** Generate `docs/reference/configuration.md` from that schema at build time (e.g. json-schema-for-humans) instead of maintaining it by hand.

## 10. Review checklist

- [ ] Every new or changed endpoint is in the committed specification, with operationId, summary, tags, typed parameters and all responses.
- [ ] Error responses use Problem Details.
- [ ] The host serves its interactive reference page (Scalar or Swagger UI) in Development, and a test asserts it (R16).
- [ ] No endpoint is hidden from the specification.
- [ ] Breaking changes bump the major version and are in the CHANGELOG.
- [ ] Event contracts are in `asyncapi.yaml`; `.proto`/SDL files are committed.
- [ ] Public library members have XML doc comments; no hand-written reference was added.

## 11. Pivot.Framework specifics (contracts must describe what the framework actually emits)

### REST (Pivot.Framework.Containers.API)
- **Generator:** Pivot registers **Swashbuckle** (`AddKeycloakAuthentication(… o.WithSwagger(title, version))` → `AddSwaggerGen` + Keycloak OAuth2 security scheme `oauth2`). Generate the committed spec from Swashbuckle — build-time via `Microsoft.Extensions.ApiDescription.Server` (Swashbuckle provides the document provider) or `dotnet swagger tofile` (`Swashbuckle.AspNetCore.Cli`). Do **not** add `Microsoft.AspNetCore.OpenApi`'s `AddOpenApi()` next to it — one generator per service.
- **Success status:** `ApiController.HandleResult<T>` returns **200** with the unwrapped value. Document 200 (not 201) unless the action returns `CreatedAtAction` itself; non-generic `Result` actions typically return 204 via `NoContent()`.
- **Failure shape** — declare these schemas once in `components` and reference them:
  - `HandleFailure` → `application/problem+json` with `title` (`Validation Error` | `Bad Request` | `Not Found` | `Conflict` | `Unauthorized` | `Forbidden`), `status`, **`type` = the Pivot error code** (e.g. `Order.NotFound`, a code, not a URI), `detail` = error message, optional `validationErrors: [{ code, message }]` (code = property name for pipeline validation). Status per `ResultExceptionType`: 400 (ValidationError/other), 404, 409, 401, 403.
  - `ExceptionHandlerMiddleware` → same media type with `type` ∈ `ValidationException` | `BadRequestException` | `NotFoundException` | `InternalServerError`, `instance` = path, `extensions.traceId`, `detail` omitted in Production.
  Every operation lists the statuses it can actually produce: 400 always (pipeline validation), 401/403 when protected, 404/409 when the handler returns those types, 500.
- **Versioning:** `AddPivotApiVersioning()` accepts URL segment (`/api/v{version:apiVersion}/…`), header `X-Api-Version` and query `api-version`, reports versions, groups docs as `v1`, `v2`. Publish one document per version; the choice of the canonical reader is recorded in an ADR (R13).
- **Security:** reference the `oauth2` scheme (authorization code + PKCE against Keycloak) on protected operations; Swagger UI "Authorize" via `UseKeycloakOAuth`.
- **BFF endpoints** returning `BffResponse<T>` document `availability`, `degradedComponents`, and the **503 + `Retry-After`** response produced by `BffResponseFilter`.
- **Auth endpoints** from `MapAuthenticationApi` (`/auth/login`, `/callback`, `/refresh`, `/logout`, `/profile`, `/introspect`) are part of the contract (R14) — don't hide them.

### gRPC (Pivot.Framework.Containers.Grpc)
- `.proto` files live under `Protos/` (Pivot's build-transitive default `ProtoRoot`) and are the contract (R22).
- Document the error model: status codes from `DefaultGrpcResultStatusMapper` (InvalidArgument, NotFound, AlreadyExists, Unauthenticated, PermissionDenied, FailedPrecondition) and the `validation-errors` trailer (JSON array of `{ "Code", "Message" }`).

### Events over RabbitMQ (AsyncAPI, R20–R21)
- **Payload schema** = the event record serialized by **Newtonsoft.Json with default settings → PascalCase property names** (`Id`, `OccurredOnUtc`, `CorrelationId`, then the event's own properties). Don't document camelCase unless the service changes the serializer.
- **Transport encoding:** body = AES-256-CBC( GZip( UTF-8 JSON ) ) with the 16-byte IV prefixed; `contentType` on the wire is `application/octet-stream`. Describe the logical payload as `application/json` and document the encoding and the shared-key requirement in the message `description`/bindings — consumers outside .NET must implement it.
- **Headers / properties:** AMQP `type` = the event's assembly-qualified CLR type name (consumers resolve it with `Type.GetType`), headers `CorrelationId`, `EventId`, `Timestamp` (unix ms); optional `ProjectionVersion` during replays. Delivery is persistent, publish is `mandatory`.
- **Channels:** exchange + routing key from `IOutboxRoutingResolver` (or `RabbitMQ:Exchange`/`RoutingKey`), queues and DLQs from `AddRabbitMQTopology` (`{queue}.dlq`, `{exchange}.dlx`).
- Integration events are versioned contracts in `{Solution}.IntegrationEvents`; a renamed or moved CLR type is a **breaking change** (the `type` property changes) — bump the major version and keep the old type until consumers migrate.
