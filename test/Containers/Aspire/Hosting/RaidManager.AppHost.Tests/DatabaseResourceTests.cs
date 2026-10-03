using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Shouldly;
using Xunit;

namespace RaidManager.AppHost.Tests;

/// <summary>Verifies the database the AppHost declares for the local run.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: Builds the AppHost's application model without starting it, so no container runs, and checks the PostgreSQL
/// server, its data volume and the <c>Database</c> resource the API reads its connection string from (ADR-0029).
/// </remarks>
public sealed class DatabaseResourceTests
{
    #region Tests
    /// <summary>Finds one PostgreSQL server with a data volume and the RaidManager database on it.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AppHostDeclaresThePostgreSqlDatabaseWithAVolume()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RaidManager_AppHost>();

        var server = builder.Resources.OfType<PostgresServerResource>().ShouldHaveSingleItem();
        var database = builder.Resources.OfType<PostgresDatabaseResource>().ShouldHaveSingleItem();

        server.Name.ShouldBe("postgres");
        server.Annotations.OfType<ContainerMountAnnotation>().ShouldContain(mount => mount.Type == ContainerMountType.Volume);
        database.Name.ShouldBe("Database");
        database.DatabaseName.ShouldBe("raidmanager");
        database.Parent.ShouldBeSameAs(server);
    }
    #endregion Tests
}
