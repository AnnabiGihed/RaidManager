using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pivot.Framework.Domain.Shared;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Infrastructure.Features.Communities;

namespace RaidManager.Infrastructure.Tests.Features.Communities;

/// <summary>Verifies that Discord's answers are reused briefly and failures never are.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: A removed role must stop counting once the cache expires, and an outage must not be remembered (ADR-0022).
/// </remarks>
public sealed class CachedDiscordServerMembersTests : IDisposable
{
    #region Fields
    /// <summary>Stores the memory cache under test.</summary>
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>Stores the answers the inner lookup gives, in order.</summary>
    private readonly Queue<Result<DiscordMembership>> _answers = new();

    /// <summary>Stores how many times the inner lookup was asked.</summary>
    private int _calls;
    #endregion Fields

    #region Tests
    /// <summary>Reuses an answer within the cache duration.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnAnswerIsReusedWithinTheCacheDuration()
    {
        _answers.Enqueue(Result.Success(DiscordMembership.Member(["111"])));
        var lookup = CreateLookup(TimeSpan.FromMinutes(1));

        await lookup.FindAsync("1", "2", CancellationToken.None);
        var second = await lookup.FindAsync("1", "2", CancellationToken.None);

        _calls.ShouldBe(1);
        second.Value.RoleIds.ShouldBe(["111"]);
    }

    /// <summary>Asks Discord again once the cached answer expires, so a removed role stops counting.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task DiscordIsAskedAgainAfterTheCacheDuration()
    {
        _answers.Enqueue(Result.Success(DiscordMembership.Member(["111"])));
        _answers.Enqueue(Result.Success(DiscordMembership.Member([])));
        var lookup = CreateLookup(TimeSpan.FromMilliseconds(50));

        await lookup.FindAsync("1", "2", CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        var later = await lookup.FindAsync("1", "2", CancellationToken.None);

        _calls.ShouldBe(2);
        later.Value.RoleIds.ShouldBeEmpty();
    }

    /// <summary>Never reuses a failure: the next check asks Discord again.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AFailureIsNotReused()
    {
        _answers.Enqueue(Result.Failure<DiscordMembership>(DiscordErrors.Unavailable));
        _answers.Enqueue(Result.Success(DiscordMembership.NotMember));
        var lookup = CreateLookup(TimeSpan.FromMinutes(1));

        (await lookup.FindAsync("1", "2", CancellationToken.None)).IsFailure.ShouldBeTrue();
        var retry = await lookup.FindAsync("1", "2", CancellationToken.None);

        _calls.ShouldBe(2);
        retry.Value.IsMember.ShouldBeFalse();
    }

    /// <summary>Keeps each member of each server apart.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task EachMemberOfEachServerIsCachedApart()
    {
        _answers.Enqueue(Result.Success(DiscordMembership.Member(["111"])));
        _answers.Enqueue(Result.Success(DiscordMembership.NotMember));
        var lookup = CreateLookup(TimeSpan.FromMinutes(1));

        await lookup.FindAsync("1", "2", CancellationToken.None);
        var otherServer = await lookup.FindAsync("3", "2", CancellationToken.None);

        _calls.ShouldBe(2);
        otherServer.Value.IsMember.ShouldBeFalse();
    }
    #endregion Tests

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _cache.Dispose();
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Creates the cached lookup over an inner lookup that gives the queued answers.</summary>
    /// <param name="duration">The cache duration.</param>
    /// <returns>The cached lookup.</returns>
    private CachedDiscordServerMembers CreateLookup(TimeSpan duration) =>
        new(new QueuedLookup(this), _cache, Options.Create(new DiscordOptions { BotToken = "unused", MemberCacheDuration = duration }));
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Gives the test's queued answers and counts the calls.</summary>
    /// <param name="test">The test whose queue and counter are used.</param>
    private sealed class QueuedLookup(CachedDiscordServerMembersTests test) : IDiscordServerMembers
    {
        /// <inheritdoc />
        public Task<Result<DiscordMembership>> FindAsync(string discordGuildId, string discordUserId, CancellationToken cancellationToken)
        {
            test._calls++;
            return Task.FromResult(test._answers.Dequeue());
        }
    }
    #endregion Nested Types
}
