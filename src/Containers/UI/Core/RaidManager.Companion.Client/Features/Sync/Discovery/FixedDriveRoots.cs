namespace RaidManager.Companion.Client.Features.Sync.Discovery;

/// <summary>Lists the computer's fixed drives.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The default <see cref="IDriveRoots"/>: removable, network and optical drives are left out, as the owner's
/// decision on #550 searches "every fixed drive".
/// </remarks>
internal sealed class FixedDriveRoots : IDriveRoots
{
    #region Public Methods
    /// <inheritdoc />
    public IReadOnlyList<string> GetFixedDriveRoots() =>
        [.. DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady).Select(drive => drive.RootDirectory.FullName)];
    #endregion Public Methods
}
