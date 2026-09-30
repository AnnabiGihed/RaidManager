using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Events;
using RaidManager.Domain.Features.Identity.ValueObjects;

namespace RaidManager.Domain.Tests.Features.Identity.Aggregates;

/// <summary>Defines business-readable steps for refreshing a user's Discord profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Verifies that only a real profile change updates the user and raises an event, so routine sign-ins write nothing.
/// </remarks>
[Binding]
[Scope(Feature = "Discord profile")]
public sealed class DiscordProfileStepDefinitions
{
    #region Fields
    /// <summary>Stores the user under test.</summary>
    private User _user = null!;

    /// <summary>Stores whether the latest refresh changed the profile.</summary>
    private bool _changed;
    #endregion Fields

    #region Given Steps
    /// <summary>Registers a user and clears the registration event.</summary>
    /// <param name="displayName">The registered display name.</param>
    /// <param name="avatarUrl">The registered avatar URL.</param>
    [Given("a user registered as {string} with avatar {string}")]
    public void GivenAUserRegisteredAsWithAvatar(string displayName, string avatarUrl)
    {
        _user = User.Register(DiscordUserId.Create("80351110224678912"), displayName, avatarUrl);
        ((Pivot.Framework.Domain.Primitives.IAggregateRoot)_user).ClearDomainEvents();
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Refreshes the profile with the given values.</summary>
    /// <param name="displayName">The latest display name.</param>
    /// <param name="avatarUrl">The latest avatar URL.</param>
    [When("the Discord profile is refreshed as {string} with avatar {string}")]
    public void WhenTheDiscordProfileIsRefreshedAsWithAvatar(string displayName, string avatarUrl) =>
        _changed = _user.UpdateDiscordProfile(displayName, avatarUrl);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the refresh reported a change.</summary>
    [Then("the profile changed")]
    public void ThenTheProfileChanged() => _changed.ShouldBeTrue();

    /// <summary>Asserts that the refresh reported no change.</summary>
    [Then("the profile did not change")]
    public void ThenTheProfileDidNotChange() => _changed.ShouldBeFalse();

    /// <summary>Asserts the current display name.</summary>
    /// <param name="displayName">The expected display name.</param>
    [Then("the display name is {string}")]
    public void ThenTheDisplayNameIs(string displayName) => _user.DisplayName.ShouldBe(displayName);

    /// <summary>Asserts that a profile updated event was raised.</summary>
    [Then("a Discord profile updated event was raised")]
    public void ThenADiscordProfileUpdatedEventWasRaised() =>
        _user.GetDomainEvents().OfType<DiscordProfileUpdated>().ShouldHaveSingleItem();

    /// <summary>Asserts that no profile updated event was raised.</summary>
    [Then("no Discord profile updated event was raised")]
    public void ThenNoDiscordProfileUpdatedEventWasRaised() => _user.GetDomainEvents().ShouldBeEmpty();
    #endregion Then Steps
}
