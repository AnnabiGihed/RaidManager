using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Shared.Shell;

namespace RaidManager.ViewModels.Tests.Features.Shared.Shell;

/// <summary>Verifies what the app shell shows: sidebar sections, breadcrumb titles and the user card.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Covers the owner's rule that the sidebar lists only existing pages and that only officers see the Officer
/// section, plus the breadcrumb for pages outside the sidebar.
/// </remarks>
public sealed class ShellViewModelTests
{
    #region Fields
    /// <summary>Stores the shell under test, with a flow page and one officer page.</summary>
    private readonly ShellViewModel _shell = new([
        new ShellEntry(ShellSection.Player, "Overview", "/"),
        new ShellEntry(ShellSection.Player, "Review new characters", "/characters/review", InSidebar: false),
        new ShellEntry(ShellSection.Officer, "Schedule", "/schedule"),
    ]);
    #endregion Fields

    #region Tests
    /// <summary>Shows a player only the Player section, without flow pages.</summary>
    [Fact]
    public void PlayerSeesOnlyThePlayerSidebarEntries()
    {
        var navigation = _shell.Navigation(isOfficer: false);

        navigation.Select(group => group.Heading).ShouldBe(["PLAYER"]);
        navigation[0].Entries.Select(entry => entry.Title).ShouldBe(["Overview"]);
    }

    /// <summary>Shows an officer both sections in order.</summary>
    [Fact]
    public void OfficerAlsoSeesTheOfficerSection()
    {
        var navigation = _shell.Navigation(isOfficer: true);

        navigation.Select(group => group.Heading).ShouldBe(["PLAYER", "OFFICER"]);
        navigation[1].Entries.Single().Route.ShouldBe("/schedule");
    }

    /// <summary>Leaves out a section with no page yet.</summary>
    [Fact]
    public void EmptySectionIsLeftOut()
    {
        var shell = new ShellViewModel([new ShellEntry(ShellSection.Player, "Overview", "/")]);

        shell.Navigation(isOfficer: true).Select(group => group.Heading).ShouldBe(["PLAYER"]);
    }

    /// <summary>Names the page at a URL for the breadcrumb.</summary>
    /// <param name="relativeUri">The URL relative to the base.</param>
    /// <param name="expected">The expected title.</param>
    [Theory]
    [InlineData("", "Overview")]
    [InlineData("/", "Overview")]
    [InlineData("?tab=past", "Overview")]
    [InlineData("characters/review?returnUrl=%2Fraids", "Review new characters")]
    [InlineData("Characters/Review/", "Review new characters")]
    [InlineData("schedule#next", "Schedule")]
    public void PageTitleNamesKnownPages(string relativeUri, string expected) => _shell.PageTitle(relativeUri).ShouldBe(expected);

    /// <summary>Leaves the breadcrumb page empty for an unknown URL.</summary>
    [Fact]
    public void UnknownPageHasNoTitle() => _shell.PageTitle("sign-in/failed").ShouldBeNull();

    /// <summary>Builds initials from a display name.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="expected">The expected initials.</param>
    [Theory]
    [InlineData("Arthas Menethil", "AM")]
    [InlineData("anguish", "A")]
    [InlineData("  bryn   de  valewood ", "BD")]
    [InlineData("   ", "?")]
    [InlineData(null, "?")]
    public void InitialsUseTheFirstTwoWords(string? name, string expected) => ShellViewModel.Initials(name).ShouldBe(expected);

    /// <summary>Labels the player's role.</summary>
    [Fact]
    public void RoleLabelNamesOfficersAndPlayers()
    {
        ShellViewModel.RoleLabel(isOfficer: true).ShouldBe("Community officer");
        ShellViewModel.RoleLabel(isOfficer: false).ShouldBe("Player");
    }
    #endregion Tests
}
