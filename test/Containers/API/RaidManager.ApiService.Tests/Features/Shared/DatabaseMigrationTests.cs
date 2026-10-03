using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Shared.Hosting;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Persistence.EntityFrameworkCore;

namespace RaidManager.ApiService.Tests.Features.Shared;

/// <summary>Verifies the migration step a deployment runs before it starts a new API version.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: A deployed API doesn't migrate at startup; the deploy workflow runs <see cref="DatabaseMigration"/> as a step
/// of its own (#389). The API here runs as Production, so its fresh database starts with every migration pending.
/// </remarks>
public sealed class DatabaseMigrationTests : IClassFixture<ProductionApiFactory>
{
    #region Fields
    /// <summary>Stores the API factory running as Production.</summary>
    private readonly ProductionApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DatabaseMigrationTests"/> class.</summary>
    /// <param name="api">The API factory running as Production.</param>
    public DatabaseMigrationTests(ProductionApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Migrates a database that a deployed API left untouched at startup.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task TheMigrationStepBringsAFreshDatabaseUpToDate()
    {
        (await PendingMigrationsAsync()).ShouldNotBeEmpty();

        await DatabaseMigration.MigrateAsync(_api.Services);

        (await PendingMigrationsAsync()).ShouldBeEmpty();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Lists the migrations the database hasn't applied yet.</summary>
    /// <returns>The pending migrations.</returns>
    private async Task<List<string>> PendingMigrationsAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return [.. await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database.GetPendingMigrationsAsync()];
    }
    #endregion Private Helpers
}
