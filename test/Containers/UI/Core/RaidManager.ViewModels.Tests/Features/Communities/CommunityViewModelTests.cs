using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Verifies the loading and wording of the already-linked and community pages.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The view model finds a community by id or by user, and reports a missing community and an unreachable API.
/// </remarks>
public sealed class CommunityViewModelTests
{
    #region Fields
    /// <summary>Stores the Administrator.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Loads a community by id and words its pages.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CommunityByIdIsWorded()
    {
        var community = FakeCommunitiesApi.Community(UserId);
        _api.Communities.Add(community);
        var page = new CommunityViewModel(_api);

        await page.LoadAsync(community.CommunityId, CancellationToken.None);

        page.Status.ShouldBe(CommunityPageStatus.Ready);
        page.AlreadyLinkedTitle.ShouldBe("Dark Templars is already linked");
        page.AdministratorLine.ShouldBe("Its Administrator, Gihed Annabi, manages the realm and the officer roles.");
        page.LinkedTitle.ShouldBe("Dark Templars is linked");
        page.LinkedOn.ShouldBe("Discord server linked to RaidManager on Icecrown.");
    }

    /// <summary>Loads the user's community.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UsersCommunityIsFound()
    {
        _api.Communities.Add(FakeCommunitiesApi.Community(UserId));
        var page = new CommunityViewModel(_api);

        await page.LoadForUserAsync(UserId, [], CancellationToken.None);

        page.Community.ShouldNotBeNull().Name.ShouldBe("Dark Templars");
    }

    /// <summary>Is missing for no id, an empty id, an unknown id, or a user without a community, and words nothing.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MissingCommunitiesAreMissing()
    {
        var page = new CommunityViewModel(_api);

        await page.LoadAsync(null, CancellationToken.None);
        page.Status.ShouldBe(CommunityPageStatus.Missing);
        await page.LoadAsync(Guid.Empty, CancellationToken.None);
        page.Status.ShouldBe(CommunityPageStatus.Missing);
        await page.LoadAsync(Guid.NewGuid(), CancellationToken.None);
        page.Status.ShouldBe(CommunityPageStatus.Missing);
        await page.LoadForUserAsync(UserId, [], CancellationToken.None);
        page.Status.ShouldBe(CommunityPageStatus.Missing);
        page.AlreadyLinkedTitle.ShouldBeEmpty();
        page.AdministratorLine.ShouldBeEmpty();
        page.LinkedTitle.ShouldBeEmpty();
        page.LinkedOn.ShouldBeEmpty();
    }

    /// <summary>Reports an unreachable API.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnreachableApiFails()
    {
        _api.Failure = new TaskCanceledException("timeout");
        var page = new CommunityViewModel(_api);

        await page.LoadForUserAsync(UserId, [], CancellationToken.None);

        page.Status.ShouldBe(CommunityPageStatus.Failed);
    }
    #endregion Tests
}
