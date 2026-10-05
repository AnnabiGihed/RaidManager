namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Describes what an uploaded snapshot changed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets the companion drop a snapshot from its queue either way, and tell a new import from a repeated upload (#384).
/// </remarks>
public enum SnapshotImportOutcome
{
    /// <summary>The snapshot was newer than what RaidManager had, and its facts were applied.</summary>
    Imported = 1,

    /// <summary>A snapshot captured at the same instant or later was already applied; nothing changed.</summary>
    AlreadyCurrent = 2,
}
