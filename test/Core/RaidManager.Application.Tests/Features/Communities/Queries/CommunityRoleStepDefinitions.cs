using Moq;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Application.Features.Communities.Queries.GetCommunityRole;
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

/// <summary>Defines business-readable steps for the community role check.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Verifies that the role comes from Discord's current answer and that every check Discord can't answer fails
/// closed (ADR-0022).
/// </remarks>
[Binding]
[Scope(Feature = "Community role check")]
public sealed class CommunityRoleStepDefinitions
{
    #region Fields
    /// <summary>Stores one registered user per name used in the scenario.</summary>
    private readonly Dictionary<string, User> _users = [];

    /// <summary>Stores the community repository double.</summary>
    private readonly Mock<ICommunityRepository> _communities = new();

    /// <summary>Stores the user repository double.</summary>
    private readonly Mock<IUserRepository> _userRepository = new();

    /// <summary>Stores the Discord membership lookup double.</summary>
    private readonly Mock<IDiscordServerMembers> _discord = new();

    /// <summary>Stores the Administrator who linked the community.</summary>
    private readonly User _administrator = User.Register(DiscordUserId.Create("900000000000000001"), "Gihed", null);

    /// <summary>Stores the linked community.</summary>
    private Community? _community;

    /// <summary>Stores the result of the latest check.</summary>
    private Result<CommunityMemberRole>? _result;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRoleStepDefinitions"/> class.</summary>
    public CommunityRoleStepDefinitions()
    {
        // Unknown ids are found nowhere; Given steps make the community and the users known.
        _userRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId id, CancellationToken _) => _users.Values.Append(_administrator).FirstOrDefault(user => user.Id == id));
        _communities
            .Setup(repository => repository.FindByIdAsync(It.IsAny<CommunityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunityId id, CancellationToken _) => _community?.Id == id ? _community : null);
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the linked community, failing the scenario when none was linked.</summary>
    private Community Community => _community ?? throw new InvalidOperationException("No community was linked in this scenario.");

    /// <summary>Gets the captured check result, failing the scenario if no check ran.</summary>
    private Result<CommunityMemberRole> CheckResult => _result ?? throw new InvalidOperationException("No check ran in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Links a community and maps one Discord role to Officer.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    [Given("a linked community whose Discord role {string} gives Officer")]
    public void GivenALinkedCommunityWhoseDiscordRoleGivesOfficer(string discordRoleId)
    {
        _community = Community.Link("123456789012345678", "Citadel Vanguard", WarmaneRealm.Icecrown, _administrator.Id);
        _community.MapDiscordRole(discordRoleId, CommunityMemberRole.Officer);
    }

    /// <summary>Makes Discord report a user in the server with the listed roles.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="discordRoleIds">The user's Discord role snowflakes, comma-separated.</param>
    [Given("Discord says {string} is in the server with the roles {string}")]
    public void GivenDiscordSaysIsInTheServerWithTheRoles(string name, string discordRoleIds) =>
        DiscordAnswers(UserNamed(name), Result.Success(DiscordMembership.Member(discordRoleIds.Split(','))));

    /// <summary>Makes Discord report the Administrator in the server with no roles.</summary>
    [Given("Discord says the Administrator is in the server with no roles")]
    public void GivenDiscordSaysTheAdministratorIsInTheServerWithNoRoles() =>
        DiscordAnswers(_administrator, Result.Success(DiscordMembership.Member([])));

    /// <summary>Makes Discord report that a user isn't in the server.</summary>
    /// <param name="name">The user's name.</param>
    [Given("Discord says {string} is not in the server")]
    public void GivenDiscordSaysIsNotInTheServer(string name) => DiscordAnswers(UserNamed(name), Result.Success(DiscordMembership.NotMember));

    /// <summary>Makes every Discord lookup fail.</summary>
    [Given("Discord can't be reached")]
    public void GivenDiscordCantBeReached() =>
        _discord
            .Setup(discord => discord.FindAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DiscordMembership>(DiscordErrors.Unavailable));
    #endregion Given Steps

    #region When Steps
    /// <summary>Checks a named user's role in the linked community.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the check has run.</returns>
    [When("the role of {string} is checked")]
    public Task WhenTheRoleOfIsChecked(string name) => CheckAsync(Community.Id.Value, UserNamed(name).Id.Value);

    /// <summary>Checks the Administrator's role in the linked community.</summary>
    /// <returns>A task that completes when the check has run.</returns>
    [When("the role of the Administrator is checked")]
    public Task WhenTheRoleOfTheAdministratorIsChecked() => CheckAsync(Community.Id.Value, _administrator.Id.Value);

    /// <summary>Checks a named user's role in a community that doesn't exist.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A task that completes when the check has run.</returns>
    [When("the role of {string} is checked in an unknown community")]
    public Task WhenTheRoleOfIsCheckedInAnUnknownCommunity(string name) => CheckAsync(Guid.NewGuid(), UserNamed(name).Id.Value);

    /// <summary>Checks the role of a user RaidManager doesn't know.</summary>
    /// <returns>A task that completes when the check has run.</returns>
    [When("the role of an unknown user is checked")]
    public Task WhenTheRoleOfAnUnknownUserIsChecked() => CheckAsync(Community.Id.Value, Guid.NewGuid());

    /// <summary>Validates a check with one identifier missing.</summary>
    /// <param name="field">The missing field: <c>community id</c> or <c>user id</c>.</param>
    [When("the check is validated without a {word} id")]
    public void WhenTheCheckIsValidatedWithoutA(string field)
    {
        var query = field == "community"
            ? new GetCommunityRoleQuery(Guid.Empty, Guid.NewGuid())
            : new GetCommunityRoleQuery(Guid.NewGuid(), Guid.Empty);
        _validation = new GetCommunityRoleQueryValidator().Validate(query);
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the role the check gave.</summary>
    /// <param name="role">The expected role.</param>
    [Then("the check gives {CommunityMemberRole}")]
    public void ThenTheCheckGives(CommunityMemberRole role)
    {
        CheckResult.IsSuccess.ShouldBeTrue(CheckResult.IsFailure ? CheckResult.Error.Code : null);
        CheckResult.Value.ShouldBe(role);
    }

    /// <summary>Checks that a named user was refused as not a member.</summary>
    /// <param name="name">The user's name, for the scenario's wording.</param>
    [Then("the check is refused because {string} is not a member")]
    public void ThenTheCheckIsRefusedBecauseIsNotAMember(string name) => ShouldBeRefusedAsNotAMember(name);

    /// <summary>Checks that an unknown user was refused as not a member.</summary>
    [Then("the check is refused because the user is not a member")]
    public void ThenTheCheckIsRefusedBecauseTheUserIsNotAMember() => ShouldBeRefusedAsNotAMember("the unknown user");

    /// <summary>Checks that the check failed because Discord couldn't answer.</summary>
    [Then("the check fails because Discord is unavailable")]
    public void ThenTheCheckFailsBecauseDiscordIsUnavailable()
    {
        CheckResult.IsFailure.ShouldBeTrue();
        CheckResult.Error.ShouldBe(DiscordErrors.Unavailable);
    }

    /// <summary>Checks that the check failed because the community doesn't exist.</summary>
    [Then("the check fails because the community was not found")]
    public void ThenTheCheckFailsBecauseTheCommunityWasNotFound()
    {
        CheckResult.IsFailure.ShouldBeTrue();
        CheckResult.Error.ShouldBe(CommunityErrors.NotFound);
        CheckResult.ResultExceptionType.ShouldBe(ResultExceptionType.NotFound);
    }

    /// <summary>Checks that validation failed on a property.</summary>
    /// <param name="property">The property name.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property)
    {
        var validation = _validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(error => error.PropertyName == property);
    }
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Gets or registers the user with a name, each with their own Discord account.</summary>
    /// <param name="name">The user's name.</param>
    /// <returns>The user.</returns>
    private User UserNamed(string name)
    {
        if (!_users.TryGetValue(name, out var user))
        {
            user = User.Register(DiscordUserId.Create($"80000000000000000{_users.Count}"), name, null);
            _users[name] = user;
        }

        return user;
    }

    /// <summary>Makes Discord give an answer for one user in the community's server.</summary>
    /// <param name="user">The user.</param>
    /// <param name="answer">Discord's answer.</param>
    private void DiscordAnswers(User user, Result<DiscordMembership> answer) =>
        _discord
            .Setup(discord => discord.FindAsync(Community.DiscordGuildId, user.DiscordUserId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

    /// <summary>Runs the check through its handler.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="userId">The user.</param>
    /// <returns>A task that completes when the check has run.</returns>
    private async Task CheckAsync(Guid communityId, Guid userId)
    {
        var handler = new GetCommunityRoleQueryHandler(_communities.Object, _userRepository.Object, _discord.Object);
        _result = await handler.Handle(new GetCommunityRoleQuery(communityId, userId), CancellationToken.None);
    }

    /// <summary>Asserts that the check was refused because the user isn't in the server.</summary>
    /// <param name="who">Who was checked, for the failure message.</param>
    private void ShouldBeRefusedAsNotAMember(string who)
    {
        CheckResult.IsFailure.ShouldBeTrue($"{who} should have been refused.");
        CheckResult.Error.ShouldBe(CommunityErrors.NotAMember);
        CheckResult.ResultExceptionType.ShouldBe(ResultExceptionType.AccessDenied);
    }
    #endregion Private Helpers
}
