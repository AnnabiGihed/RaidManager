using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Sync;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Sync;

/// <summary>Verifies the folder picker behind board 1's "Add a folder".</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Without a window there is nothing to open the picker over, and a picker that returns no folder counts as a
/// cancel (#551). Windows' own picker is the owner's check of the companion build.
/// </remarks>
public sealed class AvaloniaFolderPickerTests
{
    #region Tests
    /// <summary>Before the window exists, the picker answers a cancel.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WithoutAWindowNothingIsPicked()
    {
        var picker = new AvaloniaFolderPicker(new ApplicationShell(null));

        (await picker.PickFolderAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>A picker that returns no folder, as the headless platform's does, answers a cancel.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task NoFolderChosenIsACancel()
    {
        var shell = new ApplicationShell(null);
        var window = new Window();
        shell.Attach(window);
        window.Show();
        var picker = new AvaloniaFolderPicker(shell);

        (await picker.PickFolderAsync(TestContext.Current.CancellationToken)).ShouldBeNull();
    }
    #endregion Tests
}
