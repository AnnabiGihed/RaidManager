using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Tray;
using RaidManager.Companion.Composition;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Shell;

namespace RaidManager.Companion;

/// <summary>The companion application.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Builds the services once, shows the window, keeps running in the tray when the window closes, logs any
/// exception that escapes a command, and shows the window and checks the pairing when the player starts the companion
/// a second time.
/// </remarks>
public sealed partial class App : Application
{
    #region Fields
    /// <summary>Stores the signal of a second start, when this start owns the companion.</summary>
    private readonly IActivationSignal? _activation;

    /// <summary>Stores the host holding the services.</summary>
    private IHost? _host;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="App"/> class, for the designer and the tests.</summary>
    public App()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="App"/> class for a start that owns the companion.</summary>
    /// <param name="activation">The signal of a second start.</param>
    internal App(IActivationSignal? activation)
    {
        _activation = activation;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Compose(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes an exception that escaped to the dispatcher or was never observed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Critical, Message = "An exception escaped the companion's commands.")]
    private static partial void LogEscapedException(ILogger logger, Exception exception);

    /// <summary>Builds the services, the window and the tray, and starts the pairing.</summary>
    /// <param name="desktop">The desktop lifetime.</param>
    private void Compose(IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var shell = new ApplicationShell(desktop);
        _host = CompanionHost.Build(shell, CompanionEnvironment.Of(typeof(App).Assembly));
        _host.Start();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        Dispatcher.UIThread.UnhandledException += (_, e) => LogEscapedException(logger, e.Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => LogEscapedException(logger, e.Exception);

        var tray = _host.Services.GetRequiredService<TrayViewModel>();
        DataContext = tray;
        var pairing = _host.Services.GetRequiredService<PairingViewModel>();
        var window = new MainWindow { DataContext = pairing };
        shell.Attach(window);
        desktop.MainWindow = window;
        desktop.Exit += (_, _) => _host.Dispose();
        _activation?.Listen(() => Dispatcher.UIThread.Post(() => tray.OpenCommand.Execute(null)));
        pairing.StartCommand.Execute(null);
    }
    #endregion Private Helpers
}
