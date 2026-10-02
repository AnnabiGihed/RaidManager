namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes one row of the members page as the page shows it.</summary>
/// <param name="DiscordUserId">The Discord user snowflake.</param>
/// <param name="DisplayName">The name shown in the server.</param>
/// <param name="AvatarUrl">The Discord picture's address, or <see langword="null"/> for initials.</param>
/// <param name="DiscordRoles">The Discord roles, such as "@Officier, @Veteran", or "No roles".</param>
/// <param name="Roles">The roles the member has, each shown as a badge.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives the page the wording of each member, so it only lays it out.
/// </remarks>
public sealed record MemberRowView(string DiscordUserId, string DisplayName, string? AvatarUrl, string DiscordRoles, IReadOnlyList<string> Roles);
