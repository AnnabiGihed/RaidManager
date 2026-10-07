using System.Security.Cryptography;
using System.Text;

namespace RaidManager.Companion.Client.Features.Sync.Discovery;

/// <summary>Finds World of Warcraft installations and reads their account folders.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The folder discovery of #550 (owner decision): an installation is a folder with <c>Wow.exe</c> beside a
/// <c>WTF\Account</c> folder. The search looks in the usual places of every fixed drive, the drive's root and its
/// folders down to three levels (<c>C:\Program Files (x86)\World of Warcraft</c>,
/// <c>C:\Games\Warmane\World of Warcraft</c>), skipping the system's own folders, hidden folders and links; a full scan
/// of every drive was declined as too slow. A folder the player chooses needs only <c>WTF\Account</c>.
/// </remarks>
public sealed class WowInstallationFinder
{
    #region Constants
    /// <summary>Defines how many folder levels below a drive's root the search looks.</summary>
    private const int SearchDepth = 3;

    /// <summary>Defines the game's executable.</summary>
    private const string GameExecutable = "Wow.exe";

    /// <summary>Defines the folder of an account WoW keeps for every account, not an account itself.</summary>
    private const string SavedVariablesFolder = "SavedVariables";
    #endregion Constants

    #region Fields
    /// <summary>Stores the folders the search never enters.</summary>
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "Users", "ProgramData", "Recovery", "PerfLogs", "$Recycle.Bin", "System Volume Information",
        "$WinREAgent", "node_modules", ".git",
    };

    /// <summary>Stores how folders are listed: links, hidden and system folders are skipped, refused ones ignored.</summary>
    private static readonly EnumerationOptions Listing = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
    };

    /// <summary>Stores the drives to search.</summary>
    private readonly IDriveRoots _drives;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="WowInstallationFinder"/> class.</summary>
    /// <param name="drives">The drives to search.</param>
    public WowInstallationFinder(IDriveRoots drives)
    {
        _drives = drives;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Computes the hash the companion's files keep for an account folder.</summary>
    /// <param name="accountFolder">The account folder's full path.</param>
    /// <returns>The SHA-256 of the path in upper case, as hexadecimal.</returns>
    public static string AccountId(string accountFolder)
    {
        ArgumentNullException.ThrowIfNull(accountFolder);
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(accountFolder)).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    /// <summary>Reads an installation's account folders.</summary>
    /// <param name="folder">The installation's folder.</param>
    /// <returns>The installation, or <see langword="null"/> when the folder has no <c>WTF\Account</c>.</returns>
    public static WowInstallation? Describe(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var accountsFolder = AccountsFolder(folder);
        if (!Directory.Exists(accountsFolder))
        {
            return null;
        }

        var accounts = Directory.EnumerateDirectories(accountsFolder, "*", Listing)
            .Where(account => !string.Equals(Path.GetFileName(account), SavedVariablesFolder, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(account => new WowAccount(AccountId(account), Path.GetFileName(account), account, CountCharacters(account)))
            .ToList();
        return new WowInstallation(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)), accounts);
    }

    /// <summary>Searches the usual places of every fixed drive.</summary>
    /// <param name="cancellationToken">A token to stop the search.</param>
    /// <returns>The installation folders found, in search order.</returns>
    public IReadOnlyList<string> Search(CancellationToken cancellationToken)
    {
        var found = new List<string>();
        foreach (var root in _drives.GetFixedDriveRoots())
        {
            SearchFolder(root, 0, found, cancellationToken);
        }

        return found;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Gets the account folder of an installation.</summary>
    /// <param name="folder">The installation's folder.</param>
    /// <returns>The path of <c>WTF\Account</c>.</returns>
    private static string AccountsFolder(string folder) => Path.Combine(folder, "WTF", "Account");

    /// <summary>Determines whether a folder is a World of Warcraft installation.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns><see langword="true"/> when it holds <c>Wow.exe</c> and <c>WTF\Account</c>.</returns>
    private static bool IsInstallation(string folder) =>
        File.Exists(Path.Combine(folder, GameExecutable)) && Directory.Exists(AccountsFolder(folder));

    /// <summary>Counts the character folders of an account: one per realm and character.</summary>
    /// <param name="account">The account folder.</param>
    /// <returns>The number of characters.</returns>
    private static int CountCharacters(string account) =>
        Directory.EnumerateDirectories(account, "*", Listing)
            .Where(realm => !string.Equals(Path.GetFileName(realm), SavedVariablesFolder, StringComparison.OrdinalIgnoreCase))
            .Sum(realm => Directory.EnumerateDirectories(realm, "*", Listing).Count());

    /// <summary>Lists a folder's subfolders, or none when the folder vanished or can't be read.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>The subfolders.</returns>
    private static List<string> Children(string folder)
    {
        try
        {
            return [.. Directory.EnumerateDirectories(folder, "*", Listing)];
        }
        catch (IOException)
        {
            // A drive taken out or a folder deleted during the search: there is nothing to find there.
            return [];
        }
    }

    /// <summary>Looks for installations in a folder and its subfolders, down to the search depth.</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="depth">The folder's depth below its drive's root.</param>
    /// <param name="found">The installations found so far.</param>
    /// <param name="cancellationToken">A token to stop the search.</param>
    private static void SearchFolder(string folder, int depth, List<string> found, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsInstallation(folder))
        {
            found.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)));
            return;
        }

        if (depth == SearchDepth)
        {
            return;
        }

        foreach (var child in Children(folder))
        {
            if (!SkippedFolders.Contains(Path.GetFileName(child)))
            {
                SearchFolder(child, depth + 1, found, cancellationToken);
            }
        }
    }
    #endregion Private Helpers
}
