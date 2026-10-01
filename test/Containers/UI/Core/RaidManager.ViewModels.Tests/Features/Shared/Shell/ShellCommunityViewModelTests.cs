using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.ViewModels.Tests.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Shared.Shell;

/// <summary>Verifies the sidebar's community card and the user's role label.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The card shows the user's community or the hint to link one, the Administrator is labeled so, and a failed call shows no community.
/// </remarks>
public sealed class ShellCommunityViewModelTests
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Shows the hint to link a server when the user has no community.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NoCommunityShowsTheHint()
    {
        var shell = new ShellCommunityViewModel(_api);

        await shell.LoadAsync(UserId, CancellationToken.None);

        shell.CardTitle.ShouldBe("No community yet");
        shell.CardSubtitle.ShouldBe("Link a Discord server");
        shell.CardGlyph.ShouldBe("+");
        shell.RoleLabel.ShouldBe("Player");
    }

    /// <summary>Shows the Administrator's community and role.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AdministratorSeesTheirCommunity()
    {
        _api.Communities.Add(FakeCommunitiesApi.Community(UserId));
        var shell = new ShellCommunityViewModel(_api);

        await shell.LoadAsync(UserId, CancellationToken.None);

        shell.CardTitle.ShouldBe("Dark Templars");
        shell.CardSubtitle.ShouldBe("Icecrown · Community");
        shell.CardGlyph.ShouldBe("DT");
        shell.RoleLabel.ShouldBe(ShellCommunityViewModel.AdministratorLabel);
    }

    /// <summary>Shows no community when the API can't answer, instead of breaking the shell.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task UnreachableApiShowsNoCommunity()
    {
        _api.Failure = new HttpRequestException("down");
        var shell = new ShellCommunityViewModel(_api);

        await shell.LoadAsync(UserId, CancellationToken.None);

        shell.Community.ShouldBeNull();
        shell.CardTitle.ShouldBe("No community yet");
    }
    #endregion Tests
}
