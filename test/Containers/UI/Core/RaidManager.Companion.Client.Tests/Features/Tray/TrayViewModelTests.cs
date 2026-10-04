using Moq;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tray;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Tray;

/// <summary>Verifies the tray menu's commands.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="TrayViewModel"/>: Open shows the window and Quit ends the companion (board
/// 18 of the companion pairing mockup).
/// </remarks>
public sealed class TrayViewModelTests
{
    #region Fields
    /// <summary>Stores the shell double.</summary>
    private readonly Mock<IApplicationShell> _shell = new();
    #endregion Fields

    #region Tests
    /// <summary>Open shows the window.</summary>
    [Fact]
    public void OpenCommandShowsTheWindow()
    {
        new TrayViewModel(_shell.Object).OpenCommand.Execute(null);

        _shell.Verify(shell => shell.ShowWindow(), Times.Once);
        _shell.VerifyNoOtherCalls();
    }

    /// <summary>Quit ends the companion.</summary>
    [Fact]
    public void QuitCommandEndsTheCompanion()
    {
        new TrayViewModel(_shell.Object).QuitCommand.Execute(null);

        _shell.Verify(shell => shell.Quit(), Times.Once);
        _shell.VerifyNoOtherCalls();
    }

    /// <summary>The tray needs a shell.</summary>
    [Fact]
    public void ConstructorWithoutAShellThrows() => Should.Throw<ArgumentNullException>(() => new TrayViewModel(null!));
    #endregion Tests
}
