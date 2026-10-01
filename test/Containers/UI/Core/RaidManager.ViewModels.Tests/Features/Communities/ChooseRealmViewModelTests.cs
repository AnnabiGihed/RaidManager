using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Verifies the realm choice of board 2.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Finishing needs a realm, links the server Discord confirmed, and handles a server linked in the meantime and an unreachable API.
/// </remarks>
public sealed class ChooseRealmViewModelTests
{
    #region Fields
    /// <summary>Stores the user who added the bot.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the pending link.</summary>
    private static readonly PendingCommunityLink Link = new("987", "Dark Templars", UserId);

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Lists the four Warmane realms.</summary>
    [Fact]
    public void RealmsAreTheWarmaneRealms() => ChooseRealmViewModel.Realms.ShouldBe(["Icecrown", "Lordaeron", "Blackrock", "Onyxia"]);

    /// <summary>Is missing without a valid pending link.</summary>
    [Fact]
    public void NoLinkIsMissing()
    {
        var choice = new ChooseRealmViewModel(_api);

        choice.Initialize(null, "Gihed");

        choice.Status.ShouldBe(CommunityPageStatus.Missing);
        choice.CanFinish.ShouldBeFalse();
    }

    /// <summary>Says who becomes Administrator, and can't finish before a realm is chosen.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FinishingNeedsARealm()
    {
        var choice = Ready("Gihed Annabi");

        choice.Title.ShouldBe("Set up Dark Templars");
        choice.AddedBy.ShouldBe("Added by Gihed Annabi, who becomes its Administrator");
        choice.CanFinish.ShouldBeFalse();
        (await choice.FinishAsync(CancellationToken.None)).ShouldBe(ChooseRealmOutcome.Failed);
        _api.Links.ShouldBeEmpty();
    }

    /// <summary>Links the server with the chosen realm.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FinishingLinksTheServer()
    {
        var choice = Ready(null);
        choice.AddedBy.ShouldBe("Added by you, who becomes its Administrator");
        choice.SelectedRealm = "Onyxia";

        (await choice.FinishAsync(CancellationToken.None)).ShouldBe(ChooseRealmOutcome.Linked);

        _api.Links.ShouldHaveSingleItem().ShouldBe((Link, "Onyxia"));
        choice.CommunityId.ShouldBe(_api.Communities.ShouldHaveSingleItem().CommunityId);
        choice.IsSaving.ShouldBeFalse();
    }

    /// <summary>Opens the existing community when the server was linked in the meantime.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AServerLinkedInTheMeantimeIsAlreadyLinked()
    {
        var existing = FakeCommunitiesApi.Community(Guid.NewGuid(), Link.DiscordGuildId);
        _api.Communities.Add(existing);
        var choice = Ready("Gihed");
        choice.SelectedRealm = "Icecrown";

        (await choice.FinishAsync(CancellationToken.None)).ShouldBe(ChooseRealmOutcome.AlreadyLinked);

        choice.CommunityId.ShouldBe(existing.CommunityId);
    }

    /// <summary>Fails with a notice when the API refuses without the server being linked, or can't be reached.</summary>
    /// <param name="variant">What goes wrong.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("conflict without a community")]
    [InlineData("unreachable")]
    public async Task FailureLeavesANotice(string variant)
    {
        _api.ConflictOnLink = variant == "conflict without a community";
        _api.Failure = variant == "unreachable" ? new HttpRequestException("down") : null;
        var choice = Ready("Gihed");
        choice.SelectedRealm = "Icecrown";

        (await choice.FinishAsync(CancellationToken.None)).ShouldBe(ChooseRealmOutcome.Failed);

        choice.Failure.ShouldNotBeNull().Title.ShouldBe("The community wasn't linked");
        choice.CanFinish.ShouldBeTrue();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a choice ready for the pending link.</summary>
    /// <param name="userName">The signed-in user's name.</param>
    /// <returns>The view model.</returns>
    private ChooseRealmViewModel Ready(string? userName)
    {
        var choice = new ChooseRealmViewModel(_api);
        choice.Initialize(Link, userName);
        choice.Status.ShouldBe(CommunityPageStatus.Ready);
        return choice;
    }
    #endregion Private Helpers
}
