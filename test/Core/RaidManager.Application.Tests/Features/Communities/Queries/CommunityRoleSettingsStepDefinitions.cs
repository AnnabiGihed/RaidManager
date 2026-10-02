using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Application.Features.Communities.Commands.CreateCommunityRole;
using RaidManager.Application.Features.Communities.Commands.DeleteCommunityRole;
using RaidManager.Application.Features.Communities.Commands.MapCommunityRole;
using RaidManager.Application.Features.Communities.Commands.RefreshCommunityName;
using RaidManager.Application.Features.Communities.Commands.UnmapCommunityRole;
using RaidManager.Application.Features.Communities.Commands.UpdateCommunityRole;
using RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;
using RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Enums;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Communities.Queries;

/// <summary>Defines business-readable steps for reading and changing a community's officer roles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Verifies the roles card's counts and mappings from Discord's answers, that only the Administrator changes
/// mappings, and that the stored server name follows Discord.
/// </remarks>
[Binding]
[Scope(Feature = "Community role settings")]
public sealed class CommunityRoleSettingsStepDefinitions
{
    #region Fields
    /// <summary>Stores one registered user per name.</summary>
    private readonly Dictionary<string, User> _users = [];

    /// <summary>Stores the Discord role ids by name; the server's mappable roles are those in <see cref="_serverRoles"/>.</summary>
    private readonly Dictionary<string, string> _roleIds = [];

    /// <summary>Stores the server's mappable roles.</summary>
    private readonly List<DiscordServerRole> _serverRoles = [];

    /// <summary>Stores the people in the server.</summary>
    private readonly List<DiscordServerMember> _members = [];

    /// <summary>Stores the community repository double.</summary>
    private readonly Mock<ICommunityRepository> _communities = new();

    /// <summary>Stores the user repository double.</summary>
    private readonly Mock<IUserRepository> _userRepository = new();

    /// <summary>Stores the Discord membership lookup double.</summary>
    private readonly Mock<IDiscordServerMembers> _discordMembers = new();

    /// <summary>Stores the Discord server reader double.</summary>
    private readonly Mock<IDiscordServers> _discordServers = new();

    /// <summary>Stores the unit of work double.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the server's name.</summary>
    private string _serverName = string.Empty;

    /// <summary>Stores a value indicating whether Discord fails every call.</summary>
    private bool _discordDown;

    /// <summary>Stores a value indicating whether Discord fails to read the server.</summary>
    private bool _serverDown;

    /// <summary>Stores a value indicating whether Discord fails to list the server's people.</summary>
    private bool _memberListDown;

    /// <summary>Stores the community.</summary>
    private Community? _community;

    /// <summary>Stores the result of the latest read.</summary>
    private Result<CommunityRoleSettingsResponse>? _read;

    /// <summary>Stores the result of the latest change.</summary>
    private Result? _change;

    /// <summary>Stores the result of the latest role validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;

