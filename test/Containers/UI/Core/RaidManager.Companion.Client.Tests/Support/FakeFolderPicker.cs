using RaidManager.Companion.Client.Features.Sync;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Stands in for Windows' folder picker.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Answers board 1's "Add a folder" with the folder the test chose, or a cancel (#551).
/// </remarks>
internal sealed class FakeFolderPicker : IFolderPicker
{
    #region Public Properties
    /// <summary>Gets or sets the folder the player chooses; <see langword="null"/> is a cancel.</summary>
    public string? Choice { get; set; }

    /// <summary>Gets the number of times the picker opened.</summary>
    public int Opened { get; private set; }
    #endregion Public Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<string?> PickFolderAsync(CancellationToken cancellationToken)
    {
        Opened++;
        return Task.FromResult(Choice);
    }
    #endregion Public Methods
}
