namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one glyph socket.</summary>
/// <param name="Socket">The socket.</param>
/// <param name="Type"><c>1</c> major, <c>2</c> minor.</param>
/// <param name="Enabled">Whether the socket is unlocked.</param>
/// <param name="Empty">Whether an unlocked socket has no glyph.</param>
/// <param name="SpellId">The glyph's spell id.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one glyph socket of the addon contract (#384).
/// </remarks>
public sealed record SnapshotGlyph(int Socket, int Type, bool? Enabled, bool? Empty, int? SpellId);
