using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Queue;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Builds queued snapshots and queues in a temporary folder.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Gives the sync tests a character snapshot without a whole addon file, and a queue that never touches the
/// user profile (#550).
/// </remarks>
internal static class Snapshots
{
    #region Constants
    /// <summary>Defines the realm the tests' characters play on.</summary>
    public const string Realm = "Icecrown";

    /// <summary>Defines the account the tests' snapshots come from unless a test names one.</summary>
    public const string Account = "MAINACCOUNT";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds a queued snapshot.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    /// <param name="account">The account it came from.</param>
    /// <returns>The snapshot.</returns>
    public static QueuedSnapshot Queued(string name, long capturedAt = 100, string account = Account) =>
        new(account, Realm, name, capturedAt, "0.1.0", new JsonObject
        {
            ["realm"] = Realm,
            ["name"] = name,
            ["capturedAt"] = capturedAt,
        });

    /// <summary>Opens the queue kept in a folder.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>The queue, read from the folder's file.</returns>
    public static SnapshotQueue Queue(string folder) => new(new SyncFileLocation(folder), NullLogger<SnapshotQueue>.Instance);

    /// <summary>Gets a new temporary folder path, not created yet.</summary>
    /// <returns>The path.</returns>
    public static string TemporaryFolder() => Path.Combine(Path.GetTempPath(), $"companion-sync-tests-{Guid.NewGuid():N}");

    /// <summary>Deletes a temporary folder if it exists.</summary>
    /// <param name="folder">The folder.</param>
    public static void DeleteFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>Describes the waiting snapshots as the scenarios write them.</summary>
    /// <param name="queue">The queue.</param>
    /// <returns>The description, such as <c>Arthasdk at 100, Sylvanash at 100</c>, or <c>none</c>.</returns>
    public static string Describe(SnapshotQueue queue)
    {
        var waiting = string.Join(", ", queue.Waiting.Select(snapshot => $"{snapshot.Name} at {snapshot.CapturedAt}"));
        return waiting.Length == 0 ? "none" : waiting;
    }
    #endregion Public Methods
}
