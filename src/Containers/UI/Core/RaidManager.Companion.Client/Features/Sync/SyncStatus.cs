using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Upload;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Represents everything the sync screens show, at one moment.</summary>
/// <param name="Paused">Whether the player paused uploads (board 3); reading and queuing go on.</param>
/// <param name="Searching">Whether the companion is searching for WoW folders.</param>
/// <param name="Connection">Whether uploads reach RaidManager (board 4 when offline).</param>
/// <param name="LastSuccess">When RaidManager last accepted a snapshot since the companion started.</param>
/// <param name="Queued">The number of snapshots waiting to upload.</param>
/// <param name="Installations">The watched WoW installations and their accounts (board 1).</param>
/// <param name="ExcludedAccounts">The hashes of the accounts the player cleared.</param>
/// <param name="ExcludedFolders">The installation folders the player cleared; their accounts aren't watched.</param>
/// <param name="Activity">The recent activity, the upload in progress and the characters waiting for WoW first.</param>
/// <param name="Problems">The accounts whose file needs the player's help (board 5).</param>
/// <param name="Refusals">The snapshots RaidManager refused, the latest of each character.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The sync state of #550 that #551's screens show: pairing (through <see cref="Connection"/>), last success,
/// queued uploads, watched accounts and actionable failures, as #17's third criterion asks.
/// </remarks>
public sealed record SyncStatus(
    bool Paused,
    bool Searching,
    UploadConnection Connection,
    DateTimeOffset? LastSuccess,
    int Queued,
    IReadOnlyList<WowInstallation> Installations,
    IReadOnlyCollection<string> ExcludedAccounts,
    IReadOnlyCollection<string> ExcludedFolders,
    IReadOnlyList<CharacterActivity> Activity,
    IReadOnlyList<AccountProblem> Problems,
    IReadOnlyList<RefusedSnapshot> Refusals)
{
    #region Public Properties
    /// <summary>Gets the number of accounts found, such as the 4 of "3 of 4 watched".</summary>
    public int AccountCount => Installations.Sum(installation => installation.Accounts.Count);

    /// <summary>Gets the number of accounts watched, such as the 3 of "3 of 4 watched".</summary>
    public int WatchedAccountCount =>
        Installations.Where(IsWatched).Sum(installation => installation.Accounts.Count(account => !ExcludedAccounts.Contains(account.Id)));
    #endregion Public Properties

    #region Public Methods
    /// <summary>Tells whether an installation is watched, that is, the player didn't clear its folder.</summary>
    /// <param name="installation">The installation.</param>
    /// <returns><see langword="true"/> unless its folder is excluded.</returns>
    public bool IsWatched(WowInstallation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);
        return !ExcludedFolders.Contains(installation.Folder, StringComparer.OrdinalIgnoreCase);
    }
    #endregion Public Methods
}