    /// <summary>Stores the result of the latest members list.</summary>
    private Result<CommunityMembersResponse>? _memberList;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRoleSettingsStepDefinitions"/> class.</summary>
    public CommunityRoleSettingsStepDefinitions()
    {
        _userRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId id, CancellationToken _) => _users.Values.FirstOrDefault(user => user.Id == id));
        _communities
            .Setup(repository => repository.FindByIdAsync(It.IsAny<CommunityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunityId id, CancellationToken _) => _community?.Id == id ? _community : null);
        _discordMembers
            .Setup(discord => discord.FindAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string userId, CancellationToken _) => _discordDown
                ? Result.Failure<DiscordMembership>(DiscordErrors.Unavailable)
                : Result.Success(_members.Find(member => member.UserId == userId) is { } member
                    ? DiscordMembership.Member(member.RoleIds)
                    : DiscordMembership.NotMember));
        _discordServers
            .Setup(discord => discord.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _discordDown || _serverDown
                ? Result.Failure<DiscordServer>(DiscordErrors.Unavailable)
                : Result.Success(new DiscordServer(_serverName, [.. _serverRoles])));
        _discordServers
            .Setup(discord => discord.ListMembersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _memberListDown
                ? Result.Failure<IReadOnlyList<DiscordServerMember>>(DiscordErrors.Unavailable)
                : Result.Success<IReadOnlyList<DiscordServerMember>>([.. _members]));
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the community, failing the scenario when none was linked.</summary>
    private Community Community => _community ?? throw new InvalidOperationException("No community was linked in this scenario.");

    /// <summary>Gets the latest read, failing the scenario if none ran.</summary>
    private Result<CommunityRoleSettingsResponse> Read => _read ?? throw new InvalidOperationException("No read ran in this scenario.");

    /// <summary>Gets the latest request's outcome, read or change.</summary>
    private Result Outcome => (Result?)_read ?? (Result?)_memberList ?? _change ?? throw new InvalidOperationException("No request ran in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Links a community administered by a user.</summary>
    /// <param name="name">The Administrator's name.</param>
    [Given("a linked community administered by {string}")]
    public void GivenALinkedCommunityAdministeredBy(string name) =>
        _community = Community.Link("123456789012345678", "Dark Templars", WarmaneRealm.Icecrown, UserNamed(name).Id);

    /// <summary>Names the server and gives it mappable roles, highest first.</summary>
    /// <param name="name">The server name.</param>
    /// <param name="roles">The role names, comma-separated.</param>
    [Given("the Discord server is named {string} with the roles {string}")]
    public void GivenTheDiscordServerIsNamedWithTheRoles(string name, string roles)
    {
        _serverName = name;
        var names = roles.Split(',');
        _serverRoles.AddRange(names.Select((role, index) => new DiscordServerRole(RoleId(role), role, names.Length - index)));
    }

    /// <summary>Maps a Discord role before the scenario's action.</summary>
    /// <param name="role">The Discord role name.</param>
    /// <param name="raidManagerRole">The preset role, such as <c>Officer</c> or <c>RaidLeader</c>.</param>
    [Given("the Discord role {string} gives {word}")]
    public void GivenTheDiscordRoleGives(string role, string raidManagerRole) => Community.MapDiscordRole(RoleId(role), Preset(raidManagerRole));

    /// <summary>Puts people in the server with their Discord roles.</summary>
    /// <param name="people">Table with the columns <c>name</c> and <c>roles</c> (comma-separated, may be empty).</param>
    [Given("these people are in the Discord server")]
    public void GivenThesePeopleAreInTheDiscordServer(DataTable people)
    {
        foreach (var row in people.Rows)
        {
            var roles = row["roles"].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(RoleId).ToList();
            _members.Add(new DiscordServerMember(UserNamed(row["name"]).DiscordUserId.Value, row["name"], roles));
        }
    }

    /// <summary>Maps a role that Discord no longer has.</summary>
    /// <param name="role">The Discord role name.</param>
    /// <param name="raidManagerRole">The preset role, such as <c>Officer</c> or <c>RaidLeader</c>.</param>
    [Given("the Discord role {string} was mapped to {word} and deleted in Discord")]
    public void GivenTheDiscordRoleWasMappedAndDeleted(string role, string raidManagerRole) =>
        Community.MapDiscordRole(RoleId(role), Preset(raidManagerRole));

    /// <summary>Registers a user who isn't in the server.</summary>
    /// <param name="name">The user's name.</param>
    [Given("{string} is not in the Discord server")]
    public void GivenIsNotInTheDiscordServer(string name) => UserNamed(name);

    /// <summary>Makes every Discord call fail.</summary>
    [Given("Discord can't be reached")]
    public void GivenDiscordCantBeReached() => _discordDown = true;

    /// <summary>Makes Discord fail to read the server, after it confirmed the membership.</summary>
    [Given("Discord can't read the server")]
    public void GivenDiscordCantReadTheServer() => _serverDown = true;

    /// <summary>Makes Discord fail to list the server's people, after it read the server.</summary>
    [Given("Discord can't list the server's people")]
    public void GivenDiscordCantListTheServersPeople() => _memberListDown = true;

    /// <summary>Creates a role as the Administrator and maps a Discord role to it.</summary>
    /// <param name="discordRole">The Discord role name.</param>
    /// <param name="name">The new role's name.</param>
    /// <param name="permissions">What it allows, by name.</param>
    [Given("the Discord role {string} gives a new role {string} allowing {string}")]
    public void GivenTheDiscordRoleGivesANewRole(string discordRole, string name, string permissions)
    {
        var role = Community.CreateRole(name, CommunityPermissionNames.Parse(PermissionList(permissions)), byAdministrator: true).Value;
        Community.MapDiscordRole(RoleId(discordRole), role.Id);
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Reads the community's roles as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the read has run.</returns>
    [When("{string} reads the community's roles")]
    public async Task WhenReadsTheCommunitysRoles(string name)
    {
        var handler = new GetCommunityRoleSettingsQueryHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object);
        _read = await handler.Handle(new GetCommunityRoleSettingsQuery(Community.Id.Value, UserNamed(name).Id.Value), CancellationToken.None);
    }

    /// <summary>Lists the community's members as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the list has run.</returns>
    [When("{string} lists the community's members")]
    public async Task WhenListsTheCommunitysMembers(string name)
    {
        var handler = new GetCommunityMembersQueryHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object, TimeProvider.System);
        _memberList = await handler.Handle(new GetCommunityMembersQuery(Community.Id.Value, UserNamed(name).Id.Value), CancellationToken.None);
    }

    /// <summary>Lists the members of a community that doesn't exist.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the list has run.</returns>
    [When("{string} lists the members of an unknown community")]
    public async Task WhenListsTheMembersOfAnUnknownCommunity(string name)
    {
        var handler = new GetCommunityMembersQueryHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object, TimeProvider.System);
        _memberList = await handler.Handle(new GetCommunityMembersQuery(Guid.NewGuid(), UserNamed(name).Id.Value), CancellationToken.None);
    }

    /// <summary>Maps a Discord role as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="role">The Discord role name.</param>
    /// <param name="raidManagerRole">The preset role, such as <c>Officer</c> or <c>RaidLeader</c>.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} maps the Discord role {string} to {word}")]
    public async Task WhenMapsTheDiscordRole(string name, string role, string raidManagerRole)
    {
        var handler = new MapCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new MapCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleId(role), Preset(raidManagerRole).Value), CancellationToken.None);
    }

    /// <summary>Maps a Discord role to a role id the community doesn't have, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="role">The Discord role name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} maps the Discord role {string} to a role the community doesn't have")]
    public async Task WhenMapsTheDiscordRoleToAnUnknownRole(string name, string role)
    {
        var handler = new MapCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new MapCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleId(role), Guid.NewGuid()), CancellationToken.None);
    }

    /// <summary>Stops a Discord role giving a RaidManager role, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="raidManagerRole">The preset role, such as <c>Officer</c> or <c>RaidLeader</c>.</param>
    /// <param name="role">The Discord role name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} removes the {word} mapping of the Discord role {string}")]
    public async Task WhenRemovesTheMapping(string name, string raidManagerRole, string role)
    {
        var handler = new UnmapCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _unitOfWork.Object);
        _change = await handler.Handle(
            new UnmapCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleId(role), Preset(raidManagerRole).Value),
            CancellationToken.None);
    }

    /// <summary>Refreshes the community's name from Discord's answer.</summary>
    /// <param name="name">The server's name as Discord reports it.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("the community's name is refreshed to {string}")]
    public async Task WhenTheCommunitysNameIsRefreshedTo(string name)
    {
        var handler = new RefreshCommunityNameCommandHandler(_communities.Object, _unitOfWork.Object);
        _change = await handler.Handle(new RefreshCommunityNameCommand(Community.Id.Value, name), CancellationToken.None);
    }

    /// <summary>Creates a role, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="role">The role name.</param>
    /// <param name="permissions">What it allows, by name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} creates the role {string} allowing {string}")]
    public async Task WhenCreatesTheRole(string name, string role, string permissions)
    {
        var handler = new CreateCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new CreateCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, role, PermissionList(permissions)), CancellationToken.None);
    }

    /// <summary>Changes a role, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="role">The role's current name.</param>
    /// <param name="newName">The role's new name.</param>
    /// <param name="permissions">What it allows now, by name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} changes the role {string} to {string} allowing {string}")]
    public async Task WhenChangesTheRole(string name, string role, string newName, string permissions)
    {
        var handler = new UpdateCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _unitOfWork.Object);
        _change = await handler.Handle(
            new UpdateCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleNamed(role), newName, PermissionList(permissions)),
            CancellationToken.None);
    }

    /// <summary>Changes a role of a community that doesn't exist.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} changes a role of a community that doesn't exist")]
    public async Task WhenChangesARoleOfACommunityThatDoesntExist(string name)
    {
        var handler = new UpdateCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new UpdateCommunityRoleCommand(Guid.NewGuid(), UserNamed(name).Id.Value, Guid.NewGuid(), "Veteran", []), CancellationToken.None);
    }

    /// <summary>Deletes a role, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="role">The role name; an unknown name stands for a role the community doesn't have.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} deletes the role {string}")]
    public async Task WhenDeletesTheRole(string name, string role)
    {
        var handler = new DeleteCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new DeleteCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleNamed(role)), CancellationToken.None);
    }

    /// <summary>Maps a Discord role to a role by its name, as a user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="discordRole">The Discord role name.</param>
    /// <param name="role">The role name.</param>
    /// <returns>A task that completes when the change has run.</returns>
    [When("{string} maps the Discord role {string} to the role {string}")]
    public async Task WhenMapsTheDiscordRoleToTheRole(string name, string discordRole, string role)
    {
        var handler = new MapCommunityRoleCommandHandler(_communities.Object, _userRepository.Object, _discordMembers.Object, _discordServers.Object, _unitOfWork.Object);
        _change = await handler.Handle(new MapCommunityRoleCommand(Community.Id.Value, UserNamed(name).Id.Value, RoleId(discordRole), RoleNamed(role)), CancellationToken.None);
    }

    /// <summary>Validates a role's name and permissions as a create command would carry them.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The permission names, comma-separated.</param>
    [When("a role named {string} allowing {string} is validated")]
    public void WhenARoleNamedIsValidated(string name, string permissions) =>
        _validation = new CreateCommunityRoleCommandValidator().Validate(new CreateCommunityRoleCommand(Guid.NewGuid(), Guid.NewGuid(), name, PermissionList(permissions)));
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the members, in order, with their Discord roles and RaidManager role.</summary>
    /// <param name="rows">Table with the columns <c>name</c>, <c>discord roles</c> and <c>role</c>.</param>
    [Then("the members are listed as")]
    public void ThenTheMembersAreListedAs(DataTable rows)
    {
        var members = (_memberList ?? throw new InvalidOperationException("No list ran in this scenario.")).Value.Members;
        members.Select(member => member.DisplayName).ShouldBe(rows.Rows.Select(row => row["name"]));
        foreach (var (actual, expected) in members.Zip(rows.Rows))
        {
            string.Join(",", actual.DiscordRoles.Select(role => role.Name)).ShouldBe(expected["discord roles"]);
            string.Join(", ", actual.Roles).ShouldBe(expected["roles"]);
        }
    }

    /// <summary>Checks that the list says when Discord was asked.</summary>
    [Then("the list says when Discord was asked")]
    public void ThenTheListSaysWhenDiscordWasAsked() =>
        (DateTimeOffset.UtcNow - _memberList!.Value.CheckedAtUtc).ShouldBeLessThan(TimeSpan.FromMinutes(1));

    /// <summary>Checks that the asking user may edit.</summary>
    [Then("the roles can be edited")]
    public void ThenTheRolesCanBeEdited() => Read.Value.CanEdit.ShouldBeTrue();

    /// <summary>Checks that the asking user may not edit.</summary>
    [Then("the roles can't be edited")]
    public void ThenTheRolesCantBeEdited() => Read.Value.CanEdit.ShouldBeFalse();

    /// <summary>Checks the mappable roles, in order.</summary>
    /// <param name="roles">The role names, comma-separated.</param>
    [Then("the mappable roles are {string}")]
    public void ThenTheMappableRolesAre(string roles) => Read.Value.MappableRoles.Select(role => role.Name).ShouldBe(roles.Split(','));

    /// <summary>Checks each row's Discord roles and member count.</summary>
    /// <param name="rows">Table with the columns <c>role</c>, <c>discord roles</c> and <c>members</c>.</param>
    [Then("these roles have these members")]
    public void ThenTheseRolesHaveTheseMembers(DataTable rows)
    {
        Read.Value.ServerName.ShouldBe("Dark Templars");
        Read.Value.Rows.Select(row => row.Name).ShouldBe(rows.Rows.Select(row => row["role"]));
        foreach (var (actual, expected) in Read.Value.Rows.Zip(rows.Rows))
        {
            string.Join(",", actual.DiscordRoles.Select(role => role.Name)).ShouldBe(expected["discord roles"]);
            actual.Members.ShouldBe(int.Parse(expected["members"], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Checks that a row shows a missing Discord role.</summary>
    /// <param name="role">The RaidManager role.</param>
    [Then("the {word} role shows a missing Discord role")]
    public void ThenTheRoleShowsAMissingDiscordRole(string role)
    {
        var missing = Read.Value.Rows.Single(row => row.RoleId == Preset(role).Value).DiscordRoles.ShouldHaveSingleItem();
        missing.Missing.ShouldBeTrue();
        missing.Name.ShouldBeNull();
    }

    /// <summary>Checks that the request failed because the community has no such role.</summary>
    [Then("the request fails because the role doesn't exist")]
    public void ThenTheRequestFailsBecauseTheRoleDoesntExist() => ShouldFail(CommunityErrors.RoleNotFound, ResultExceptionType.NotFound);

    /// <summary>Checks that the request was refused because the user isn't in the server.</summary>
    [Then("the request is refused because the user is not a member")]
    public void ThenTheRequestIsRefusedBecauseTheUserIsNotAMember() => ShouldFail(CommunityErrors.NotAMember, ResultExceptionType.AccessDenied);

    /// <summary>Checks that the request was refused because the user can't manage roles.</summary>
    [Then("the request is refused because the user can't manage roles")]
    public void ThenTheRequestIsRefusedBecauseTheUserCantManageRoles() => ShouldFail(CommunityErrors.NotRoleManager, ResultExceptionType.AccessDenied);

    /// <summary>Checks that the request was refused because only the Administrator changes the role.</summary>
    [Then("the request is refused because the role is locked")]
    public void ThenTheRequestIsRefusedBecauseTheRoleIsLocked() => ShouldFail(CommunityErrors.RoleLocked, ResultExceptionType.AccessDenied);

    /// <summary>Checks that the request was refused because only the Administrator lets a role manage roles.</summary>
    [Then("the request is refused because only the Administrator can let a role manage roles")]
    public void ThenTheRequestIsRefusedBecauseOnlyTheAdministratorCanLetARoleManageRoles() =>
        ShouldFail(CommunityErrors.CannotGrantRoleManagement, ResultExceptionType.AccessDenied);

    /// <summary>Checks that the request failed because another role has the name.</summary>
    [Then("the request fails because the role name is taken")]
    public void ThenTheRequestFailsBecauseTheRoleNameIsTaken() => ShouldFail(CommunityErrors.RoleNameTaken, ResultExceptionType.Conflict);

    /// <summary>Checks one of the community's roles and what it allows.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The expected permissions, by name.</param>
    [Then("the community has the role {string} allowing {string}")]
    public void ThenTheCommunityHasTheRole(string name, string permissions) =>
        Community.Roles.Single(role => role.Name == name).Permissions.ShouldBe(CommunityPermissionNames.Parse(PermissionList(permissions)));

    /// <summary>Checks that the community has no role with a name.</summary>
    /// <param name="name">The role name.</param>
    [Then("the community has no role {string}")]
    public void ThenTheCommunityHasNoRole(string name) => Community.Roles.ShouldNotContain(role => role.Name == name);

    /// <summary>Checks that the asking user can't let a role manage roles.</summary>
    [Then("the roles can't let a role manage roles")]
    public void ThenTheRolesCantLetARoleManageRoles() => Read.Value.CanGrantRoleManagement.ShouldBeFalse();

    /// <summary>Checks that the asking user may change a role.</summary>
    /// <param name="name">The role name.</param>
    [Then("the {string} role can be changed")]
    public void ThenTheRoleCanBeChanged(string name) => Read.Value.Rows.Single(row => row.Name == name).CanChange.ShouldBeTrue();

    /// <summary>Checks that the asking user may not change a role.</summary>
    /// <param name="name">The role name.</param>
    [Then("the {string} role can't be changed")]
    public void ThenTheRoleCantBeChanged(string name) => Read.Value.Rows.Single(row => row.Name == name).CanChange.ShouldBeFalse();

    /// <summary>Checks the property a malformed role was rejected on.</summary>
    /// <param name="property">The property name.</param>
    [Then("the role is rejected on {string}")]
    public void ThenTheRoleIsRejectedOn(string property)
    {
        var validation = _validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(error => error.PropertyName == property);
    }

    /// <summary>Checks that the request was refused because the role can't be mapped.</summary>
    [Then("the request is refused because the role can't be mapped")]
    public void ThenTheRequestIsRefusedBecauseTheRoleCantBeMapped()
    {
        Outcome.IsFailure.ShouldBeTrue();
        Outcome.Error.ShouldBe(CommunityErrors.RoleNotMappable);
    }

    /// <summary>Checks that the request failed because the community doesn't exist.</summary>
    [Then("the request fails because the community doesn't exist")]
    public void ThenTheRequestFailsBecauseTheCommunityDoesntExist() => ShouldFail(CommunityErrors.NotFound, ResultExceptionType.NotFound);

    /// <summary>Checks that the request failed because Discord couldn't answer.</summary>
    [Then("the request fails because Discord is unavailable")]
    public void ThenTheRequestFailsBecauseDiscordIsUnavailable()
    {
        Outcome.IsFailure.ShouldBeTrue();
        Outcome.Error.ShouldBe(DiscordErrors.Unavailable);
    }

    /// <summary>Checks that the change was committed.</summary>
    [Then("the change is saved")]
    public void ThenTheChangeIsSaved()
    {
        Outcome.IsSuccess.ShouldBeTrue(Outcome.IsFailure ? Outcome.Error.Code : null);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Checks that nothing was committed.</summary>
    [Then("nothing is saved")]
    public void ThenNothingIsSaved() => _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    /// <summary>Checks the RaidManager role a Discord role gives.</summary>
    /// <param name="role">The Discord role name.</param>
    /// <param name="raidManagerRole">The expected RaidManager role.</param>
    [Then(@"^the Discord role ""(.*)"" gives (Officer|RaidLeader)$")]
    public void ThenTheDiscordRoleGives(string role, string raidManagerRole) =>
        Community.RolesFor([RoleId(role)]).Select(given => given.Id).ShouldBe([Preset(raidManagerRole)]);

    /// <summary>Checks that a Discord role gives two RaidManager roles.</summary>
    /// <param name="role">The Discord role name.</param>
    /// <param name="first">The first RaidManager role.</param>
    /// <param name="second">The second RaidManager role.</param>
    [Then("the Discord role {string} gives {word} and {word}")]
    public void ThenTheDiscordRoleGivesBoth(string role, string first, string second) =>
        Community.RolesFor([RoleId(role)]).Select(given => given.Id).ShouldBe([Preset(first), Preset(second)], ignoreOrder: true);

    /// <summary>Checks that a Discord role gives no RaidManager role.</summary>
    /// <param name="role">The Discord role name.</param>
    [Then("the Discord role {string} gives nothing")]
    public void ThenTheDiscordRoleGivesNothing(string role) => Community.RoleMappings.ShouldNotContain(mapping => mapping.DiscordRoleId == RoleId(role));

    /// <summary>Checks the community's name.</summary>
    /// <param name="name">The expected name.</param>
    [Then("the community is named {string}")]
    public void ThenTheCommunityIsNamed(string name) => Community.Name.ShouldBe(name);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Splits a comma-separated list of permission names.</summary>
    /// <param name="permissions">The names.</param>
    /// <returns>The names, trimmed.</returns>
    private static List<string> PermissionList(string permissions) =>
        [.. permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    /// <summary>Gets or registers the user with a name, each with their own Discord account.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>The user.</returns>
    private User UserNamed(string name)
    {
        if (!_users.TryGetValue(name, out var user))
        {
            user = User.Register(DiscordUserId.Create($"70000000000000000{_users.Count}"), name, null);
            _users[name] = user;
        }

        return user;
    }

    /// <summary>Finds one of the community's preset roles by its name without spaces, as the scenarios write it.</summary>
    /// <param name="name">The name, such as <c>Officer</c> or <c>RaidLeader</c>.</param>
    /// <returns>The role's identifier.</returns>
    private CommunityRoleId Preset(string name) => Community.Roles.Single(role => string.Equals(role.Name.Replace(" ", string.Empty, StringComparison.Ordinal), name, StringComparison.OrdinalIgnoreCase)).Id;

    /// <summary>Finds one of the community's roles by name, or makes up an id for a name it doesn't have.</summary>
    /// <param name="name">The role name.</param>
    /// <returns>The role's identifier.</returns>
    private Guid RoleNamed(string name) => Community.Roles.FirstOrDefault(role => role.Name == name)?.Id.Value ?? Guid.NewGuid();

    /// <summary>Gets or assigns the snowflake of a Discord role name.</summary>
    /// <param name="name">The role name.</param>
    /// <returns>The role snowflake.</returns>
    private string RoleId(string name)
    {
        if (!_roleIds.TryGetValue(name, out var id))
        {
            id = $"50000000000000000{_roleIds.Count}";
            _roleIds[name] = id;
        }

        return id;
    }

    /// <summary>Asserts that the latest request failed with an error and an exception type.</summary>
    /// <param name="error">The expected error.</param>
    /// <param name="type">The expected exception type.</param>
    private void ShouldFail(Error error, ResultExceptionType type)
    {
        Outcome.IsFailure.ShouldBeTrue();
        Outcome.Error.ShouldBe(error);
        Outcome.ResultExceptionType.ShouldBe(type);
    }
    #endregion Private Helpers
}
