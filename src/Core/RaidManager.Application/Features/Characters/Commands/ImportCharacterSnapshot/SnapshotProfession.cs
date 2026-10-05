namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one profession or secondary skill.</summary>
/// <param name="Name">The localized name, such as <c>Tailoring</c>.</param>
/// <param name="Rank">The current skill.</param>
/// <param name="MaxRank">The maximum skill of the character's training.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors one profession of the addon contract (#384).
/// </remarks>
public sealed record SnapshotProfession(string? Name, int Rank, int MaxRank);
