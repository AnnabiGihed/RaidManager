namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents the game client that captured the snapshot.</summary>
/// <param name="Locale">The client locale, such as <c>enUS</c>.</param>
/// <param name="Build">The client build, such as <c>12340</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Tells which language the snapshot's names are in: raid names are only read from an English client (#384).
/// </remarks>
public sealed record SnapshotClient(string? Locale, string? Build);
