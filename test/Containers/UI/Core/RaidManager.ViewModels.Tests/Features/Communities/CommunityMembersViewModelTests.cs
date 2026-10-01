using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Verifies the members page: its rows, the checked note, and its failures.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page words each member as board 5 shows them, says in UTC when Discord was asked, and never shows an old list after a failure.
/// </remarks>
public sealed class CommunityMembersViewModelTests
{
    #region Fields
    /// <summary>Stores the community.</summary>
    private static readonly Guid CommunityId = Guid.NewGuid();

    /// <summary>Stores the instant the tests treat as now: 1 October 2026, 18:45 UTC.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 18, 45, 0, TimeSpan.Zero);

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Words each member and says when Discord was asked today.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MembersAreWordedWithTodaysCheck()
    {
        _api.MemberLists[CommunityId] = FakeCommunitiesApi.MemberList(CommunityId, Now.AddMinutes(-5));
        var members = Create();

        await members.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        members.Status.ShouldBe(CommunityPageStatus.Ready);
        members.Rows.Select(row => (row.DisplayName, row.DiscordRoles, row.RoleLabel)).ShouldBe([("Malarya", "@Officier, @Veteran", "Officer"), ("OrlkDemon", "No roles", "Member")]);
        members.Rows[0].AvatarUrl.ShouldBe("https://cdn.discordapp.com/avatars/1/a.png");
        members.CheckedNote.ShouldBe("Last checked with Discord today at 18:40 UTC.");
    }

    /// <summary>Gives the date of an answer from another day.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnOlderCheckGivesItsDate()
    {
        _api.MemberLists[CommunityId] = FakeCommunitiesApi.MemberList(CommunityId, new DateTimeOffset(2026, 9, 30, 21, 5, 0, TimeSpan.Zero));
        var members = Create();

        await members.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        members.CheckedNote.ShouldBe("Last checked with Discord on 30 September at 21:05 UTC.");
    }

    /// <summary>Explains each failure and keeps no list.</summary>
    /// <param name="status">How the API answers.</param>
    /// <param name="title">The expected title.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(CommunityApiStatus.Refused, "You can't see these members")]
    [InlineData(CommunityApiStatus.BotRemoved, "The members couldn't be shown")]
    [InlineData(CommunityApiStatus.DiscordUnavailable, "The members couldn't be shown")]
    public async Task FailuresAreExplainedWithoutAList(CommunityApiStatus status, string title)
    {
        var members = Create();
        await members.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);
        _api.RoleStatus = status;

        await members.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        members.Status.ShouldBe(CommunityPageStatus.Failed);
        members.Rows.ShouldBeEmpty();
        members.Problem.ShouldNotBeNull().Title.ShouldBe(title);
    }

    /// <summary>Explains an unreachable API.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnUnreachableApiIsExplained()
    {
        _api.Failure = new HttpRequestException("down");
        var members = Create();

        await members.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        members.Problem.ShouldNotBeNull().Message.ShouldBe("Discord didn't answer. Try again in a minute.");
    }

    /// <summary>Shows the members as unavailable without asking the API.</summary>
    [Fact]
    public void ShowUnavailableExplainsWithoutAsking()
    {
        var members = Create();

        members.ShowUnavailable();

        members.Status.ShouldBe(CommunityPageStatus.Failed);
        members.Rows.ShouldBeEmpty();
        members.Problem.ShouldNotBeNull().Title.ShouldBe("The members couldn't be shown");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates the view model with the tests' clock.</summary>
    /// <returns>The view model.</returns>
    private CommunityMembersViewModel Create() => new(_api, new FixedClock(Now));
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Answers a fixed instant as now.</summary>
    /// <param name="now">The instant.</param>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
    #endregion Nested Types
}
