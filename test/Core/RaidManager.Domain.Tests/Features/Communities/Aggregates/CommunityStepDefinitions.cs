using Pivot.Framework.Domain.Exceptions;
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
/// Purpose: Verifies that the installer administers the community, that role mappings report real changes only, and
/// that a member gets the highest role their Discord roles map to.
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

    /// <summary>Stores the role the last member check gave.</summary>
    private CommunityMemberRole? _memberRole;

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
    [Given("the Discord role {string} gives {CommunityMemberRole}")]
    public void GivenTheDiscordRoleGives(string discordRoleId, CommunityMemberRole role)
    {
        Community.MapDiscordRole(discordRoleId, role);
        _changesBeforeAction = RoleMappingChanges();
    }
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
    [When("the Administrator maps the Discord role {string} to {CommunityMemberRole}")]
    public void WhenTheAdministratorMapsTheDiscordRole(string discordRoleId, CommunityMemberRole role) =>
        Attempt(() => Community.MapDiscordRole(discordRoleId, role));

    /// <summary>Removes a Discord role's mapping.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    [When("the Administrator removes the mapping of the Discord role {string}")]
    public void WhenTheAdministratorRemovesTheMapping(string discordRoleId) => Community.UnmapDiscordRole(discordRoleId);

    /// <summary>Gives the role of a member other than the Administrator from their Discord roles.</summary>
    /// <param name="discordRoleIds">The member's Discord role snowflakes, comma-separated; empty for none.</param>
    [When("a member has the Discord roles {string}")]
    public void WhenAMemberHasTheDiscordRoles(string discordRoleIds) =>
        _memberRole = Community.RoleFor(new UserId(Guid.NewGuid()), discordRoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the community name.</summary>
    /// <param name="name">The expected name.</param>
    [Then("the community is named {string}")]
    public void ThenTheCommunityIsNamed(string name) => Community.Name.ShouldBe(name);

    /// <summary>Checks that the installer administers the community, whatever their Discord roles.</summary>
    [Then("the installer's role is Administrator")]
    public void ThenTheInstallersRoleIsAdministrator()
    {
        Community.AdministratorId.ShouldBe(Installer);
        Community.RoleFor(Installer, []).ShouldBe(CommunityMemberRole.Administrator);
    }

    /// <summary>Checks that the last change was rejected.</summary>
    [Then("the community change is rejected")]
    public void ThenTheCommunityChangeIsRejected() => _rejection.ShouldNotBeNull();

    /// <summary>Checks the role a Discord role gives.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The expected RaidManager role.</param>
    [Then("the Discord role {string} gives {CommunityMemberRole}")]
    public void ThenTheDiscordRoleGives(string discordRoleId, CommunityMemberRole role) =>
        Community.RoleMappings.ShouldContain(mapping => mapping.DiscordRoleId == discordRoleId && mapping.Role == role);

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

    /// <summary>Checks the role the last member check gave.</summary>
    /// <param name="role">The expected role.</param>
    [Then("the member's role is {CommunityMemberRole}")]
    public void ThenTheMembersRoleIs(CommunityMemberRole role) => _memberRole.ShouldBe(role);
    #endregion Then Steps

    #region Private Helpers
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
