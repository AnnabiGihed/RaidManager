using Microsoft.Extensions.Logging.Abstractions;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shell;
using RaidManager.Companion.Client.Features.Sync;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Builds the companion's window view models over test doubles: pairing, sync and shell.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: One place for the sync screens' tests (#551): a computer paired with Bryn, a sync the test drives, a
/// folder picker, a window thread the test runs, and a clock at 15:00 UTC on 2026-10-08 in the UTC time zone.
/// </remarks>
internal sealed class SyncScreens : IDisposable
{
    #region Constants
    /// <summary>Defines the player this computer is paired with.</summary>
    public const string Player = "Bryn";
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SyncScreens"/> class.</summary>
    public SyncScreens()
    {
        Time.SetLocalTimeZone(TimeZoneInfo.Utc);
        Pairing = PairingViewModels.Create(Api, Tokens, new FakeBrowserLauncher(), Time);
        Sync = new SyncViewModel(SyncDouble, Picker, Ui, Pairing, Time, NullLogger<SyncViewModel>.Instance);
        Shell = new ShellViewModel(Pairing, Sync);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the clock.</summary>
    public FlowClock Time { get; } = new(SyncStatuses.Now);

    /// <summary>Gets the pairing API double.</summary>
    public FakeCompanionApi Api { get; } = new();

    /// <summary>Gets the token store double.</summary>
    public FakeTokenStore Tokens { get; } = new();

    /// <summary>Gets the sync double.</summary>
    public FakeSnapshotSync SyncDouble { get; } = new();

    /// <summary>Gets the folder picker double.</summary>
    public FakeFolderPicker Picker { get; } = new();

    /// <summary>Gets the window's thread.</summary>
    public QueuedUiThread Ui { get; } = new();

    /// <summary>Gets the pairing view model.</summary>
    public PairingViewModel Pairing { get; }

    /// <summary>Gets the sync view model.</summary>
    public SyncViewModel Sync { get; }

    /// <summary>Gets the shell view model.</summary>
    public ShellViewModel Shell { get; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Starts the companion with a stored pairing, as a later start does.</summary>
    /// <returns>A task that completes once the pairing is loaded and checked.</returns>
    public Task StartPairedAsync()
    {
        Tokens.Stored = new PairedCompanion(Guid.NewGuid(), Player, "device-token");
        return Pairing.StartCommand.ExecuteAsync();
    }

    /// <summary>Changes the sync's status and runs the window's thread, as the companion does.</summary>
    /// <param name="status">The new status.</param>
    public void Change(SyncStatus status)
    {
        SyncDouble.Change(status);
        Ui.RunAll();
    }

    /// <summary>Releases the view models.</summary>
    public void Dispose()
    {
        Sync.Dispose();
        Pairing.Dispose();
    }
    #endregion Public Methods
}
