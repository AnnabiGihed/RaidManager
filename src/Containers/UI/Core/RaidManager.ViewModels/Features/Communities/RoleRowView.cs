namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes one row of the roles card as the page shows it.</summary>
/// <param name="Role">The row's key: the role's id, or <c>Administrator</c> or <c>Member</c>.</param>
/// <param name="Label">The role's label, such as "Raid leader".</param>
/// <param name="Source">The line shown instead of chips for Administrator and Member; <see langword="null"/> for mapped roles.</param>
/// <param name="Chips">The mapped Discord roles.</param>
/// <param name="MembersLabel">The member count, such as "3 members".</param>
/// <param name="Editable">Whether the user can add and remove Discord roles on this row, edit and delete the role.</param>
/// <param name="Allows">What the role allows, such as "Allows raids, rosters"; <see langword="null"/> for Administrator and Member.</param>
/// <param name="Locked">Whether the user manages roles but this role is the Administrator's to change.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives the page the wording of each row, so it only lays it out.
/// </remarks>
public sealed record RoleRowView(string Role, string Label, string? Source, IReadOnlyList<RoleChipView> Chips, string MembersLabel, bool Editable, string? Allows = null, bool Locked = false);
