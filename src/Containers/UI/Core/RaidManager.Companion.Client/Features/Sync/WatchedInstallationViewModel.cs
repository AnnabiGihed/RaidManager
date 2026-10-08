namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Shows one installation card of board 1.</summary>
/// <param name="Folder">The installation's folder, such as <c>C:\Games\Warmane\World of Warcraft</c>.</param>
/// <param name="Accounts">Its account rows.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: An "INSTALLATION" card of <c>companion-sync</c> board 1 (#551).
/// </remarks>
public sealed record WatchedInstallationViewModel(string Folder, IReadOnlyList<WatchedAccountViewModel> Accounts);
