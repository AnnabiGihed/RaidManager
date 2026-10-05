namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one talent tree.</summary>
/// <param name="Name">The localized tree name, such as <c>Blood</c>.</param>
/// <param name="PointsSpent">The points spent in the tree.</param>
/// <param name="Ranks">One digit per talent of the tree: its current rank.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one talent tree of the addon contract (#384).
/// </remarks>
public sealed record SnapshotTalentTab(string? Name, int PointsSpent, string? Ranks);
