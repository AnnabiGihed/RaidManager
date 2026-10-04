using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Moq;
using RaidManager.Companion.Features.Shared;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Shared;

/// <summary>Verifies the shell the tray menu and a second start use, and the browser launcher over its window.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Open brings the window back from the tray or the taskbar, Quit ends the lifetime, and nothing breaks before
/// the window is attached.
/// </remarks>
public sealed class ApplicationShellTests
{
    #region Constants
    /// <summary>Defines what the headless platform's launcher answers: it has no browser, so it refuses.</summary>
    private const bool HeadlessLauncherAnswer = false;
    #endregion Constants

    #region Tests
    /// <summary>Open shows a hidden window and restores a minimized one.</summary>
    [AvaloniaFact]
    public void ShowWindowBringsTheWindowBack()
    {
        var window = new Window();
        var shell = new ApplicationShell(null);
        shell.Attach(window);
        window.Show();
        window.WindowState = WindowState.Minimized;
        window.Hide();

        shell.ShowWindow();

        window.IsVisible.ShouldBeTrue();
        window.WindowState.ShouldBe(WindowState.Normal);
        shell.Window.ShouldBeSameAs(window);
    }

    /// <summary>Before a window is attached, Open does nothing and the launcher refuses.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WithoutAWindowNothingOpens()
    {
        var shell = new ApplicationShell(null);

        shell.ShowWindow();
        shell.Quit();

        (await new AvaloniaBrowserLauncher(shell).OpenAsync(new Uri("https://raidmanager.test/"))).ShouldBeFalse();
    }

    /// <summary>Quit ends the desktop lifetime.</summary>
    [AvaloniaFact]
    public void QuitShutsTheLifetimeDown()
    {
        var lifetime = new Mock<IClassicDesktopStyleApplicationLifetime>();

        new ApplicationShell(lifetime.Object).Quit();

        lifetime.Verify(desktop => desktop.Shutdown(0), Times.Once);
    }

    /// <summary>With a window, the launcher asks the window's launcher and reports its answer.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WithAWindowTheLauncherAnswers()
    {
        var shell = new ApplicationShell(null);
        var window = new Window();
        window.Show();
        shell.Attach(window);

        var opened = await new AvaloniaBrowserLauncher(shell).OpenAsync(new Uri("https://raidmanager.test/"));

        opened.ShouldBe(HeadlessLauncherAnswer);
    }
    #endregion Tests
}
