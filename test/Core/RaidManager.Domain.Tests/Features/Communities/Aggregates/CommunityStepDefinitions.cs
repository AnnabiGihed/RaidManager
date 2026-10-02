using Pivot.Framework.Domain.Exceptions;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Events;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Communities.Aggregates;

/// <summary>Defines business-readable steps for linking a community and giving roles from Discord roles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Verifies that the installer administers the community, that it starts with the Officer and Raid leader
/// presets, that role mappings report real changes only, and that a member gets every role and permission their
/// Discord roles map to.
/// </remarks>
[Binding]
[Scope(Feature = "Community linking and roles")]
public sealed class CommunityStepDefinitions
{
    #region Fields
    /// <summary>Stores the user who adds the bot in every scenario.</summary>
    private static readonly UserId Installer = new(Guid.NewGuid());

    /// <summary>Stores the community under test when linking succeeded.</summary>
    private Community? _community;

    /// <summary>Stores the domain exception raised when a change was rejected.</summary>
    private DomainException? _rejection;

    /// <summary>Stores the Discord roles of the member the last check was for.</summary>
    private string[] _memberDiscordRoles = [];

    /// <summary>Stores the result of the last role change.</summary>
    private Result? _roleChange;

    /// <summary>Stores how many role-mapping changes the community had reported when the scenario's setup ended.</summary>
    private int _changesBeforeAction;
    #endregion Fields

