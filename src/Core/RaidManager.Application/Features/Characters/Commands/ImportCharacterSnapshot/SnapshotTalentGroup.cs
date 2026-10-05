namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one talent group.</summary>
/// <param name="Group">The talent group, 1 or 2.</param>
/// <param name="Tabs">The three talent trees, in the game's order.</param>
/// <param name="Glyphs">The glyph sockets.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one talent group of the addon contract (#384).
/// </remarks>
public sealed record SnapshotTalentGroup(int Group, IReadOnlyList<SnapshotTalentTab>? Tabs, IReadOnlyList<SnapshotGlyph>? Glyphs);
