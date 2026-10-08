using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using RaidManager.Companion.Features.Shared;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Shared;

/// <summary>Verifies that work posted from another thread runs on Avalonia's UI thread.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The sync's status arrives on a pool thread; the screens may only change on the UI thread (#551).
/// </remarks>
public sealed class AvaloniaUiThreadTests
{
    #region Tests
    /// <summary>Work posted from a pool thread runs later, on the UI thread.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task PostedWorkRunsOnTheUiThread()
    {
        bool? ranOnUiThread = null;

        await Task.Run(() => new AvaloniaUiThread().Post(() => ranOnUiThread = Dispatcher.UIThread.CheckAccess()), TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        ranOnUiThread.ShouldBe(true);
    }
    #endregion Tests
}
