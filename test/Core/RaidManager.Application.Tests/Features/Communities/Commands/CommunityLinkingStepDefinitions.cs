using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Communities.Commands.LinkCommunity;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.Errors;
using RaidManager.Domain.Features.Communities.Repositories;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Errors;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Communities.Commands;

/// <summary>Defines business-readable steps for linking a Discord server as a community.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Verifies that a server links once, with the user who added the bot as Administrator, and that the command's
/// input is validated before it reaches the handler.
/// </remarks>
[Binding]
[Scope(Feature = "Community linking")]
public sealed class CommunityLinkingStepDefinitions
{
    #region Fields
    /// <summary>Stores one registered user per name used in the scenario.</summary>
    private readonly Dictionary<string, User> _users = [];

    /// <summary>Stores the Discord servers that are already linked.</summary>
    private readonly HashSet<string> _linkedServers = [];

    /// <summary>Stores the community repository double.</summary>
    private readonly Mock<ICommunityRepository> _communities = new();

    /// <summary>Stores the user repository double.</summary>
    private readonly Mock<IUserRepository> _userRepository = new();

    /// <summary>Stores the unit of work double.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the community the handler added, if any.</summary>
    private Community? _added;

    /// <summary>Stores the result of the latest link.</summary>
    private Result<Guid>? _result;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityLinkingStepDefinitions"/> class.</summary>
    public CommunityLinkingStepDefinitions()
    {
        _userRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserId id, CancellationToken _) => _users.Values.FirstOrDefault(user => user.Id == id));
        _communities
            .Setup(repository => repository.IsDiscordGuildLinkedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string guildId, CancellationToken _) => _linkedServers.Contains(guildId));
        _communities
            .Setup(repository => repository.AddAsync(It.IsAny<Community>(), It.IsAny<CancellationToken>()))
            .Callback((Community community, CancellationToken _) => _added = community)
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the captured link result, failing the scenario if no link ran.</summary>
    private Result<Guid> LinkResult => _result ?? throw new InvalidOperationException("No link was attempted in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Registers a user.</summary>
    /// <param name="name">The user's name.</param>
    [Given("{string} is a RaidManager user")]
    public void GivenIsARaidManagerUser(string name) =>
        _users[name] = User.Register(DiscordUserId.Create($"90000000000000000{_users.Count}"), name, null);

    /// <summary>Marks a Discord server as already linked.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    [Given("the Discord server {string} is already linked")]
    public void GivenTheDiscordServerIsAlreadyLinked(string discordGuildId) => _linkedServers.Add(discordGuildId);
    #endregion Given Steps

    #region When Steps
    /// <summary>Links a server as a named user.</summary>
    /// <param name="name">The user's name.</param>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="serverName">The server name.</param>
    /// <param name="realm">The Warmane realm.</param>
    /// <returns>A task that completes when the link has run.</returns>
    [When("{string} links the Discord server {string} named {string} on {WarmaneRealm}")]
    public Task WhenLinksTheDiscordServer(string name, string discordGuildId, string serverName, WarmaneRealm realm) =>
        LinkAsync(new LinkCommunityCommand(discordGuildId, serverName, realm.ToString(), _users[name].Id.Value));

    /// <summary>Links a server as a user RaidManager doesn't know.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="serverName">The server name.</param>
    /// <param name="realm">The Warmane realm.</param>
    /// <returns>A task that completes when the link has run.</returns>
    [When("an unknown user links the Discord server {string} named {string} on {WarmaneRealm}")]
    public Task WhenAnUnknownUserLinksTheDiscordServer(string discordGuildId, string serverName, WarmaneRealm realm) =>
        LinkAsync(new LinkCommunityCommand(discordGuildId, serverName, realm.ToString(), Guid.NewGuid()));

    /// <summary>Validates a link with one field made invalid, or with every field set.</summary>
    /// <param name="variation">The invalid field, as the scenario names it, or <c>every field set</c>.</param>
    [When("^a link is validated with (.*)$")]
    public void WhenALinkIsValidatedWith(string variation)
    {
        var valid = new LinkCommunityCommand("123456789012345678", "Citadel Vanguard", nameof(WarmaneRealm.Icecrown), Guid.NewGuid());
        var command = variation switch
        {
            "a server id of letters" => valid with { DiscordGuildId = "citadel" },
            "a blank name" => valid with { Name = "   " },
            "an unknown realm" => valid with { Realm = "Narnia" },
            "a realm number" => valid with { Realm = "1" },
            "no user" => valid with { AdministratorUserId = Guid.Empty },
            "every field set" => valid,
            _ => throw new ArgumentOutOfRangeException(nameof(variation), variation, "Unknown validation case."),
        };
        _validation = new LinkCommunityCommandValidator().Validate(command);
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks that the community was linked with the user as Administrator.</summary>
    /// <param name="name">The user's name.</param>
    [Then("the community is linked with {string} as its Administrator")]
    public void ThenTheCommunityIsLinkedWithAsItsAdministrator(string name)
    {
        LinkResult.IsSuccess.ShouldBeTrue(LinkResult.IsFailure ? LinkResult.Error.Code : null);
        var community = _added.ShouldNotBeNull();
        LinkResult.Value.ShouldBe(community.Id.Value);
        community.AdministratorId.ShouldBe(_users[name].Id);
        community.Realm.ShouldBe(WarmaneRealm.Icecrown);
        community.Name.ShouldBe("Citadel Vanguard");
    }

    /// <summary>Checks that the link was committed.</summary>
    [Then("the link is saved")]
    public void ThenTheLinkIsSaved() => _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Checks that nothing was added or committed.</summary>
    [Then("nothing is saved")]
    public void ThenNothingIsSaved()
    {
        _added.ShouldBeNull();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Checks that the link was refused because the server is already linked.</summary>
    [Then("the link is refused because the server is already linked")]
    public void ThenTheLinkIsRefusedBecauseTheServerIsAlreadyLinked()
    {
        LinkResult.IsFailure.ShouldBeTrue();
        LinkResult.Error.ShouldBe(CommunityErrors.AlreadyLinked);
        LinkResult.ResultExceptionType.ShouldBe(ResultExceptionType.Conflict);
    }

    /// <summary>Checks that the link was refused because the user doesn't exist.</summary>
    [Then("the link is refused because the user was not found")]
    public void ThenTheLinkIsRefusedBecauseTheUserWasNotFound()
    {
        LinkResult.IsFailure.ShouldBeTrue();
        LinkResult.Error.ShouldBe(IdentityErrors.UserNotFound);
        LinkResult.ResultExceptionType.ShouldBe(ResultExceptionType.NotFound);
    }

    /// <summary>Checks that validation failed on a property.</summary>
    /// <param name="property">The property name.</param>
    [Then("the link validation fails on {string}")]
    public void ThenTheLinkValidationFailsOn(string property)
    {
        var validation = _validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(error => error.PropertyName == property);
    }

    /// <summary>Checks that validation passed.</summary>
    [Then("the link validation passes")]
    public void ThenTheLinkValidationPasses() => _validation.ShouldNotBeNull().IsValid.ShouldBeTrue();
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Runs a link through its handler.</summary>
    /// <param name="command">The command.</param>
    /// <returns>A task that completes when the link has run.</returns>
    private async Task LinkAsync(LinkCommunityCommand command)
    {
        var handler = new LinkCommunityCommandHandler(_communities.Object, _userRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(command, CancellationToken.None);
    }
    #endregion Private Helpers
}
