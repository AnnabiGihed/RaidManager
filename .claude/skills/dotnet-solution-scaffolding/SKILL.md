---
name: dotnet-solution-scaffolding
description: 'Lay out a NEW .NET solution in the mandatory house Clean-Architecture structure on Pivot.Framework: src/Containers (API, Aspire, Jobs, UI), src/Core (Application, Domain, IntegrationEvents), src/Infrastructure (Infrastructure, Persistence.EntityFrameworkCore), a mirrored test tree, the Pivot.Framework package references per layer, the GitHub Packages feed, build props and the composition root. Use whenever creating a new service, solution, API or project skeleton — not when adding a feature to an existing solution.'
---

Use this skill to scaffold a brand-new solution in the canonical structure so it matches every other service from day
one. Mandatory regardless of size — a small API gets the same layout with fewer containers. Features are then added
with `dotnet-ddd-cqrs-conventions`. If a solution already exists, mirror it instead.

## Canonical structure

```text
{Solution}.sln (or .slnx — see dotnet-10-and-csharp-14)
src/
├── Containers/
│   ├── API/
│   │   ├── {Solution}.ApiService                 ← backend API host (ASP.NET Core, Pivot.Framework.Containers.API)
│   │   ├── {Solution}.GrpcService                ← gRPC host, when needed (Pivot.Framework.Containers.Grpc)
│   │   └── {Solution}.PublicApi                  ← public-facing API host, when required
│   ├── Aspire/
│   │   ├── Defaults/{Solution}.ServiceDefaults   ← telemetry, health, resilience defaults
│   │   └── Hosting/{Solution}.AppHost            ← .NET Aspire orchestration (startup project; SQL Server/PostgreSQL, RabbitMQ, Redis, Keycloak containers for dev)
│   ├── Jobs/{Solution}.{JobName}                 ← background/batch workers (Hangfire host or worker), when needed
│   └── UI/
│       ├── Core/{Solution}.Shared, {Solution}.ViewModels
│       └── Hosting/{Solution}.Web                ← Blazor Server host, when there is a UI
├── Core/
│   ├── {Solution}.Application                    ← commands, queries, handlers, validators, projections (CQRS via MediatR)
│   ├── {Solution}.Domain                         ← aggregates, ids, value objects, domain events, errors, repository interfaces
│   └── {Solution}.IntegrationEvents              ← integration event contracts shared across services
└── Infrastructure/
    ├── {Solution}.Infrastructure                 ← messaging config (routing resolvers, topology), external clients, auth glue
    └── {Solution}.Persistence.EntityFrameworkCore ← DbContext (PivotDbContextBase), UnitOfWork, repositories, configurations, migrations
test/
└── (mirrors src/ exactly: one {Project}.Tests per source project, plus {Solution}.E2E.Tests)
Directory.Build.props  Directory.Packages.props  .editorconfig  stylecop.json
README.md  CHANGELOG.md  CONTRIBUTING.md  LICENSE  nuget.config
docs/  (see docs-as-code)
```
Folder names exact: `src/`, `test/` (singular); group folders PascalCase.

## Pivot.Framework package per project

All `Pivot.Framework.*` packages share one version — pin them together in `Directory.Packages.props`.

