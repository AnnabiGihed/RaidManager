namespace RaidManager.ApiService.Features.Communities;

/// <summary>Answers a created role with its identifier.</summary>
/// <param name="RoleId">The new role.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Lets the website map Discord roles to the role it just created.
/// </remarks>
public sealed record CreatedCommunityRole(Guid RoleId);
