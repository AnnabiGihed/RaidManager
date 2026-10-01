namespace RaidManager.ApiService.Features.Communities;

/// <summary>Describes a mapped Discord role.</summary>
/// <param name="DiscordRoleId">The role snowflake.</param>
/// <param name="Name">The role's current name, or <see langword="null"/> when it was deleted in Discord.</param>
/// <param name="Missing">Whether the role no longer exists in Discord.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the roles card show a deleted Discord role as missing.
/// </remarks>
public sealed record MappedDiscordRole(string DiscordRoleId, string? Name, bool Missing);
