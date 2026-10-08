using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Shows one installation card of board 1 and holds the player's unsaved choice for its folder.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: An "INSTALLATION" card of <c>companion-sync</c> board 1, with its own checkbox: clearing it excludes the
/// whole folder, and its accounts show as excluded (board 10, owner decision on #551); ticking it again ticks every
/// account in it, dropping their earlier exclusions (owner decision on #593). The choice is saved by "Save and sync".
/// </remarks>
public sealed class WatchedInstallationViewModel : ViewModelBase
{
    #region Fields
    /// <summary>Stores whether the player wants the folder watched.</summary>
    private bool _isWatched;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="WatchedInstallationViewModel"/> class.</summary>
    /// <param name="folder">The installation's folder, such as <c>C:\Games\Warmane\World of Warcraft</c>.</param>
    /// <param name="isWatched">Whether the folder is watched now.</param>
    /// <param name="accounts">Its account rows.</param>
    public WatchedInstallationViewModel(string folder, bool isWatched, IReadOnlyList<WatchedAccountViewModel> accounts)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        Folder = folder;
        WasWatched = isWatched;
        Accounts = accounts;
        _isWatched = isWatched;
        ShowFolderChoice();
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the installation's folder.</summary>
    public string Folder { get; }

    /// <summary>Gets the account rows.</summary>
    public IReadOnlyList<WatchedAccountViewModel> Accounts { get; }

    /// <summary>Gets a value indicating whether the folder is watched in the saved choices.</summary>
    public bool WasWatched { get; }

    /// <summary>Gets or sets a value indicating whether the player wants the folder watched; ticking it ticks every
    /// account in it.</summary>
    public bool IsWatched
    {
        get => _isWatched;
        set
        {
            if (!SetProperty(ref _isWatched, value))
            {
                return;
            }

            ShowFolderChoice();
            if (value)
            {
                foreach (var account in Accounts)
                {
                    account.IsWatched = true;
                }
            }
        }
    }
    #endregion Public Properties

    #region Private Helpers
    /// <summary>Tells each account row whether its folder is watched.</summary>
    private void ShowFolderChoice()
    {
        foreach (var account in Accounts)
        {
            account.IsFolderWatched = _isWatched;
        }
    }
    #endregion Private Helpers
}
