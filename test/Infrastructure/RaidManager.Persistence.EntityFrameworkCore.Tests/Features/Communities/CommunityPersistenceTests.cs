using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Communities;

/// <summary>Verifies that the Community aggregate persists on PostgreSQL with its Discord role mappings.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Proves the roles and mappings round-trip, one Discord role keeps several roles, a removed mapping's row goes, one Discord server links once,
/// and communities are listed by name whatever the letter case.
/// </remarks>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class CommunityPersistenceTests
{
    #region Fields
    /// <summary>Stores the PostgreSQL fixture.</summary>
    private readonly PostgreSqlFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityPersistenceTests"/> class.</summary>
    /// <param name="database">The PostgreSQL fixture.</param>
    public CommunityPersistenceTests(PostgreSqlFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Saves a community with role mappings and reloads it in a new scope.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SavedCommunityKeepsItsServerRealmAdministratorAndRoleMappingsWhenReloaded()
    {
        var administrator = new UserId(Guid.NewGuid());
        var community = Community.Link(NewGuildId(), "Citadel Vanguard", WarmaneRealm.Lordaeron, administrator);
        var officer = community.Roles[0].Id;
        var raidLeader = community.Roles[1].Id;
        community.MapDiscordRole("1001", officer);
        community.MapDiscordRole("1002", raidLeader);
        (await SaveNewAsync(community)).ShouldBeTrue();

        await using var scope = _database.Services.CreateAsyncScope();
        var reloaded = (await scope.ServiceProvider.GetRequiredService<ICommunityRepository>().FindByIdAsync(community.Id)).ShouldNotBeNull();
        reloaded.DiscordGuildId.ShouldBe(community.DiscordGuildId);
        reloaded.Name.ShouldBe("Citadel Vanguard");
        reloaded.Realm.ShouldBe(WarmaneRealm.Lordaeron);
        reloaded.AdministratorId.ShouldBe(administrator);
        reloaded.Roles.Select(role => (role.Id, role.Name, role.Permissions))
            .ShouldBe([(officer, CommunityRole.OfficerName, CommunityRole.OfficerPermissions), (raidLeader, CommunityRole.RaidLeaderName, CommunityRole.RaidLeaderPermissions)]);
        reloaded.PermissionsFor(new UserId(Guid.NewGuid()), ["1002"]).ShouldBe(CommunityRole.RaidLeaderPermissions);
        reloaded.RoleMappings.Count.ShouldBe(2);
        reloaded.Audit.ShouldNotBeNull();

        var context = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>();
        var storedRealm = await context.Database.SqlQuery<string>($"SELECT \"Realm\" AS \"Value\" FROM \"Communities\" WHERE \"Id\" = {community.Id.Value}").SingleAsync();
        storedRealm.ShouldBe(nameof(WarmaneRealm.Lordaeron));
        var eventTypes = await context.OutboxMessages.Select(message => message.EventType).ToListAsync();
        eventTypes.ShouldContain(type => type != null && type.Contains(".CommunityCreated,", StringComparison.Ordinal));
    }

    /// <summary>Gives one Discord role a second RaidManager role and removes another mapping on a saved community.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ADiscordRoleKeepsBothRolesAndARemovedMappingGoes()
    {
        var community = Community.Link(NewGuildId(), "Frozen Throne", WarmaneRealm.Icecrown, new UserId(Guid.NewGuid()));
        var officer = community.Roles[0].Id;
        var raidLeader = community.Roles[1].Id;
        community.MapDiscordRole("2001", officer);
        community.MapDiscordRole("2002", officer);
        (await SaveNewAsync(community)).ShouldBeTrue();

        await using (var editScope = _database.Services.CreateAsyncScope())
        {
            var editable = (await editScope.ServiceProvider.GetRequiredService<ICommunityRepository>().FindByIdAsync(community.Id)).ShouldNotBeNull();
            editable.MapDiscordRole("2001", raidLeader);
            editable.UnmapDiscordRole("2002", officer);
            var saved = await editScope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
            saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
        }

        await using var scope = _database.Services.CreateAsyncScope();
        var reloaded = (await scope.ServiceProvider.GetRequiredService<ICommunityRepository>().FindByIdAsync(community.Id)).ShouldNotBeNull();
        reloaded.RolesFor(["2001"]).Select(role => role.Id).ShouldBe([officer, raidLeader]);
        reloaded.RolesFor(["2002"]).ShouldBeEmpty();
        var rows = await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"CommunityRoleMappings\" WHERE \"CommunityId\" = {community.Id.Value}").SingleAsync();
        rows.ShouldBe(2);
    }

    /// <summary>Refuses to link a Discord server that another community already links.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ADiscordServerLinksToOneCommunityOnly()
    {
        var guildId = NewGuildId();
        (await SaveNewAsync(Community.Link(guildId, "First link", WarmaneRealm.Icecrown, new UserId(Guid.NewGuid())))).ShouldBeTrue();

        var secondLink = Community.Link(guildId, "Second link", WarmaneRealm.Icecrown, new UserId(Guid.NewGuid()));

        (await SaveNewAsync(secondLink)).ShouldBeFalse();
    }

    /// <summary>Lists an administrator's communities by name whatever the letter case, as on SQL Server (ADR-0029).</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CommunitiesAreListedByNameWhateverTheirLetterCase()
    {
        var administrator = User.Register(DiscordUserId.Create(NewGuildId()), "Ordering administrator", null);
        await using (var scope = _database.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(administrator);
            (await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync()).IsSuccess.ShouldBeTrue();
        }

        foreach (var name in new[] { "beta Raiders", "Charlie Company", "alpha Guild" })
        {
            (await SaveNewAsync(Community.Link(NewGuildId(), name, WarmaneRealm.Icecrown, administrator.Id))).ShouldBeTrue();
        }

        await using var readScope = _database.Services.CreateAsyncScope();
        var listed = await readScope.ServiceProvider.GetRequiredService<ICommunityReader>().ListAdministeredByAsync(administrator.Id, CancellationToken.None);

        listed.Select(community => community.Name).ShouldBe(["alpha Guild", "beta Raiders", "Charlie Company"]);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a Discord server id that no other test uses.</summary>
    /// <returns>A numeric snowflake.</returns>
    private static string NewGuildId() =>
        Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Adds a new community and commits it through the unit of work in its own scope.</summary>
    /// <param name="community">The community to save.</param>
    /// <returns><see langword="true"/> when the unit of work committed it; <see langword="false"/> when the database refused it.</returns>
    private async Task<bool> SaveNewAsync(Community community)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ICommunityRepository>().AddAsync(community);

        try
        {
            return (await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync()).IsSuccess;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
    #endregion Private Helpers
}
