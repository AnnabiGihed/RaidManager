using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Features.Shared;

namespace RaidManager.Companion.Tests.Support;

/// <summary>Brings a real sync view model into each board of <c>companion-sync</c>, over a sync double.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Lets the view tests show each board with the mockup's data (two installations, ALTACC excluded, a last
/// success at 14:05 UTC, board 2's activity), paired with Bryn (#551). Status changes reach the view model through
/// Avalonia's UI thread, as in the companion. Call from a headless test, on the UI thread.
/// </remarks>
internal sealed class SyncStates : IDisposable
{
    #region Fields
    /// <summary>Stores the time the boards show, 15:00 UTC on 2026-10-08.</summary>
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the last success the boards show, 14:05 UTC the same day.</summary>
    private static readonly DateTimeOffset LastSuccess = new(2026, 10, 8, 14, 5, 0, TimeSpan.Zero);

    /// <summary>Stores the character of the problem boards.</summary>
    private static readonly CharacterKey Jainaice = new("Lordaeron", "Jainaice");

    /// <summary>Stores the pairing driver.</summary>
    private readonly PairingStates _pairing = new();

    /// <summary>Stores the status the double returns.</summary>
    private SyncStatus _status = Watching();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SyncStates"/> class.</summary>
    public SyncStates()
    {
        Sync.SetupGet(sync => sync.Status).Returns(() => _status);
        Sync.Setup(sync => sync.AddFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var time = new FakeTimeProvider(Now);
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        ViewModel = new SyncViewModel(Sync.Object, Picker.Object, new AvaloniaUiThread(), _pairing.ViewModel, time, NullLogger<SyncViewModel>.Instance);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the sync double, to verify what the views asked of it.</summary>
    public Mock<ISnapshotSync> Sync { get; } = new();

    /// <summary>Gets the folder picker double.</summary>
    public Mock<IFolderPicker> Picker { get; } = new();

    /// <summary>Gets the view model.</summary>
    public SyncViewModel ViewModel { get; }

    /// <summary>Gets the pairing view model the footer reads.</summary>
    public PairingViewModel Pairing => _pairing.ViewModel;
    #endregion Public Properties

    #region Public Methods
    /// <summary>Builds the status of a board.</summary>
    /// <param name="board">The board's number in <c>companion-sync</c>, 2 to 8.</param>
    /// <returns>The status.</returns>
    public static SyncStatus Board(int board) => board switch
    {
        3 => Watching(paused: true, queued: 3),
        4 => Watching(connection: UploadConnection.Offline, queued: 3),
        5 => Watching(problems: [new("JAINAACC-id", AccountProblemKind.IncompleteSnapshot, [Jainaice])]),
        6 => Watching(refusals: [new(Jainaice, "Character.Snapshot.IdentityUnavailable", LastSuccess)]),
        7 => Watching(problems: [new("ARTHASACC-id", AccountProblemKind.UnsupportedSchema, [], "0.2.0")]),
        8 => Watching(problems: [new("ARTHASACC-id", AccountProblemKind.UnreadableFile, [])]),
        _ => Watching(),
    };

    /// <summary>Pairs the computer, so the footer names the player.</summary>
    /// <returns>A task that completes once paired.</returns>
    public Task PairAsync() => _pairing.ShowAsync(PairingState.Paired);

    /// <summary>Changes the status, as the sync does from its thread.</summary>
    /// <param name="status">The new status.</param>
    public void Change(SyncStatus status)
    {
        _status = status;
        Sync.Raise(sync => sync.StatusChanged += null, EventArgs.Empty);
    }

    /// <summary>Releases the view models.</summary>
    public void Dispose()
    {
        ViewModel.Dispose();
        _pairing.Dispose();
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds board 2's status, changed by the arguments.</summary>
    /// <param name="paused">Whether uploads are paused.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="queued">The number of snapshots queued.</param>
    /// <param name="problems">The accounts' problems.</param>
    /// <param name="refusals">The refused snapshots.</param>
    /// <returns>The status.</returns>
    private static SyncStatus Watching(
        bool paused = false,
        UploadConnection connection = UploadConnection.Online,
        int queued = 2,
        IReadOnlyList<AccountProblem>? problems = null,
        IReadOnlyList<RefusedSnapshot>? refusals = null)
    {
        IReadOnlyList<WowInstallation> installations =
        [
            new(@"C:\Games\Warmane\World of Warcraft", [Account("ARTHASACC", 3), Account("JAINAACC", 2), Account("ALTACC", 4)]),
            new(@"D:\WoW\Warmane", [Account("THRALLACC", 1)]),
        ];
        IReadOnlyList<CharacterActivity> activity =
        [
            new(new CharacterKey("Icecrown", "Arthasdk"), CharacterActivityState.Uploading),
            new(Jainaice, CharacterActivityState.WaitingForWow),
            new(new CharacterKey("Icecrown", "Thrallsham"), CharacterActivityState.Uploaded, LastSuccess),
            new(new CharacterKey("Icecrown", "Sylvanash"), CharacterActivityState.Uploaded, LastSuccess),
        ];
        return new(paused, false, connection, LastSuccess, queued, installations, ["ALTACC-id"], activity, problems ?? [], refusals ?? []);
    }

    /// <summary>Builds an account row.</summary>
    /// <param name="name">The account folder's name.</param>
    /// <param name="characters">The number of characters.</param>
    /// <returns>The account.</returns>
    private static WowAccount Account(string name, int characters) => new($"{name}-id", name, $@"C:\WoW\{name}", characters);
    #endregion Private Helpers
}
