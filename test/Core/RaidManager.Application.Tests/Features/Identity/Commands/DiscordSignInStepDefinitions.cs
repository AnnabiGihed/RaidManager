using Moq;
using Pivot.Framework.Domain.Primitives;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;

namespace RaidManager.Application.Tests.Features.Identity.Commands;

/// <summary>Defines business-readable steps for resolving a Discord sign-in to one local user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Verifies registration on first sign-in, reuse of the same user afterwards, profile refresh and input validation.
/// </remarks>
[Binding]
[Scope(Feature = "Discord sign-in")]
public sealed class DiscordSignInStepDefinitions
{
    #region Fields
    /// <summary>Stores the repository double.</summary>
    private readonly Mock<IUserRepository> _users = new();

    /// <summary>Stores the unit-of-work double.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the result the next commit returns.</summary>
    private Result _commitResult = Result.Success();

    /// <summary>Stores the user already registered in the scenario, if any.</summary>
    private User? _registered;

    /// <summary>Stores the result of the latest sign-in.</summary>
    private Result<Guid>? _result;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordSignInStepDefinitions"/> class.</summary>
    public DiscordSignInStepDefinitions()
    {
        _users
            .Setup(users => users.FindByDiscordIdAsync(It.IsAny<DiscordUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscordUserId id, CancellationToken _) => _registered?.DiscordUserId.Value == id.Value ? _registered : null);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _commitResult);
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the captured sign-in result, failing the scenario if no sign-in ran.</summary>
    private Result<Guid> SignInResult => _result ?? throw new InvalidOperationException("No sign-in was sent in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Registers the Discord account before the sign-in under test.</summary>
    /// <param name="discordId">The Discord account identifier.</param>
    /// <param name="displayName">The registered display name.</param>
    [Given("Discord account {string} is registered as {string}")]
    public void GivenDiscordAccountIsRegisteredAs(string discordId, string displayName)
    {
        _registered = User.Register(DiscordUserId.Create(discordId), displayName, null);
        ((IAggregateRoot)_registered).ClearDomainEvents();
    }

    /// <summary>Makes the next commit fail.</summary>
    [Given("the commit will fail")]
    public void GivenTheCommitWillFail() =>
        _commitResult = Result.Failure(new Error("Commit.Failed", "The database rejected the change."));
    #endregion Given Steps

    #region When Steps
    /// <summary>Sends a sign-in for the Discord account.</summary>
    /// <param name="discordId">The Discord account identifier.</param>
    /// <param name="displayName">The current Discord display name.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    [When("Discord account {string} signs in as {string}")]
    public async Task WhenDiscordAccountSignsInAs(string discordId, string displayName) =>
        _result = await new SignInWithDiscordCommandHandler(_users.Object, _unitOfWork.Object)
            .Handle(new SignInWithDiscordCommand(discordId, displayName, null), CancellationToken.None);

    /// <summary>Validates a sign-in command.</summary>
    /// <param name="discordId">The Discord account identifier.</param>
    /// <param name="displayName">The display name.</param>
    [When("a sign-in for Discord account {string} as {string} is validated")]
    public void WhenASignInForDiscordAccountAsIsValidated(string discordId, string displayName) =>
        _validation = new SignInWithDiscordCommandValidator().Validate(new SignInWithDiscordCommand(discordId, displayName, null));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that a new user was created.</summary>
    [Then("the sign-in succeeds with a new user")]
    public void ThenTheSignInSucceedsWithANewUser()
    {
        SignInResult.IsSuccess.ShouldBeTrue();
        SignInResult.Value.ShouldNotBe(Guid.Empty);
    }

    /// <summary>Asserts that the sign-in returned the already registered user.</summary>
    [Then("the sign-in returns the registered user")]
    public void ThenTheSignInReturnsTheRegisteredUser()
    {
        SignInResult.IsSuccess.ShouldBeTrue();
        SignInResult.Value.ShouldBe(_registered.ShouldNotBeNull().Id.Value);
    }

    /// <summary>Asserts that a new user was added and committed once.</summary>
    [Then("the user is added and committed")]
    public void ThenTheUserIsAddedAndCommitted()
    {
        var userId = SignInResult.Value;
        _users.Verify(users => users.AddAsync(It.Is<User>(user => user.Id.Value == userId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts that the registered user was updated and committed once.</summary>
    [Then("the registered user is updated and committed")]
    public void ThenTheRegisteredUserIsUpdatedAndCommitted()
    {
        var registered = _registered.ShouldNotBeNull();
        _users.Verify(users => users.UpdateAsync(registered, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Asserts that nothing was added, updated or committed.</summary>
    [Then("nothing is committed")]
    public void ThenNothingIsCommitted()
    {
        _users.Verify(users => users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(users => users.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Asserts the failure code.</summary>
    /// <param name="code">The expected error code.</param>
    [Then("the sign-in fails with {string}")]
    public void ThenTheSignInFailsWith(string code)
    {
        SignInResult.IsFailure.ShouldBeTrue();
        SignInResult.Error.Code.ShouldBe(code);
    }

    /// <summary>Asserts that validation failed, and only on the named property.</summary>
    /// <param name="property">The expected property name.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property)
    {
        _validation.ShouldNotBeNull().IsValid.ShouldBeFalse();
        _validation.Errors.ShouldAllBe(error => error.PropertyName == property);
    }
    #endregion Then Steps
}
