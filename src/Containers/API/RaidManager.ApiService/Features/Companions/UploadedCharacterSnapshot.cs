using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Represents what an uploaded snapshot changed.</summary>
/// <param name="Outcome"><c>Imported</c> when its facts were applied, <c>AlreadyCurrent</c> when a snapshot as recent was already applied.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Tells the companion it can drop the snapshot from its queue, and whether the upload was new (#384).
/// </remarks>
public sealed record UploadedCharacterSnapshot(string Outcome)
{
    #region Factory Methods
    /// <summary>Maps the application outcome to the transport record.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The transport record.</returns>
    public static UploadedCharacterSnapshot From(SnapshotImportOutcome outcome) => new(outcome.ToString());
    #endregion Factory Methods
}
