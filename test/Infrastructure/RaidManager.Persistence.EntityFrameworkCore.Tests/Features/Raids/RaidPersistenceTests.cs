using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.Repositories;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Raids;

/// <summary>Verifies that the Raid aggregate persists on PostgreSQL with its targets, signups and roster.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Proves the mapping round-trips every part of the raid, replaces edited child rows, and records events in the outbox.
/// </remarks>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class RaidPersistenceTests
{
    #region Fields
    /// <summary>Stores the PostgreSQL fixture.</summary>
    private readonly PostgreSqlFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RaidPersistenceTests"/> class.</summary>
    /// <param name="database">The PostgreSQL fixture.</param>
    public RaidPersistenceTests(PostgreSqlFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Saves a raid with signups and a roster selection and reloads it in a new scope.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SavedRaidKeepsDetailsSignupsAndRosterWhenReloaded()
    {
        var targets = new[]
        {
            new RaidTarget(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayer),
            new RaidTarget(RaidInstance.Naxxramas, RaidDifficulty.TwentyFivePlayer),
        };
        var raid = CreateRaid(targets);
        raid.OpenForSignups();
        var lateOption = new SignupOption(new CharacterId(Guid.NewGuid()), new LoadoutId(Guid.NewGuid()));
        var secondOption = new SignupOption(new CharacterId(Guid.NewGuid()), new LoadoutId(Guid.NewGuid()));
        var latePlayer = new UserId(Guid.NewGuid());
        var decliningPlayer = new UserId(Guid.NewGuid());
        var lateArrivalUtc = raid.StartsAtUtc.AddMinutes(30);
        raid.SubmitSignup(latePlayer, RaidAvailability.Late, lateArrivalUtc, [lateOption, secondOption], Readiness(raid, lateOption, secondOption), "  After work  ", raid.SignupDeadlineUtc.AddHours(-1));
        raid.SubmitSignup(decliningPlayer, RaidAvailability.Declined, null, [], [], null, raid.SignupDeadlineUtc.AddHours(-1));
        raid.SelectRosterOption(latePlayer, lateOption.CharacterId, lateOption.LoadoutId, Readiness(raid, lateOption).Single(), 2, 3);
        await SaveNewAsync(raid);

        await using var scope = _database.Services.CreateAsyncScope();
        var reloaded = (await scope.ServiceProvider.GetRequiredService<IRaidRepository>().FindByIdAsync(raid.Id)).ShouldNotBeNull();
        reloaded.Title.ShouldBe("Monday ICC");
        reloaded.Description.ShouldBe("Bring flasks.");
        reloaded.CommunityId.ShouldBe(raid.CommunityId);
        reloaded.CreatedByUserId.ShouldBe(raid.CreatedByUserId);
        reloaded.StartsAtUtc.ShouldBe(raid.StartsAtUtc);
        reloaded.SignupDeadlineUtc.ShouldBe(raid.SignupDeadlineUtc);
        reloaded.Requirements.ShouldBe(raid.Requirements);
        reloaded.Status.ShouldBe(RaidStatus.OpenForSignups);
        reloaded.Targets.ShouldBe(targets);
        reloaded.Size.ShouldBe(25);
        reloaded.Audit.ShouldNotBeNull();

        reloaded.Signups.Count.ShouldBe(2);
        var lateSignup = reloaded.Signups.Single(signup => signup.UserId == latePlayer);
        lateSignup.Availability.ShouldBe(RaidAvailability.Late);
        lateSignup.LateArrivalUtc.ShouldBe(lateArrivalUtc);
        lateSignup.Comment.ShouldBe("After work");
        lateSignup.Options.ShouldBe([lateOption, secondOption], ignoreOrder: true);
        reloaded.Signups.Single(signup => signup.UserId == decliningPlayer).Options.ShouldBeEmpty();

        var selection = reloaded.RosterSelections.ShouldHaveSingleItem();
        selection.UserId.ShouldBe(latePlayer);
        selection.CharacterId.ShouldBe(lateOption.CharacterId);
        selection.LoadoutId.ShouldBe(lateOption.LoadoutId);
        selection.GroupNumber.ShouldBe(2);
        selection.Position.ShouldBe(3);

        var context = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>();
        var storedStatus = await context.Database.SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"Raids\" WHERE \"Id\" = {raid.Id.Value}").SingleAsync();
        storedStatus.ShouldBe(nameof(RaidStatus.OpenForSignups));
        context.Model.FindEntityType(typeof(Raid))!.FindProperty(nameof(Raid.Version))!.IsConcurrencyToken.ShouldBeTrue();
        var eventTypes = await context.OutboxMessages.Select(message => message.EventType).ToListAsync();
        eventTypes.ShouldContain(type => type != null && type.Contains(".RaidCreated,", StringComparison.Ordinal));
    }

    /// <summary>Edits a saved raid's targets, signup and roster and checks that the stored rows are replaced.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EditedRaidReplacesItsTargetsOptionsAndRosterRows()
    {
        var raid = CreateRaid([new RaidTarget(RaidInstance.IcecrownCitadel, RaidDifficulty.TenPlayer)]);
        raid.OpenForSignups();
        var player = new UserId(Guid.NewGuid());
        var firstOption = new SignupOption(new CharacterId(Guid.NewGuid()), new LoadoutId(Guid.NewGuid()));
        var nowUtc = raid.SignupDeadlineUtc.AddHours(-1);
        raid.SubmitSignup(player, RaidAvailability.Confirmed, null, [firstOption], Readiness(raid, firstOption), null, nowUtc);
        raid.SelectRosterOption(player, firstOption.CharacterId, firstOption.LoadoutId, Readiness(raid, firstOption).Single(), 1, 1);
        await SaveNewAsync(raid);

        var newTargets = new[]
        {
            new RaidTarget(RaidInstance.RubySanctum, RaidDifficulty.TenPlayerHeroic),
            new RaidTarget(RaidInstance.IcecrownCitadel, RaidDifficulty.TenPlayerHeroic),
        };
        var secondOption = new SignupOption(new CharacterId(Guid.NewGuid()), new LoadoutId(Guid.NewGuid()));
        await using (var editScope = _database.Services.CreateAsyncScope())
        {
            var editable = (await editScope.ServiceProvider.GetRequiredService<IRaidRepository>().FindByIdAsync(raid.Id)).ShouldNotBeNull();
            editable.UpdateDetails("Heroic ICC", null, newTargets, editable.StartsAtUtc, editable.SignupDeadlineUtc, editable.Requirements);
            editable.SubmitSignup(player, RaidAvailability.Tentative, null, [secondOption], Readiness(editable, secondOption), null, nowUtc);
            editable.SelectRosterOption(player, secondOption.CharacterId, secondOption.LoadoutId, Readiness(editable, secondOption).Single(), 2, 4);
            var saved = await editScope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
            saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
        }

        await using var scope = _database.Services.CreateAsyncScope();
        var reloaded = (await scope.ServiceProvider.GetRequiredService<IRaidRepository>().FindByIdAsync(raid.Id)).ShouldNotBeNull();
        reloaded.Title.ShouldBe("Heroic ICC");
        reloaded.Description.ShouldBeNull();
        reloaded.Targets.ShouldBe(newTargets);
        var signup = reloaded.Signups.ShouldHaveSingleItem();
        signup.Availability.ShouldBe(RaidAvailability.Tentative);
        signup.Options.ShouldBe([secondOption]);
        var selection = reloaded.RosterSelections.ShouldHaveSingleItem();
        selection.CharacterId.ShouldBe(secondOption.CharacterId);
        selection.GroupNumber.ShouldBe(2);
        selection.Position.ShouldBe(4);

        var database = scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database;
        (await database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RaidTargets\" WHERE \"RaidId\" = {raid.Id.Value}").SingleAsync()).ShouldBe(2);
        (await database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RosterSelections\" WHERE \"RaidId\" = {raid.Id.Value}").SingleAsync()).ShouldBe(1);
        (await database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RaidSignupOptions\" WHERE \"RaidSignupId\" = {signup.Id.Value}").SingleAsync()).ShouldBe(1);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a draft raid starting in a week, with a signup deadline a day earlier.</summary>
    /// <param name="targets">The required instances and difficulties.</param>
    /// <returns>The draft raid.</returns>
    private static Raid CreateRaid(IEnumerable<RaidTarget> targets)
    {
        var startsAtUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(7).AddHours(19), TimeSpan.Zero);
        return Raid.Create(
            new CommunityId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            "  Monday ICC ",
            targets,
            startsAtUtc,
            startsAtUtc.AddDays(-1),
            new RaidRequirements(new GearScore(5500), true, TimeSpan.FromDays(3)),
            " Bring flasks. ");
    }

    /// <summary>Builds an available readiness for each offered character, current for the raid's targets and start.</summary>
    /// <param name="raid">The raid the characters are assessed for.</param>
    /// <param name="options">The offered character loadouts.</param>
    /// <returns>One readiness per offered character.</returns>
    private static List<CharacterReadiness> Readiness(Raid raid, params SignupOption[] options) =>
        options.Select(option => CharacterReadiness.Create(
            option.CharacterId,
            raid.StartsAtUtc,
            raid.Targets.Select(target => new EligibilityAssessment(target, ReadinessVerdict.Available, null)))).ToList();

    /// <summary>Adds a new raid and commits it through the unit of work in its own scope.</summary>
    /// <param name="raid">The raid to save.</param>
    /// <returns>A task that completes when the raid is committed.</returns>
    private async Task SaveNewAsync(Raid raid)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IRaidRepository>().AddAsync(raid);
        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();
        saved.IsSuccess.ShouldBeTrue(saved.IsFailure ? saved.Error.Message : null);
    }
    #endregion Private Helpers
}
