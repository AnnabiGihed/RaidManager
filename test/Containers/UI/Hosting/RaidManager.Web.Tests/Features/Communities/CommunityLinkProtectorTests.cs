using Microsoft.AspNetCore.DataProtection;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Communities;

namespace RaidManager.Web.Tests.Features.Communities;

/// <summary>Verifies what the protector accepts back from the browser.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Only the same user's untampered state and pending link come back; anything else is treated as absent.
/// </remarks>
public sealed class CommunityLinkProtectorTests
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the protector under test.</summary>
    private readonly CommunityLinkProtector _protector = new(new EphemeralDataProtectionProvider());
    #endregion Fields

    #region Tests
    /// <summary>Accepts the state it created for the same user only.</summary>
    [Fact]
    public void StateIsExpectedOnlyForTheSameUserAndValue()
    {
        var (state, cookie) = _protector.CreateState(UserId);

        _protector.IsExpectedState(cookie, state, UserId).ShouldBeTrue();
        _protector.IsExpectedState(cookie, state + "x", UserId).ShouldBeFalse();
        _protector.IsExpectedState(cookie, state, Guid.NewGuid()).ShouldBeFalse();
        _protector.IsExpectedState("tampered", state, UserId).ShouldBeFalse();
        _protector.IsExpectedState(null, state, UserId).ShouldBeFalse();
        _protector.IsExpectedState(cookie, null, UserId).ShouldBeFalse();
    }

    /// <summary>Creates a different state each time.</summary>
    [Fact]
    public void EachStateIsNew() => _protector.CreateState(UserId).State.ShouldNotBe(_protector.CreateState(UserId).State);

    /// <summary>Reads a pending link back for the same user only.</summary>
    [Fact]
    public void PendingLinkComesBackOnlyForItsUser()
    {
        var link = new PendingCommunityLink("987654321098765432", "Dark Templars", UserId);
        var protectedLink = _protector.Protect(link);

        _protector.Unprotect(protectedLink, UserId).ShouldBe(link);
        _protector.Unprotect(protectedLink, Guid.NewGuid()).ShouldBeNull();
        _protector.Unprotect("tampered", UserId).ShouldBeNull();
        _protector.Unprotect(null, UserId).ShouldBeNull();
    }
    #endregion Tests
}
