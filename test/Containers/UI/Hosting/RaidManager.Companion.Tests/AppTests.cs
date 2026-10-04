using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Moq;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tray;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests;

/// <summary>Verifies what the application declares: the dark theme, the font and the tray icon of board 18.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The tray icon has the board's tooltip and menu, and its click and entries reach the tray view model
/// through compiled bindings (avalonia-tests §3).
/// </remarks>
public sealed class AppTests
{
    #region Tests
    /// <summary>The application is dark, and its resources hold the palette and the embedded Open Sans.</summary>
    [AvaloniaFact]
    public void ThemeIsDarkWithTheEmbeddedFont()
    {
        var app = Application.Current!;

        app.RequestedThemeVariant.ShouldBe(Avalonia.Styling.ThemeVariant.Dark);
        app.TryGetResource("BodyFont", null, out var font).ShouldBeTrue();
        font.ShouldBeOfType<Avalonia.Media.FontFamily>().Name.ShouldBe("Open Sans");
        app.TryGetResource("BrandAccentBrush", null, out _).ShouldBeTrue();
    }

    /// <summary>The tray icon shows the board's tooltip and menu: Open, a separator and Quit.</summary>
    [AvaloniaFact]
    public void TrayIconMatchesBoardEighteen()
    {
        var icon = TrayIcon.GetIcons(Application.Current!).ShouldNotBeNull().ShouldHaveSingleItem();

        icon.ToolTipText.ShouldBe("RaidManager Companion");
        icon.Icon.ShouldNotBeNull();
        var items = icon.Menu.ShouldNotBeNull().Items;
        items.Count.ShouldBe(3);
        items[0].ShouldBeOfType<NativeMenuItem>().Header.ShouldBe("Open");
        items[1].ShouldBeOfType<NativeMenuItemSeparator>();
        items[2].ShouldBeOfType<NativeMenuItem>().Header.ShouldBe("Quit");
    }

    /// <summary>A click on the icon and both entries reach the tray view model.</summary>
    [AvaloniaFact]
    public void TrayCommandsReachTheShell()
    {
        var shell = new Mock<IApplicationShell>();
        using var states = new PairingStates();
        var app = Application.Current!;
        app.DataContext = new TrayViewModel(shell.Object, states.ViewModel);
        var icon = TrayIcon.GetIcons(app)!.Single();
        var items = icon.Menu!.Items;

        icon.Command.ShouldNotBeNull().Execute(null);
        ((NativeMenuItem)items[0]).Command.ShouldNotBeNull().Execute(null);
        ((NativeMenuItem)items[2]).Command.ShouldNotBeNull().Execute(null);

        shell.Verify(s => s.ShowWindow(), Times.Exactly(2));
        shell.Verify(s => s.Quit(), Times.Once);
    }
    #endregion Tests
}
