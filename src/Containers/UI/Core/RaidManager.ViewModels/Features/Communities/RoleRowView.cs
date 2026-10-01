namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes one row of the roles card as the page shows it.</summary>
/// <param name="Role">The RaidManager role's API name.</param>
/// <param name="Label">The role's label, such as "Raid leader".</param>
/// <param name="Source">The line shown instead of chips for Administrator and Member; <see langword="null"/> for mapped roles.</param>
/// <param name="Chips">The mapped Discord roles.</param>
/// <param name="MembersLabel">The member count, such as "3 members".</param>
/// <param name="Editable">Whether the Administrator can add and remove Discord roles on this row.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives the page the wording of each row, so it only lays it out.
/// </remarks>
public sealed record RoleRowView(string Role, string Label, string? Source, IReadOnlyList<RoleChipView> Chips, string MembersLabel, bool Editable);
