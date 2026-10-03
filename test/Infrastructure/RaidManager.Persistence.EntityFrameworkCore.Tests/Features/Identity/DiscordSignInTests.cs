using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Persistence.EntityFrameworkCore.Tests.Support;
using DomainUnitOfWork = Pivot.Framework.Domain.Repositories.IUnitOfWork;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Features.Identity;

/// <summary>Verifies against PostgreSQL that a Discord account always resolves to one local user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves the sign-in command reuses the user across sign-ins and that the database refuses a duplicate Discord account.
/// </remarks>
[Collection(PostgreSqlTestGroup.Name)]
public sealed class DiscordSignInTests
{
    #region Fields
    /// <summary>Stores the PostgreSQL fixture.</summary>
    private readonly PostgreSqlFixture _database;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordSignInTests"/> class.</summary>
    /// <param name="database">The PostgreSQL fixture.</param>
    public DiscordSignInTests(PostgreSqlFixture database)
    {
        _database = database;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Signs the same Discord account in twice and finds one user with the same id.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task SigningInTwiceReachesTheSameSingleUser()
    {
        var discordId = NewDiscordId();

        var first = await _database.SendAsync(new SignInWithDiscordCommand(discordId, "Arthas", null));
        var second = await _database.SendAsync(new SignInWithDiscordCommand(discordId, "Arthas", null));

        first.IsSuccess.ShouldBeTrue(first.IsFailure ? first.Error.Message : null);
        second.IsSuccess.ShouldBeTrue(second.IsFailure ? second.Error.Message : null);
        second.Value.ShouldBe(first.Value);
        (await CountUsersAsync(discordId)).ShouldBe(1);
    }

    /// <summary>Signs in again with a new display name and avatar and reloads the stored profile.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReturningPlayerWithANewProfileIsUpdated()
    {
        var discordId = NewDiscordId();
        await _database.SendAsync(new SignInWithDiscordCommand(discordId, "Arthas", null));

        var result = await _database.SendAsync(
            new SignInWithDiscordCommand(discordId, "Arthas the Pure", "https://cdn.discordapp.com/avatars/1/new.png"));

        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.Message : null);
        await using var scope = _database.Services.CreateAsyncScope();
        var user = (await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .FindByDiscordIdAsync(DiscordUserId.Create(discordId), CancellationToken.None)).ShouldNotBeNull();
        user.Id.Value.ShouldBe(result.Value);
        user.DisplayName.ShouldBe("Arthas the Pure");
        user.AvatarUrl.ShouldBe("https://cdn.discordapp.com/avatars/1/new.png");
    }

    /// <summary>Tries to store a second user for the same Discord account, bypassing the command.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task DatabaseRefusesASecondUserForTheSameDiscordAccount()
    {
        var discordId = NewDiscordId();
        await _database.SendAsync(new SignInWithDiscordCommand(discordId, "Arthas", null));

        await using var scope = _database.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .AddAsync(User.Register(DiscordUserId.Create(discordId), "Impostor", null));
        var saved = await scope.ServiceProvider.GetRequiredService<DomainUnitOfWork>().SaveChangesAsync();

        saved.IsFailure.ShouldBeTrue();
        (await CountUsersAsync(discordId)).ShouldBe(1);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a Discord snowflake no other test uses.</summary>
    /// <returns>A numeric Discord user identifier.</returns>
    private static string NewDiscordId() => Random.Shared.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999)
        .ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Counts the stored users linked to a Discord account.</summary>
    /// <param name="discordId">The Discord account identifier.</param>
    /// <returns>The number of users.</returns>
    private async Task<int> CountUsersAsync(string discordId)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var discordUserId = DiscordUserId.Create(discordId);
        return await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Users
            .CountAsync(user => user.DiscordUserId == discordUserId);
    }
    #endregion Private Helpers
}
