using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Verifies the Overview's loading and its explanations of an unfinished link.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The Overview knows whether the user has a community, says why adding the bot stopped, and reports an unreachable API.
/// </remarks>
public sealed class OverviewViewModelTests
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Names each reason adding the bot stopped, and nothing for an empty, unknown or numeric reason.</summary>
    /// <param name="reason">The reason in the return address.</param>
    /// <param name="title">The expected title, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("cancelled", "RaidManager wasn't added")]
    [InlineData("Failed", "RaidManager couldn't finish adding the bot")]
    [InlineData("expired", "That took too long")]
    [InlineData("otheraccount", "Another Discord account added the bot")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("hacked", null)]
    [InlineData("1", null)]
    public void EachReasonHasItsNotice(string? reason, string? title) =>
        OverviewViewModel.NoticeFor(reason)?.Title.ShouldBe(title);

    /// <summary>Shows no community when the user has none, with the reason the last attempt stopped.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UserWithoutACommunitySeesTheStepsAndTheReason()
    {
        var overview = new OverviewViewModel(_api);

        await overview.LoadAsync(UserId, "cancelled", CancellationToken.None);

        overview.Status.ShouldBe(CommunityPageStatus.Ready);
        overview.Community.ShouldBeNull();
        overview.Failure.ShouldNotBeNull().Message.ShouldBe("You cancelled on Discord's page. Nothing was linked.");
        OverviewViewModel.Steps.Count.ShouldBe(3);
    }

    /// <summary>Finds the user's community.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UserWithACommunityHasIt()
    {
        _api.Communities.Add(FakeCommunitiesApi.Community(UserId));
        var overview = new OverviewViewModel(_api);

        await overview.LoadAsync(UserId, null, CancellationToken.None);

        overview.Community.ShouldNotBeNull().Name.ShouldBe("Dark Templars");
        overview.Failure.ShouldBeNull();
    }

    /// <summary>Reports an unreachable API instead of showing no community.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnreachableApiFails()
    {
        _api.Failure = new HttpRequestException("down");
        var overview = new OverviewViewModel(_api);

        await overview.LoadAsync(UserId, null, CancellationToken.None);

        overview.Status.ShouldBe(CommunityPageStatus.Failed);
    }
    #endregion Tests
}
