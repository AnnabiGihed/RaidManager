using System.Security.Claims;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Tests.Features.Authentication;

/// <summary>Verifies <see cref="SessionClaims"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The matched communities are read from the session, skipping any value that isn't an identifier.
/// </remarks>
public sealed class SessionClaimsTests
{
    #region Tests
    /// <summary>Reads every matched community and skips values that aren't identifiers.</summary>
    [Fact]
    public void MemberCommunitiesAreReadAndBadValuesSkipped()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(RaidManagerClaimTypes.MemberCommunityId, first.ToString()),
            new Claim(RaidManagerClaimTypes.MemberCommunityId, "not-an-id"),
            new Claim(RaidManagerClaimTypes.MemberCommunityId, Guid.Empty.ToString()),
            new Claim(RaidManagerClaimTypes.MemberCommunityId, second.ToString()),
        ]));

        user.MemberCommunityIds().ShouldBe([first, second]);
        new ClaimsPrincipal(new ClaimsIdentity()).MemberCommunityIds().ShouldBeEmpty();
    }

    /// <summary>Reads the avatar URL, or none when the session has no picture.</summary>
    [Fact]
    public void AvatarUrlIsReadWhenPresent()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RaidManagerClaimTypes.AvatarUrl, "https://cdn.discordapp.com/avatars/1/a.png")]));

        user.AvatarUrl().ShouldBe("https://cdn.discordapp.com/avatars/1/a.png");
        new ClaimsPrincipal(new ClaimsIdentity()).AvatarUrl().ShouldBeNull();
    }
    #endregion Tests
}
