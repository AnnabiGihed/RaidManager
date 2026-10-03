using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shouldly;
using Xunit;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests;

/// <summary>Verifies the context that the EF Core command-line tools create.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: <c>dotnet ef migrations add</c> builds its context through the design-time factory, so migrations are generated
/// for PostgreSQL only if that factory targets it (ADR-0029). No database is needed: the context isn't opened.
/// </remarks>
public sealed class RaidManagerDbContextFactoryTests
{
    #region Tests
    /// <summary>Creates the design-time context, as the tools do, and checks its provider.</summary>
    [Fact]
    public void DesignTimeContextTargetsPostgreSql()
    {
        var factoryType = typeof(RaidManagerDbContext).Assembly.GetTypes()
            .Single(type => typeof(IDesignTimeDbContextFactory<RaidManagerDbContext>).IsAssignableFrom(type));
        var factory = (IDesignTimeDbContextFactory<RaidManagerDbContext>)Activator.CreateInstance(factoryType)!;

        using var context = factory.CreateDbContext([]);

        context.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
    #endregion Tests
}
