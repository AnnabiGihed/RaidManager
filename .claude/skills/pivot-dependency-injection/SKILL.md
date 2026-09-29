---
name: pivot-dependency-injection
description: Pivot.Framework.Tools.DependencyInjection — the IServiceInstaller convention and BaseServiceInstaller Scrutor scanning. Use when organising DI registration into per-layer installer classes, writing a loop that runs installers, adding convention-based (AsMatchingInterface) registrations, or changing Src/Tools/Pivot.Framework.Tools.DependencyInjection.
---

# Pivot.Framework.Tools.DependencyInjection

Location: `Src/Tools/Pivot.Framework.Tools.DependencyInjection`. Dependencies: `Microsoft.Extensions.Configuration.Abstractions`,
`Microsoft.Extensions.DependencyInjection.Abstractions`, `Scrutor 7`. No other Pivot dependency.

## What exists (exactly two types)

```csharp
namespace Pivot.Framework.Tools.DependencyInjection.Abstractions;
public interface IServiceInstaller
{
	void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true);
}

namespace Pivot.Framework.Tools.DependencyInjection;
public abstract class BaseServiceInstaller
{
	protected void IncludeConventionBasedRegistrations(
		IServiceCollection services, IConfiguration configuration, Assembly[] assemblies,
		ServiceLifetime lifetime = ServiceLifetime.Scoped, RegistrationStrategy? strategy = null);
}
```

`IncludeConventionBasedRegistrations` scans the assemblies with Scrutor for **non-abstract, non-open-generic
classes** and registers each `AsMatchingInterface()` (class `OrderRepository` ↔ interface `IOrderRepository`),
with the given lifetime (Scoped default) and `RegistrationStrategy.Skip` by default (existing registrations win).
It throws `ArgumentException` when `assemblies` is empty. `configuration` is accepted but unused.

## There is no `InstallServices` extension

The README shows `services.InstallServices(configuration, true, assemblies…)` — **it does not exist in the source.**
Either write the loop in the host, or add the extension to this package (see "Extending" below).

Host-side loop:

```csharp
var installerAssemblies = new[]
{
	MyService.Application.AssemblyReference.Assembly,
	MyService.Infrastructure.AssemblyReference.Assembly,
	MyService.Presentation.AssemblyReference.Assembly
};

foreach (var installer in installerAssemblies
	.SelectMany(a => a.DefinedTypes)
	.Where(t => typeof(IServiceInstaller).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
	.Select(Activator.CreateInstance)
	.Cast<IServiceInstaller>())
{
	installer.Install(builder.Services, builder.Configuration, includeConventionBasedRegistration: true);
}
```

## Writing an installer

```csharp
public sealed class InfrastructureServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		services.AddPostgreSqlContext<AppDbContext>(configuration.GetConnectionString("Default")!);
		services.AddEfCoreWritePersistence<AppDbContext, AppUnitOfWork>();

		if (includeConventionBasedRegistration)
			IncludeConventionBasedRegistrations(services, configuration, [AssemblyReference.Assembly]);
	}
}
```

Guidelines:
- One installer per concern/layer; installers must be parameterless-constructible (they are created by reflection).
- Explicit framework registrations (`AddEfCoreWritePersistence`, `AddOutboxDraining`, …) belong in installers; convention scanning is for your own `Foo`/`IFoo` pairs.
- Because the default strategy is `Skip`, register explicit overrides **before** calling the scan.
- Beware: `AsMatchingInterface` on an assembly also picks up classes like `AppUnitOfWork` only if an `IAppUnitOfWork` exists — framework generic interfaces (`IUnitOfWork<TContext>`) are not matched, so keep registering those explicitly.
- `AddOutboxDraining` throws if called twice — make sure only one installer calls it.

## Extending this package

If you add an `InstallServices` extension, follow repo conventions: `public static class ServiceInstallerExtensions`
in namespace `Pivot.Framework.Tools.DependencyInjection`, guard clauses, header doc comment, tabs, return
`IServiceCollection`, and keep dependencies to the three packages above. There is no test project for this
package; add one (`Tests/Pivot.Framework.Tools.DependencyInjection.Tests`, xUnit + FluentAssertions) and add it
to `Pivot.Framework.sln` if you introduce logic. Update README section "Tools — DependencyInjection" accordingly.
