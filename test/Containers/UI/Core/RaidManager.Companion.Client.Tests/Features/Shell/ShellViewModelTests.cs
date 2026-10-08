using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Shell;

/// <summary>Verifies what the companion's window shows and its size.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The owner's decisions on #551: the window follows each board (480 x 600 pairing, 560 x 680 sync, title bar
/// included), a computer already paired opens on the sync, and board 6 shows after a new pairing until "Choose
/// folders" opens the watched folders.
/// </remarks>
public sealed class ShellViewModelTests : IDisposable
{
    #region Fields
    /// <summary>Stores the screens under test.</summary>
    private readonly SyncScreens _screens = new();
    #endregion Fields

    #region Public Methods
    /// <summary>Releases the view models.</summary>
    public void Dispose() => _screens.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>The window starts on the pairing at the pairing boards' size.</summary>
    [Fact]
    public void StartsOnThePairing()
    {
        _screens.Shell.Current.ShouldBeSameAs(_screens.Pairing);
        _screens.Shell.WindowWidth.ShouldBe(480);
        _screens.Shell.WindowHeight.ShouldBe(556);
    }

    /// <summary>A stored pairing opens the sync at the sync boards' size, on the board its status calls for.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StoredPairingOpensTheSync()
    {
        var sizes = new List<string?>();
        _screens.Shell.PropertyChanged += (_, e) => sizes.Add(e.PropertyName);

        await _screens.StartPairedAsync();

        _screens.Shell.Current.ShouldBeSameAs(_screens.Sync);
        _screens.Shell.WindowWidth.ShouldBe(560);
        _screens.Shell.WindowHeight.ShouldBe(636);
        sizes.ShouldContain(nameof(_screens.Shell.WindowHeight));
        _screens.Sync.Screen.ShouldBe(SyncScreen.Syncing);
    }

    /// <summary>A new pairing shows board 6; "Choose folders" opens board 1 of the sync.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task NewPairingShowsBoardSixUntilChooseFolders()
    {
        _screens.Api.Started = new StartedPairing("device-code", "K7M-4QX", SyncStatuses.Now.AddMinutes(10), TimeSpan.FromSeconds(5));
        _screens.Api.AnswerPoll(TokenPoll.Collected(new PairedCompanion(Guid.NewGuid(), SyncScreens.Player, "device-token")));
        await Task.Run(_screens.Pairing.StartCommand.ExecuteAsync);

        _screens.Time.AdvanceSeconds(5, () => _screens.Pairing.State != PairingState.Waiting);

        _screens.Pairing.State.ShouldBe(PairingState.Paired);
        _screens.Shell.Current.ShouldBeSameAs(_screens.Pairing);
        _screens.Pairing.ChooseFoldersCommand.Execute(null);
        _screens.Shell.Current.ShouldBeSameAs(_screens.Sync);
        _screens.Sync.Screen.ShouldBe(SyncScreen.WatchedFolders);
    }

    /// <summary>A pairing refused while the sync shows brings the pairing back.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task RevokedPairingShowsThePairing()
    {
        await _screens.StartPairedAsync();
        _screens.Api.Check = TokenCheckStatus.Refused;

        await _screens.Pairing.CheckPairingCommand.ExecuteAsync();

        _screens.Pairing.State.ShouldBe(PairingState.Revoked);
        _screens.Shell.Current.ShouldBeSameAs(_screens.Pairing);
        _screens.Shell.WindowWidth.ShouldBe(480);
    }
    #endregion Tests
}
