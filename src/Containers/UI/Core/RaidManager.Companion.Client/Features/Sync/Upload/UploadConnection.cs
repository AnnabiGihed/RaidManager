namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Defines whether uploads can reach RaidManager.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The connection part of the sync state #551 shows (#550): board 4 of <c>companion-sync</c> is
/// <see cref="Offline"/>.
/// </remarks>
public enum UploadConnection
{
    /// <summary>Nothing was uploaded yet since the companion started.</summary>
    Unknown,

    /// <summary>The last upload reached RaidManager.</summary>
    Online,

    /// <summary>The last upload couldn't reach RaidManager; it retries on its own.</summary>
    Offline,

    /// <summary>This computer isn't paired; nothing uploads.</summary>
    NotPaired,

    /// <summary>RaidManager refused the device token; nothing uploads until the companion pairs again.</summary>
    NeedsPairing,
}
