using System.Security.Claims;

namespace RaidManager.Web.Features.Authentication;

/// <summary>Reads what the session cookie keeps about the signed-in player.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Gives pages the communities the player's Discord servers matched at sign-in, kept until the next sign-in
/// (ADR-0023).
/// </remarks>
public static class SessionClaims
{
    #region Public Methods
    /// <summary>Lists the communities the player's Discord servers matched at sign-in.</summary>
    /// <param name="user">The signed-in player.</param>
    /// <returns>The communities' identifiers; empty when none matched or the session predates the lookup.</returns>
    public static IReadOnlyList<Guid> MemberCommunityIds(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return
        [
            .. user.FindAll(RaidManagerClaimTypes.MemberCommunityId)
                .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty),
        ];
    }
    #endregion Public Methods
}
