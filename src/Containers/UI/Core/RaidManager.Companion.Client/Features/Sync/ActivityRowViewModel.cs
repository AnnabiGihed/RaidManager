namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Shows one row of board 2's recent activity.</summary>
/// <param name="Name">The character's name.</param>
/// <param name="Realm">The character's realm.</param>
/// <param name="State">What the sync is doing with it.</param>
/// <param name="UploadedText">The text of an uploaded row, such as "Uploaded today, 14:05"; empty otherwise.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: A row of <c>companion-sync</c> board 2: uploading with its bar, waiting for WoW with its amber dot, or
/// uploaded with its time (#551).
/// </remarks>
public sealed record ActivityRowViewModel(string Name, string Realm, CharacterActivityState State, string UploadedText)
{
    #region Public Properties
    /// <summary>Gets a value indicating whether the snapshot is being sent.</summary>
    public bool IsUploading => State == CharacterActivityState.Uploading;

    /// <summary>Gets a value indicating whether WoW hasn't finished writing the snapshot.</summary>
    public bool IsWaitingForWow => State == CharacterActivityState.WaitingForWow;

    /// <summary>Gets a value indicating whether RaidManager accepted the latest snapshot.</summary>
    public bool IsUploaded => State == CharacterActivityState.Uploaded;
    #endregion Public Properties
}
