namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Describes the problem boards 5 to 8 show: the line under the heading, the notice, what to do and the retry.</summary>
/// <param name="Screen">The board.</param>
/// <param name="HeadingLine">The line under the heading, such as "One snapshot needs your help.".</param>
/// <param name="Title">The notice's title.</param>
/// <param name="Message">The notice's line.</param>
/// <param name="Steps">What to do, in the board's two lines.</param>
/// <param name="RetryLabel">The retry button's label, such as "Retry Jainaice".</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: One wording per problem, as boards 5 to 8 of <c>companion-sync</c> write it (owner decision on #551). The
/// file's problems come first, in the sync's order, then the snapshots RaidManager refused.
/// </remarks>
public sealed record SyncProblem(SyncScreen Screen, string HeadingLine, string Title, string Message, string Steps, string RetryLabel)
{
    #region Constants
    /// <summary>Defines the line under the heading of a character's problem.</summary>
    private const string SnapshotHeadingLine = "One snapshot needs your help.";

    /// <summary>Defines the line under the heading of an account's problem.</summary>
    private const string AccountHeadingLine = "An account's file needs your help.";

    /// <summary>Defines the label of an account's retry.</summary>
    private const string Retry = "Retry";
    #endregion Constants

    #region Factory Methods
    /// <summary>Finds the problem to show for a status.</summary>
    /// <param name="status">The sync's status.</param>
    /// <returns>The first problem, or <see langword="null"/> when nothing needs the player.</returns>
    public static SyncProblem? For(SyncStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Problems.Count > 0)
        {
            var problem = status.Problems[0];
            var account = AccountName(status, problem.AccountId);
            return problem.Kind switch
            {
                AccountProblemKind.IncompleteSnapshot => Incomplete(problem.Characters.Count > 0 ? problem.Characters[0].Name : account),
                AccountProblemKind.UnsupportedSchema => Unsupported(account, problem.AddonVersion),
                _ => Unreadable(account),
            };
        }

        return status.Refusals.Count > 0 ? Refused(status.Refusals[0].Character.Name) : null;
    }
    #endregion Factory Methods

    #region Private Helpers
    /// <summary>Builds board 5.</summary>
    /// <param name="name">The character, or the account when the file was cut before any character.</param>
    /// <returns>The problem.</returns>
    private static SyncProblem Incomplete(string name) => new(
        SyncScreen.IncompleteSnapshot,
        SnapshotHeadingLine,
        $"{name}'s snapshot is incomplete",
        "WoW didn't finish writing it, perhaps after a crash.",
        $"To fix it, log in with {name} in WoW and type /reload, or\nlog out. Then retry. Your other characters keep syncing.",
        $"Retry {name}");

    /// <summary>Builds board 6.</summary>
    /// <param name="name">The character.</param>
    /// <returns>The problem.</returns>
    private static SyncProblem Refused(string name) => new(
        SyncScreen.RefusedSnapshot,
        SnapshotHeadingLine,
        $"RaidManager refused {name}'s snapshot",
        "WoW didn't report who the character is, so it wasn't saved.",
        $"To fix it, log in with {name} in WoW, wait a few seconds\nand type /reload. Then retry. Others keep syncing.",
        $"Retry {name}");

    /// <summary>Builds board 7.</summary>
    /// <param name="account">The account.</param>
    /// <param name="version">The addon version that wrote the file, when known.</param>
    /// <returns>The problem.</returns>
    private static SyncProblem Unsupported(string account, string? version) => new(
        SyncScreen.UnsupportedAddon,
        AccountHeadingLine,
        "This addon version isn't supported",
        version is null ? $"{account}'s file was written by another addon version." : $"{account}'s file was written by addon {version}.",
        "Update the companion, or install the addon version that\ncame with it. Then retry. Other accounts keep syncing.",
        Retry);

    /// <summary>Builds board 8.</summary>
    /// <param name="account">The account.</param>
    /// <returns>The problem.</returns>
    private static SyncProblem Unreadable(string account) => new(
        SyncScreen.UnreadableFile,
        AccountHeadingLine,
        "This file isn't RaidManager's",
        $"{account}'s RaidManager.lua can't be read.",
        "Reinstall the RaidManager addon, log in to WoW and type\n/reload. Then retry. Other accounts keep syncing.",
        Retry);

    /// <summary>Finds an account's folder name from its hash.</summary>
    /// <param name="status">The status holding the installations.</param>
    /// <param name="accountId">The account's hash.</param>
    /// <returns>The name, or "An account" when the account is no longer listed.</returns>
    private static string AccountName(SyncStatus status, string accountId) =>
        status.Installations.SelectMany(installation => installation.Accounts)
            .FirstOrDefault(account => account.Id == accountId)?.Name ?? "An account";
    #endregion Private Helpers
}
