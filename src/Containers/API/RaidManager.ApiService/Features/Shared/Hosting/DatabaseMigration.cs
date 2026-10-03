using Microsoft.EntityFrameworkCore;
using RaidManager.Persistence.EntityFrameworkCore;

namespace RaidManager.ApiService.Features.Shared.Hosting;

/// <summary>Applies the database migrations, at startup locally and as a step of its own in a deployment.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: A deployed API never migrates at startup. The deploy workflow runs the new API image once with
/// <see cref="MigrateAndExitKey"/> set, before it replaces the running version, so a failed migration leaves the
/// previous version serving (ADR-0027, #389; #434 adds rollback).
/// </remarks>
public static class DatabaseMigration
{
    #region Constants
    /// <summary>Defines the setting that makes the API apply the migrations and exit, without serving requests.</summary>
    public const string MigrateAndExitKey = "Database:MigrateAndExit";
    #endregion Constants

    #region Public Methods
    /// <summary>Applies every pending migration to the RaidManager database.</summary>
    /// <param name="services">The application's services.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the database is up to date.</returns>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database.MigrateAsync(cancellationToken);
    }
    #endregion Public Methods
}
