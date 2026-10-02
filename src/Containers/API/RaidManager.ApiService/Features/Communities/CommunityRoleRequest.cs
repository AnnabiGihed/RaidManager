namespace RaidManager.ApiService.Features.Communities;

/// <summary>Asks the API to create or change one of a community's roles.</summary>
/// <param name="Name">The role name, at most 50 characters and unique in the community.</param>
/// <param name="Permissions">What the role allows: <c>ManageRaids</c>, <c>BuildRosters</c>, <c>RunRaidNight</c>, <c>ReviewConflicts</c>, <c>ManageCommunityRoles</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The role form of boards 10 and 11 of the community settings mockup (story #308).
/// </remarks>
public sealed record CommunityRoleRequest(string Name, IReadOnlyList<string> Permissions);
