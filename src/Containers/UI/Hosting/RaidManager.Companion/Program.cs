using Avalonia;
using RaidManager.Companion.Features.Shared;

namespace RaidManager.Companion;

/// <summary>Starts the companion.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Runs one companion per Windows user: a second start asks the first to show its window and exits. The
/// companion ships for Windows only (ADR-0030, ADR-0032).
/// </remarks>
internal static class Program
{
    #region Constants
    /// <summary>Defines the name the companion claims in the user's Windows session.</summary>
    private const string InstanceName = "RaidManager.Companion";
    #endregion Constants

    #region Public Methods
    /// <summary>Starts the companion, or brings the running one forward.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("The RaidManager companion runs on Windows only.");
            return 1;
        }

        using var instance = SingleInstance.Acquire(InstanceName);
        if (!instance.IsFirst)
        {
            instance.ActivateFirst();
            return 0;
        }

        return BuildAvaloniaApp(instance).StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Builds the application for the designer.</summary>
    /// <returns>The application builder.</returns>
    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(null);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the application.</summary>
    /// <param name="activation">The signal of a second start, or <see langword="null"/>.</param>
    /// <returns>The application builder.</returns>
    private static AppBuilder BuildAvaloniaApp(IActivationSignal? activation) =>
        AppBuilder.Configure(() => new App(activation)).UsePlatformDetect().LogToTrace();
    #endregion Private Helpers
}
