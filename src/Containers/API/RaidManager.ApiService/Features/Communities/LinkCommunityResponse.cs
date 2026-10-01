namespace RaidManager.ApiService.Features.Communities;

/// <summary>Returns the community a Discord server was linked as.</summary>
/// <param name="CommunityId">The new community's identifier.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the website open the new community's settings page.
/// </remarks>
public sealed record LinkCommunityResponse(Guid CommunityId);
