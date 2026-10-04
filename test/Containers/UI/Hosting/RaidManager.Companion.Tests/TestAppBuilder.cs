using Avalonia;
using Avalonia.Headless;
using RaidManager.Companion.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace RaidManager.Companion.Tests;

/// <summary>Runs the real companion application headless, with its theme, styles and tray icon.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The one headless test application of the host's tests (avalonia-tests §2): headless drawing, so the tests
/// need no native rendering library on Linux.
/// </remarks>
public static class TestAppBuilder
{
    #region Public Methods
    /// <summary>Builds the application.</summary>
    /// <returns>The application builder.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    #endregion Public Methods
}
