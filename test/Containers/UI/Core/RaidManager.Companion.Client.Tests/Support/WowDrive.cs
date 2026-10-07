using RaidManager.Companion.Client.Features.Sync.Discovery;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Builds a fake fixed drive with World of Warcraft installations in a temporary folder.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Gives the discovery and sync tests of #550 the folder tree a player's computer has (<c>Wow.exe</c>,
/// <c>WTF\Account\&lt;account&gt;\&lt;realm&gt;\&lt;character&gt;</c>, <c>SavedVariables\RaidManager.lua</c>) without
/// touching real drives. Each write of the addon's file gets a later modification time, as a real write would.
/// </remarks>
internal sealed class WowDrive : IDriveRoots, IDisposable
{
    #region Fields
    /// <summary>Stores the modification time the next write of an addon file gets.</summary>
    private DateTime _nextWrite = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
    #endregion Fields

    #region Public Properties
    /// <summary>Gets the drive's root folder.</summary>
    public string Root { get; } = Snapshots.TemporaryFolder();
    #endregion Public Properties

    #region Public Methods
    /// <summary>Creates a character folder of an account, creating the account when needed.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    /// <param name="realm">The realm.</param>
    /// <param name="character">The character's name.</param>
    public static void AddCharacter(string installation, string account, string realm, string character) =>
        Directory.CreateDirectory(Path.Combine(AccountFolder(installation, account), realm, character));

    /// <summary>Gets an account's folder.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    /// <returns>The account folder's full path.</returns>
    public static string AccountFolder(string installation, string account) => Path.Combine(installation, "WTF", "Account", account);

    /// <summary>Gets an account's hash, as the companion's files keep it.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    /// <returns>The hash.</returns>
    public static string AccountId(string installation, string account) => WowInstallationFinder.AccountId(AccountFolder(installation, account));

    /// <summary>Gets an account's <c>RaidManager.lua</c> path.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    /// <returns>The path.</returns>
    public static string AddonFilePath(string installation, string account) =>
        Path.Combine(AccountFolder(installation, account), "SavedVariables", "RaidManager.lua");

    /// <summary>Gets the drive's root as the only fixed drive.</summary>
    /// <returns>The root.</returns>
    public IReadOnlyList<string> GetFixedDriveRoots() => [Root];

    /// <summary>Creates a folder with the game, without accounts.</summary>
    /// <param name="relativePath">The installation's path below the root, with <c>/</c> between folders.</param>
    /// <returns>The installation's full path.</returns>
    public string AddInstallation(string relativePath)
    {
        var folder = AddAccountsFolder(relativePath);
        File.WriteAllText(Path.Combine(folder, "Wow.exe"), "game");
        return folder;
    }

    /// <summary>Creates a folder with <c>WTF\Account</c> but no game.</summary>
    /// <param name="relativePath">The folder's path below the root, with <c>/</c> between folders.</param>
    /// <returns>The folder's full path.</returns>
    public string AddAccountsFolder(string relativePath)
    {
        var folder = PathOf(relativePath);
        Directory.CreateDirectory(Path.Combine(folder, "WTF", "Account"));
        return folder;
    }

    /// <summary>Writes an account's <c>RaidManager.lua</c> with a later modification time than the previous write.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    /// <param name="text">The file's text.</param>
    public void WriteAddonFile(string installation, string account, string text)
    {
        var path = AddonFilePath(installation, account);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, _nextWrite);
        _nextWrite = _nextWrite.AddSeconds(1);
    }

    /// <summary>Gets the full path of a folder below the root.</summary>
    /// <param name="relativePath">The path below the root, with <c>/</c> between folders.</param>
    /// <returns>The full path.</returns>
    public string PathOf(string relativePath) => Path.Combine([Root, .. relativePath.Split('/')]);

    /// <summary>Deletes the drive's folder.</summary>
    public void Dispose() => Snapshots.DeleteFolder(Root);
    #endregion Public Methods
}
