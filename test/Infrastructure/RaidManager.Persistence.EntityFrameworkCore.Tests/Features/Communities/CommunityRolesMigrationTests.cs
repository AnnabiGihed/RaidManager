using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Communities;

/// <summary>Verifies that the migration giving communities their own roles keeps every existing mapping.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Runs the migration on a database holding mappings in the old shape (Officer and RaidLeader by name), on its
/// own database in the shared SQL Server container, and checks that members keep exactly what they had (#312).
/// </remarks>
[Collection(SqlServerTestGroup.Name)]
public sealed class CommunityRolesMigrationTests
{
    #region Constants
    /// <summary>Defines the last migration before communities had their own roles.</summary>
    private const string PreviousMigration = "20261001184437_AllowADiscordRoleToGiveSeveralRoles";
    #endregion Constants

    #region Fields
    /// <summary>Stores the SQL Server fixture.</summary>
    private readonly SqlServerFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRolesMigrationTests"/> class.</summary>
    /// <param name="database">The SQL Server fixture.</param>
    public CommunityRolesMigrationTests(SqlServerFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Migrates old mappings onto the Officer and Raid leader presets, which keep their permissions.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ExistingMappingsMoveToThePresets()
    {
        var connectionString = new SqlConnectionStringBuilder(_database.ConnectionString) { InitialCatalog = $"RolesMigration{Guid.NewGuid():N}" }.ConnectionString;
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddRaidManagerPersistence(connectionString)
            .BuildServiceProvider();
        var communityId = Guid.NewGuid();
        await using (var scope = services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Communities (Id, DiscordGuildId, Name, Realm, AdministratorId, IsDeleted, Version) VALUES ({communityId}, '123456789012345678', N'Citadel Vanguard', N'Icecrown', {Guid.NewGuid()}, 0, 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO CommunityRoleMappings (CommunityId, DiscordRoleId, Role) VALUES ({communityId}, '111', N'Officer'), ({communityId}, '111', N'RaidLeader'), ({communityId}, '222', N'RaidLeader')");

            await context.Database.MigrateAsync();
        }

        await using var readScope = services.CreateAsyncScope();
        var community = (await readScope.ServiceProvider.GetRequiredService<ICommunityRepository>().FindByIdAsync(new CommunityId(communityId))).ShouldNotBeNull();
        community.Roles.Select(role => (role.Name, role.Permissions, role.Position))
            .ShouldBe([(CommunityRole.OfficerName, CommunityRole.OfficerPermissions, 1), (CommunityRole.RaidLeaderName, CommunityRole.RaidLeaderPermissions, 2)]);
        community.RolesFor(["111"]).Select(role => role.Name).ShouldBe([CommunityRole.OfficerName, CommunityRole.RaidLeaderName]);
        community.RolesFor(["222"]).Select(role => role.Name).ShouldBe([CommunityRole.RaidLeaderName]);
        community.PermissionsFor(new UserId(Guid.NewGuid()), ["222"]).ShouldBe(CommunityRole.RaidLeaderPermissions);
        community.PermissionsFor(new UserId(Guid.NewGuid()), ["333"]).ShouldBe(CommunityPermissions.None);
        await readScope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database.EnsureDeletedAsync();
    }
    #endregion Tests
}