| Project | References |
|---|---|
| `{Solution}.Domain` | `Pivot.Framework.Domain` |
| `{Solution}.IntegrationEvents` | `Pivot.Framework.Domain` (for `IntegrationEvent`) — nothing else |
| `{Solution}.Application` | `Pivot.Framework.Application`, `Pivot.Framework.Infrastructure.Abstraction` (for `IUnitOfWork<T>`, publishers), Domain, IntegrationEvents |
| `{Solution}.Persistence.EntityFrameworkCore` | `Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` (+ `.Persistence.PostgreSQL` if PostgreSQL), Application, Domain |
| `{Solution}.Infrastructure` | `Pivot.Framework.Infrastructure.Messaging.EntityFrameworkCore`; optionally `.Infrastructure.Caching`, `.Infrastructure.ReadStore.MongoDB`, `.Infrastructure.Scheduling` |
| `{Solution}.ApiService` | `Pivot.Framework.Containers.API` (brings `Authentication.AspNetCore` + `.Caching`), `Pivot.Framework.Tools.DependencyInjection`, Infrastructure, Persistence, ServiceDefaults |
| `{Solution}.GrpcService` | `Pivot.Framework.Containers.Grpc`, `Pivot.Framework.Authentication.AspNetCore` |
| `{Solution}.Web` | `Pivot.Framework.Authentication.Blazor`, `Radzen.Blazor` (the only UI component library — no Bootstrap, no DIFA; see `blazor-components`) |
| Jobs host | `Pivot.Framework.Infrastructure.Scheduling` (+ `Authentication.Hangfire` for the dashboard) |

Never scaffold hand-rolled Result/command/aggregate/repository/controller bases — they exist in Pivot (see `pivot-framework`).

## Rules

- **Dependency direction inward only.** Domain → nothing but `Pivot.Framework.Domain`. Application → Domain. IntegrationEvents → contracts only. Infrastructure and Persistence → Application/Domain, never a container. Containers compose everything. Domain referencing EF Core or ASP.NET is a structural error.
- **Aspire hosts the development composition.** Install the workload (`dotnet workload install aspire`) rather than excluding the AppHost from the build. Every runnable host references ServiceDefaults.
- **The AppHost must be runnable with F5 on day one.** A missing launch profile leaves developers with a silent console window and a dashboard address buried in the logs. Every AppHost ships:
  - `Properties/launchSettings.json` with an `https` and an `http` profile: `"commandName": "Project"`, `"launchBrowser": true`, a **fixed** `applicationUrl` for the dashboard, `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` = `Development`, and fixed `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` and `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` (the `http` profile also sets `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`). Pick ports that don't collide with the hosts' own `launchSettings.json`.
  - a `<UserSecretsId>` in the `.csproj`, so `dotnet user-secrets` and persisted generated parameters (`AddParameter(name, new GenerateParameterDefault { ... }, secret: true, persist: true)`) have somewhere to live.
  - every secret declared as `builder.AddParameter("name", secret: true)` and passed to hosts with `WithEnvironment(...)`/`WithReference(...)`. Values live in the AppHost's user secrets (`dotnet user-secrets set "Parameters:name" "<value>" --project <AppHost>`), never in `appsettings*.json` — not even as empty keys or placeholder passwords.
  - README instructions: set the AppHost as the Visual Studio startup project with the `https` profile, the dashboard address, what the first run does (image downloads, migrations), and the exact `user-secrets` commands.
