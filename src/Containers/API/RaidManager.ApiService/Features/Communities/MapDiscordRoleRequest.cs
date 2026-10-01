namespace RaidManager.ApiService.Features.Communities;

/// <summary>Asks the API to map a Discord role to a RaidManager role.</summary>
/// <param name="Role">The RaidManager role's name: <c>Officer</c> or <c>RaidLeader</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The body of the role mapping request; the community, the user and the Discord role are in the route.
/// </remarks>
public sealed record MapDiscordRoleRequest(string Role);
