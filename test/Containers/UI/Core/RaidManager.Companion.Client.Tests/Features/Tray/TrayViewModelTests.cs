using Moq;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tray;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Tray;

/// <summary>Verifies the tray menu's commands.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="TrayViewModel"/>: Open shows the window and checks the pairing at once
/// (owner decision on #524), and Quit ends the companion (board 18 of the companion pairing mockup).
/// </remarks>
public sealed class TrayViewModelTests : IDisposable
{
    #region Fields
    /// <summary>Stores the shell double.</summary>
    private readonly Mock<IApplicationShell> _shell = new();

    /// <summary>Stores the API double.</summary>
    private readonly FakeCompanionApi _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly FakeTokenStore _store = new();

    /// <summary>Stores the pairing the tray checks.</summary>
    private readonly PairingViewModel _pairing;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="TrayViewModelTests"/> class.</summary>
    public TrayViewModelTests()
    {
        _pairing = PairingViewModels.Create(_api, _store, new FakeBrowserLauncher(), new FlowClock(PairingViewModels.Start));
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Stops the pairing's flow.</summary>
    public void Dispose() => _pairing.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>Open shows the window.</summary>
    [Fact]
    public void OpenCommandShowsTheWindow()
    {
        new TrayViewModel(_shell.Object, _pairing).OpenCommand.Execute(null);

        _shell.Verify(shell => shell.ShowWindow(), Times.Once);
        _shell.VerifyNoOtherCalls();
    }

    /// <summary>Open checks the token of a paired companion at once, and a refusal shows the revoked state.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task OpenCommandChecksThePairing()
    {
        _store.Stored = new PairedCompanion(Guid.NewGuid(), "Bryn", "stored-token");
        _api.AnswerChecks(TokenCheckStatus.Valid, TokenCheckStatus.Refused);
        await Task.Run(_pairing.StartCommand.ExecuteAsync);

        new TrayViewModel(_shell.Object, _pairing).OpenCommand.Execute(null);

        _api.CheckedTokens.Count.ShouldBe(2);
        _pairing.State.ShouldBe(PairingState.Revoked);
    }

    /// <summary>Quit ends the companion.</summary>
    [Fact]
    public void QuitCommandEndsTheCompanion()
    {
        new TrayViewModel(_shell.Object, _pairing).QuitCommand.Execute(null);

        _shell.Verify(shell => shell.Quit(), Times.Once);
        _shell.VerifyNoOtherCalls();
    }

    /// <summary>The tray needs a shell and a pairing.</summary>
    [Fact]
    public void ConstructorWithoutItsPartsThrows()
    {
        Should.Throw<ArgumentNullException>(() => new TrayViewModel(null!, _pairing));
        Should.Throw<ArgumentNullException>(() => new TrayViewModel(_shell.Object, null!));
    }
    #endregion Tests
}
