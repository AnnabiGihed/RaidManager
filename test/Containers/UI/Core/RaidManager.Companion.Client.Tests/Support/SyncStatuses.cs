using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Sync.Upload;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Builds the sync statuses of the <c>companion-sync</c> boards.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The mockup's data in one place: two installations with four accounts, ALTACC excluded, a last success at
/// 14:05 UTC on 2026-10-08, and board 2's recent activity (#551). Account ids are the name followed by <c>-id</c>.
/// </remarks>
internal static class SyncStatuses
{
    #region Fields
    /// <summary>Stores the time the screens' tests run at, 15:00 UTC on 2026-10-08.</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the last success of the boards, 14:05 UTC the same day.</summary>
    public static readonly DateTimeOffset LastSuccess = new(2026, 10, 8, 14, 5, 0, TimeSpan.Zero);
    #endregion Fields

    #region Public Methods
    /// <summary>Builds a status in which nothing is found yet.</summary>
    /// <returns>The status.</returns>
    public static SyncStatus Empty() => new(false, false, UploadConnection.Unknown, null, 0, [], [], [], [], []);

    /// <summary>Builds board 2's status, changed by the arguments.</summary>
    /// <param name="paused">Whether uploads are paused.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="queued">The number of snapshots queued.</param>
    /// <param name="problems">The accounts' problems.</param>
    /// <param name="refusals">The refused snapshots.</param>
    /// <returns>The status.</returns>
    public static SyncStatus Watching(
        bool paused = false,
        UploadConnection connection = UploadConnection.Online,
        int queued = 2,
        IReadOnlyList<AccountProblem>? problems = null,
        IReadOnlyList<RefusedSnapshot>? refusals = null) =>
        new(paused, false, connection, LastSuccess, queued, Installations(), ["ALTACC-id"], Activity(), problems ?? [], refusals ?? []);

    /// <summary>Builds the mockup's two installations.</summary>
    /// <returns>The installations.</returns>
    public static IReadOnlyList<WowInstallation> Installations() =>
    [
        new(@"C:\Games\Warmane\World of Warcraft", [Account("ARTHASACC", 3), Account("JAINAACC", 2), Account("ALTACC", 4)]),
        new(@"D:\WoW\Warmane", [Account("THRALLACC", 1)]),
    ];

    /// <summary>Builds board 2's recent activity: one uploading, one waiting for WoW, three uploaded.</summary>
    /// <returns>The activity.</returns>
    public static IReadOnlyList<CharacterActivity> Activity() =>
    [
        new(new CharacterKey("Icecrown", "Arthasdk"), CharacterActivityState.Uploading),
        new(new CharacterKey("Lordaeron", "Jainaice"), CharacterActivityState.WaitingForWow),
        new(new CharacterKey("Icecrown", "Thrallsham"), CharacterActivityState.Uploaded, LastSuccess),
        new(new CharacterKey("Icecrown", "Sylvanash"), CharacterActivityState.Uploaded, LastSuccess.AddDays(-1)),
        new(new CharacterKey("Icecrown", "Bolvar"), CharacterActivityState.Uploaded, LastSuccess.AddDays(-5)),
    ];

    /// <summary>Builds an account row.</summary>
    /// <param name="name">The account folder's name.</param>
    /// <param name="characters">The number of characters.</param>
    /// <returns>The account.</returns>
    public static WowAccount Account(string name, int characters) => new($"{name}-id", name, $@"C:\WoW\{name}", characters);
    #endregion Public Methods
}