    #region Properties
    /// <summary>Gets the linked community, failing the scenario when linking did not succeed.</summary>
    private Community Community => _community ?? throw new InvalidOperationException("No community was linked in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Links a community.</summary>
    [Given("a linked community")]
    public void GivenALinkedCommunity() =>
        _community = Community.Link("123456789012345678", "Citadel Vanguard", WarmaneRealm.Icecrown, Installer);

    /// <summary>Maps a Discord role before the scenario's action, which then counts only its own changes.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The RaidManager role it gives.</param>
    [Given("the Discord role {string} gives {string}")]
    public void GivenTheDiscordRoleGives(string discordRoleId, string role)
    {
        Community.MapDiscordRole(discordRoleId, RoleNamed(role));
        _changesBeforeAction = RoleMappingChanges();
    }

    /// <summary>Creates a role as the Administrator before the scenario's action.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What the role allows.</param>
    [Given("the community has the role {string} allowing {string}")]
    public void GivenTheCommunityHasTheRole(string name, string permissions) =>
        Community.CreateRole(name, Enum.Parse<CommunityPermissions>(permissions), byAdministrator: true).IsSuccess.ShouldBeTrue();
    #endregion Given Steps

    #region When Steps
    /// <summary>Attempts to link a Discord server.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="name">The server name.</param>
    /// <param name="realm">The Warmane realm.</param>
    [When("a user links the Discord server {string} named {string} on {WarmaneRealm}")]
    public void WhenAUserLinksTheDiscordServer(string discordGuildId, string name, WarmaneRealm realm) =>
        Attempt(() => _community = Community.Link(discordGuildId, name, realm, Installer));

    /// <summary>Attempts to link a Discord server whose name has a given length.</summary>
    /// <param name="length">The name length.</param>
    [When("a user links a Discord server with a name of {int} characters")]
    public void WhenAUserLinksADiscordServerWithANameOfCharacters(int length) =>
        Attempt(() => _community = Community.Link("123456789012345678", new string('a', length), WarmaneRealm.Icecrown, Installer));

    /// <summary>Attempts to map a Discord role.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The RaidManager role it should give.</param>
    [When("the Administrator maps the Discord role {string} to {string}")]
    public void WhenTheAdministratorMapsTheDiscordRole(string discordRoleId, string role) =>
        Attempt(() => Community.MapDiscordRole(discordRoleId, RoleNamed(role)));

    /// <summary>Attempts to map a Discord role to a role id the community doesn't have.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    [When("the Administrator maps the Discord role {string} to a role the community doesn't have")]
    public void WhenTheAdministratorMapsTheDiscordRoleToAnUnknownRole(string discordRoleId) =>
        Attempt(() => Community.MapDiscordRole(discordRoleId, new CommunityRoleId(Guid.NewGuid())));

    /// <summary>Stops a Discord role giving one of the community's roles.</summary>
    /// <param name="role">The role name.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    [When("the Administrator removes the {string} mapping of the Discord role {string}")]
    public void WhenTheAdministratorRemovesTheMapping(string role, string discordRoleId) => Community.UnmapDiscordRole(discordRoleId, RoleNamed(role));

    /// <summary>Records the Discord roles of a member other than the Administrator.</summary>
    /// <param name="discordRoleIds">The member's Discord role snowflakes, comma-separated; empty for none.</param>
    [When("a member has the Discord roles {string}")]
    public void WhenAMemberHasTheDiscordRoles(string discordRoleIds) =>
        _memberDiscordRoles = discordRoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Creates a role as the Administrator or as a role manager.</summary>
    /// <param name="who">Who asks: <c>the Administrator</c> or <c>a role manager</c>.</param>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">What the role should allow.</param>
    [When(@"^(the Administrator|a role manager) creates the role ""(.*)"" allowing ""(.*)""$")]
    public void WhenCreatesTheRole(string who, string name, string permissions) =>
        _roleChange = Community.CreateRole(name, Enum.Parse<CommunityPermissions>(permissions), IsAdministrator(who));

    /// <summary>Changes a role as the Administrator or as a role manager.</summary>
    /// <param name="who">Who asks.</param>
    /// <param name="role">The role's current name; an unknown name stands for a role the community doesn't have.</param>
    /// <param name="name">The new name.</param>
    /// <param name="permissions">What the role should allow.</param>
    [When(@"^(the Administrator|a role manager) changes the role ""(.*)"" to ""(.*)"" allowing ""(.*)""$")]
    public void WhenChangesTheRole(string who, string role, string name, string permissions) =>
        _roleChange = Community.UpdateRole(RoleOrUnknown(role), name, Enum.Parse<CommunityPermissions>(permissions), IsAdministrator(who));

    /// <summary>Deletes a role as the Administrator or as a role manager.</summary>
    /// <param name="who">Who asks.</param>
    /// <param name="role">The role's name; an unknown name stands for a role the community doesn't have.</param>
    [When(@"^(the Administrator|a role manager) deletes the role ""(.*)""$")]
    public void WhenDeletesTheRole(string who, string role) => _roleChange = Community.DeleteRole(RoleOrUnknown(role), IsAdministrator(who));
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the community name.</summary>
    /// <param name="name">The expected name.</param>
    [Then("the community is named {string}")]
    public void ThenTheCommunityIsNamed(string name) => Community.Name.ShouldBe(name);

    /// <summary>Checks that the installer administers the community with every permission, whatever their Discord roles.</summary>
    [Then("the installer has every permission")]
    public void ThenTheInstallerHasEveryPermission()
    {
        Community.AdministratorId.ShouldBe(Installer);
        Community.PermissionsFor(Installer, []).ShouldBe(CommunityPermissions.All);
    }

    /// <summary>Checks the community's roles, in their list order, with what each allows.</summary>
    /// <param name="table">The expected roles: role name and permissions.</param>
    [Then("the community's roles are")]
    public void ThenTheCommunitysRolesAre(Table table) =>
        Community.Roles.Select(role => (role.Name, role.Permissions))
            .ShouldBe(table.Rows.Select(row => (row["role"], Enum.Parse<CommunityPermissions>(row["permissions"]))));

    /// <summary>Checks that the last change was rejected.</summary>
    [Then("the community change is rejected")]
    public void ThenTheCommunityChangeIsRejected() => _rejection.ShouldNotBeNull();

    /// <summary>Checks the role a Discord role gives.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The expected RaidManager role.</param>
    [Then("the Discord role {string} gives {string}")]
    public void ThenTheDiscordRoleGives(string discordRoleId, string role) =>
        Community.RoleMappings.ShouldContain(mapping => mapping.DiscordRoleId == discordRoleId && mapping.RoleId == RoleNamed(role));

    /// <summary>Checks the number of mapped Discord roles.</summary>
    /// <param name="count">The expected count.</param>
    [Then("the community has {int} role mapping(s)")]
    public void ThenTheCommunityHasRoleMappings(int count) => Community.RoleMappings.Count.ShouldBe(count);

    /// <summary>Checks that exactly one role-mapping change was reported.</summary>
    [Then("the role mappings changed once")]
    public void ThenTheRoleMappingsChangedOnce()
    {
        (RoleMappingChanges() - _changesBeforeAction).ShouldBe(1);
        Community.GetDomainEvents().OfType<CommunityRoleMappingsChanged>().ShouldAllBe(change => change.CommunityId == Community.Id);
    }

    /// <summary>Checks that no role-mapping change was reported.</summary>
    [Then("the role mappings did not change")]
    public void ThenTheRoleMappingsDidNotChange() => RoleMappingChanges().ShouldBe(_changesBeforeAction);

    /// <summary>Checks the roles the member's Discord roles give, in list order.</summary>
    /// <param name="roles">The expected role names, comma-separated; empty for none.</param>
    [Then("the member's roles are {string}")]
    public void ThenTheMembersRolesAre(string roles) =>
        string.Join(", ", Community.RolesFor(_memberDiscordRoles).Select(role => role.Name)).ShouldBe(roles);

    /// <summary>Checks the permissions the member's Discord roles give.</summary>
    /// <param name="permissions">The expected permissions.</param>
    [Then("the member's permissions are {string}")]
    public void ThenTheMembersPermissionsAre(string permissions) =>
        Community.PermissionsFor(new UserId(Guid.NewGuid()), _memberDiscordRoles).ShouldBe(Enum.Parse<CommunityPermissions>(permissions));

    /// <summary>Checks that the last role change succeeded.</summary>
    [Then("the role change succeeds")]
    public void ThenTheRoleChangeSucceeds()
    {
        var change = _roleChange.ShouldNotBeNull();
        change.IsSuccess.ShouldBeTrue(change.IsFailure ? change.Error.Code : null);
    }

    /// <summary>Checks the error the last role change failed with.</summary>
    /// <param name="code">The error code.</param>
    [Then("the role change fails with {string}")]
    public void ThenTheRoleChangeFailsWith(string code)
    {
        var change = _roleChange.ShouldNotBeNull();
        change.IsFailure.ShouldBeTrue();
        change.Error.Code.ShouldBe(code);
    }

    /// <summary>Checks what a member with some Discord roles may do.</summary>
    /// <param name="discordRoleIds">The member's Discord role snowflakes, comma-separated.</param>
    /// <param name="permissions">The expected permissions.</param>
    [Then("a member with the Discord roles {string} may {string}")]
    public void ThenAMemberWithTheDiscordRolesMay(string discordRoleIds, string permissions) =>
        Community.PermissionsFor(new UserId(Guid.NewGuid()), discordRoleIds.Split(',')).ShouldBe(Enum.Parse<CommunityPermissions>(permissions));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Tells whether the scenario's wording means the Administrator.</summary>
    /// <param name="who">The wording.</param>
    /// <returns><see langword="true"/> for the Administrator.</returns>
    private static bool IsAdministrator(string who) => who == "the Administrator";

    /// <summary>Finds one of the community's roles by name, or makes up an id for a name it doesn't have.</summary>
    /// <param name="name">The role name.</param>
    /// <returns>The role's identifier.</returns>
    private CommunityRoleId RoleOrUnknown(string name) => Community.Roles.FirstOrDefault(role => role.Name == name)?.Id ?? new CommunityRoleId(Guid.NewGuid());

    /// <summary>Finds one of the community's roles by name.</summary>
    /// <param name="name">The role name.</param>
    /// <returns>The role's identifier.</returns>
    private CommunityRoleId RoleNamed(string name) => Community.Roles.Single(role => role.Name == name).Id;

    /// <summary>Counts the role-mapping changes the community has reported so far.</summary>
    /// <returns>The number of <see cref="CommunityRoleMappingsChanged"/> events.</returns>
    private int RoleMappingChanges() => Community.GetDomainEvents().OfType<CommunityRoleMappingsChanged>().Count();

    /// <summary>Runs a change and records its rejection instead of failing the scenario.</summary>
    /// <param name="change">The change to attempt.</param>
    private void Attempt(Action change)
    {
        try
        {
            change();
        }
        catch (DomainException exception)
        {
            _rejection = exception;
        }
    }
    #endregion Private Helpers
}
