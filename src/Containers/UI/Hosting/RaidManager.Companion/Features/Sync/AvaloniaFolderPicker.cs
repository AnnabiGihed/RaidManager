using Avalonia.Platform.Storage;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Features.Shared;

namespace RaidManager.Companion.Features.Sync;

/// <summary>Opens the operating system's folder picker over the companion's window.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Board 1's "Add a folder" opens Windows' folder picker (owner decision on #551), through the window's
/// storage provider.
/// </remarks>
internal sealed class AvaloniaFolderPicker : IFolderPicker
{
    #region Fields
    /// <summary>Stores the application shell holding the window.</summary>
    private readonly ApplicationShell _shell;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="AvaloniaFolderPicker"/> class.</summary>
    /// <param name="shell">The application shell holding the window.</param>
    public AvaloniaFolderPicker(ApplicationShell shell)
    {
        _shell = shell;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken)
    {
        if (_shell.Window is not { } window)
        {
            return null;
        }

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose your World of Warcraft folder",
            AllowMultiple = false,
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
    #endregion Public Methods
}
