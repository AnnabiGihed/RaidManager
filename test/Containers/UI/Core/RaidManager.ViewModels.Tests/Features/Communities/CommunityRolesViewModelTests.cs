using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Verifies the roles card: its rows, the picker, and the outcome of each change.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The card words each row as boards 4 and 8 show it, offers only roles not already on a row, and explains every outcome.
/// </remarks>
public sealed class CommunityRolesViewModelTests
{
    #region Fields
    /// <summary>Stores the Officer row's key in the sample card.</summary>
    private static readonly string Officer = FakeCommunitiesApi.OfficerId.ToString();

    /// <summary>Stores the Raid leader row's key in the sample card.</summary>
    private static readonly string RaidLeader = FakeCommunitiesApi.RaidLeaderId.ToString();

    /// <summary>Stores the community.</summary>
    private static readonly Guid CommunityId = Guid.NewGuid();

    /// <summary>Stores the fake API.</summary>
    private readonly FakeCommunitiesApi _api = new();
    #endregion Fields

    #region Tests
    /// <summary>Words each row: sources, chips with "@" or "Deleted role", member counts, and which rows can be edited.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RowsAreWordedForTheAdministrator()
    {
        var roles = await LoadedAsync(canEdit: true);

        roles.Status.ShouldBe(CommunityPageStatus.Ready);
        roles.Rows.Select(row => row.Label).ShouldBe(["Administrator", "Officer", "Raid leader", "Member"]);
        roles.Rows.Select(row => row.Source).ShouldBe(["Added RaidManager to the server", null, null, "Everyone in the Discord server"]);
        roles.Rows.Select(row => row.MembersLabel).ShouldBe(["1 member", "1 member", "0 members", "2 members"]);
        roles.Rows.Select(row => row.Editable).ShouldBe([false, true, true, false]);
        roles.Rows[1].Chips.ShouldHaveSingleItem().ShouldBe(new RoleChipView("12", "@Officier", false));
        roles.Rows[2].Chips.ShouldHaveSingleItem().ShouldBe(new RoleChipView("99", "Deleted role", true));
    }

    /// <summary>Makes no row editable for another member.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task OtherMembersCantEdit() => (await LoadedAsync(canEdit: false)).Rows.ShouldAllBe(row => !row.Editable);

    /// <summary>Offers in the picker only the roles not already on that row, and starts on the first.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PickerOffersTheRolesNotOnTheRow()
    {
        var roles = await LoadedAsync(canEdit: true);

        roles.PickerOptions(Officer).Select(option => option.Name).ShouldBe(["Guild Master", "Veteran"]);
        roles.OpenPicker(Officer);
        roles.PickingFor.ShouldBe(Officer);
        roles.PickedRoleId.ShouldBe("11");
        roles.ClosePicker();
        roles.PickingFor.ShouldBeNull();
        roles.PickedRoleId.ShouldBeNull();
    }

    /// <summary>Maps the picked role, reloads the card and confirms it.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AddingAPickedRoleSavesAndConfirms()
    {
        var roles = await LoadedAsync(canEdit: true);
        roles.OpenPicker(RaidLeader);
        roles.PickedRoleId = "13";

        await roles.AddPickedAsync(CancellationToken.None);

        _api.RoleChanges.ShouldHaveSingleItem().ShouldBe(("13", FakeCommunitiesApi.RaidLeaderId, true));
        roles.JustSaved.ShouldBeTrue();
        roles.PickingFor.ShouldBeNull();
        roles.IsSaving.ShouldBeFalse();
    }

    /// <summary>Removes a mapping and confirms it.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RemovingAMappingSavesAndConfirms()
    {
        var roles = await LoadedAsync(canEdit: true);

        await roles.RemoveAsync("12", Officer, CancellationToken.None);

        _api.RoleChanges.ShouldHaveSingleItem().ShouldBe(("12", FakeCommunitiesApi.OfficerId, false));
        roles.JustSaved.ShouldBeTrue();
    }

    /// <summary>Adds nothing when the picker isn't open.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AddingWithoutAPickerDoesNothing()
    {
        var roles = await LoadedAsync(canEdit: true);

        await roles.AddPickedAsync(CancellationToken.None);

        _api.RoleChanges.ShouldBeEmpty();
    }

    /// <summary>Explains a refused change, Discord unavailable and a removed bot, and changes nothing.</summary>
    /// <param name="status">How the API answers the change.</param>
    /// <param name="message">The expected explanation.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(CommunityApiStatus.Refused, "Only the community's Administrator can change them, with a role the server has.")]
    [InlineData(CommunityApiStatus.DiscordUnavailable, "Discord didn't answer. Nothing changed; try again in a minute.")]
    [InlineData(CommunityApiStatus.BotRemoved, "The RaidManager bot isn't in this Discord server any more. Add it again.")]
    public async Task AFailedChangeIsExplained(CommunityApiStatus status, string message)
    {
        var roles = await LoadedAsync(canEdit: true);
        _api.RoleStatus = status;

        await roles.RemoveAsync("12", Officer, CancellationToken.None);

        roles.JustSaved.ShouldBeFalse();
        roles.Problem.ShouldNotBeNull().Title.ShouldBe("The officer roles weren't changed");
        roles.Problem.Message.ShouldBe(message);
    }

    /// <summary>Explains why the card couldn't be read.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnUnreadableCardIsExplained()
    {
        _api.RoleStatus = CommunityApiStatus.DiscordUnavailable;
        var roles = new CommunityRolesViewModel(_api);

        await roles.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        roles.Status.ShouldBe(CommunityPageStatus.Failed);
        roles.Problem.ShouldNotBeNull().Title.ShouldBe("The officer roles couldn't be shown");
        roles.Rows.ShouldBeEmpty();
        roles.PickerOptions(Officer).ShouldBeEmpty();
    }

    /// <summary>Explains an unreachable API on reading and on changing.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AnUnreachableApiIsExplained()
    {
        var roles = await LoadedAsync(canEdit: true);
        _api.Failure = new HttpRequestException("down");

        await roles.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);

        roles.Status.ShouldBe(CommunityPageStatus.Failed);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Loads the card for the Administrator or another member.</summary>
    /// <param name="canEdit">Whether the user is the Administrator.</param>
    /// <returns>The loaded view model.</returns>
    private async Task<CommunityRolesViewModel> LoadedAsync(bool canEdit)
    {
        _api.RoleSettings[CommunityId] = FakeCommunitiesApi.Card(CommunityId, canEdit);
        var roles = new CommunityRolesViewModel(_api);
        await roles.LoadAsync(Guid.NewGuid(), CommunityId, CancellationToken.None);
        return roles;
    }
    #endregion Private Helpers
}