- **Persistence is its own project**, SQL Server via EF Core by default (PostgreSQL via `Pivot.Framework.Infrastructure.Persistence.PostgreSQL` when the work item says so); migrations in that project; no in-memory providers.
- **Feature-first folders** in every project (`Features/<Feature>/…`, `Features/Shared/`) — see `dotnet-ddd-cqrs-conventions`.
- **Test tree mirrors src/** — one test project per source project plus one E2E project.
- **Thin composition root** in each container: host composition + extension calls, typically via `IServiceInstaller` classes per layer (`pivot-dependency-injection` — there is no `InstallServices` helper; loop over installers yourself). It registers MediatR + `ValidationPipelineBehavior` once, the persistence bundle, one transport, one drain mode, and auth.
- **Framework:** `net10.0` (LTS), set once in `Directory.Build.props`. Pivot.Framework itself targets `net10.0`.
- **Containerisation mandatory:** every deployable host ships a `Dockerfile`.
- **Package sources:** commit a credential-free `nuget.config` with nuget.org plus the Pivot.Framework GitHub Packages feed, package source mapping so `Pivot.Framework.*` resolves only from it, and credentials read from the environment:
  ```xml
  <configuration>
    <packageSources>
      <clear />
      <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
      <add key="pivot" value="https://nuget.pkg.github.com/AnnabiGihed/index.json" />
    </packageSources>
    <packageSourceMapping>
      <packageSource key="pivot"><package pattern="Pivot.Framework.*" /></packageSource>
      <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    </packageSourceMapping>
    <packageSourceCredentials>
      <pivot>
        <add key="Username" value="%PIVOT_PACKAGES_USER%" />
        <add key="ClearTextPassword" value="%PIVOT_PACKAGES_TOKEN%" />
      </pivot>
    </packageSourceCredentials>
  </configuration>
  ```
  The token is a GitHub PAT with `read:packages`; never commit it.
- **Bootstrap a conforming repository**, not just projects: `Directory.Build.props` + `Directory.Packages.props` (TFM, nullable, analyzers, doc generation, CPM — see `clean-code-static-analysis`), `.editorconfig` (tabs, SA1124 off), root files (`docs-root-files`), `docs/` (`docs-as-code`), and `docs/adr/0001-…` recording the stack (`docs-adr`).
- **Every project resolves an effective TFM** from `Directory.Build.props`; `.csproj` files stay minimal with no package versions.

## Minimal ApiService composition root

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var services = builder.Services;
services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);
	cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
});
services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);

services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>();
services.AddDomainEventDispatcher();
services.AddRabbitMQPublisher(builder.Configuration);
services.AddOutboxDraining<AppDbContext>(o => o.Mode = OutboxDrainMode.BackgroundPolling);
services.AddKeycloakAuthentication(builder.Configuration, o => o.WithCurrentUser().WithSwagger("{Solution} API"));
services.AddPivotApiVersioning();
services.AddControllers();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TransactionMiddleware<AppDbContext>>();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
```
Details for each call: `pivot-persistence-efcore`, `pivot-messaging-outbox`, `pivot-auth-aspnetcore`, `pivot-containers-api`.

## Workflow
1. Determine containers — API (REST and/or gRPC), Jobs, UI. Aspire always.
2. Create src/ layers with the Pivot package references above.
3. Wire references inward only; hosts → ServiceDefaults; register hosts and dev resources (DB, RabbitMQ, Redis, Keycloak) in AppHost; add the AppHost's `launchSettings.json`, `UserSecretsId` and secret parameters, then **run the AppHost** and confirm the dashboard opens and every resource reaches Running.
4. Mirror the test/ tree + E2E.
5. Bootstrap build props, analyzers, `nuget.config`, root files, `docs/`, first ADR.
6. Hand off features to `dotnet-ddd-cqrs-conventions`, tests to `dotnet-unit-tests` / `dotnet-e2e-tests`.

## Output format
1. **Containers chosen** and why. 2. **Project tree** with roles. 3. **References** (inward graph, Pivot packages, AppHost wiring). 4. **Bootstrap files.** 5. **Next steps.**

## Completion criteria
- Tree matches the canonical structure and casing; Aspire AppHost + ServiceDefaults wired to every runnable host.
- The AppHost has `Properties/launchSettings.json` (`https`/`http`, `launchBrowser`, fixed dashboard/OTLP/resource-service ports), a `UserSecretsId`, secrets as `AddParameter(..., secret: true)` with no values in any `appsettings*.json`, README run instructions, and it was actually started once with every resource Running.
- Core has Application, Domain, IntegrationEvents; Persistence separate from Infrastructure; references inward only.
- Each layer references its Pivot.Framework package; versions pinned together centrally; `nuget.config` maps `Pivot.Framework.*` to GitHub Packages with env-var credentials.
- `test/` mirrors `src/` plus E2E; every project resolves `net10.0`; no package versions in `.csproj`.
- Repo bootstrapped (props, analyzers, root files, docs, ADR-0001); feature-first folders; SQL Server/PostgreSQL via EF Core, no in-memory provider.

## Example prompts
- "Scaffold a new solution called InventoryRegistry on Pivot.Framework in our standard structure."
- "Create a REST API skeleton with the Containers/Core/Infrastructure layout and mirrored tests."
- "Set up a service with an API, an import job and a Blazor UI, following our project structure."
