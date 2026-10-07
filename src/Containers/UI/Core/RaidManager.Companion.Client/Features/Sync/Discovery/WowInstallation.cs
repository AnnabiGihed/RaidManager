namespace RaidManager.Companion.Client.Features.Sync.Discovery;

/// <summary>Represents one World of Warcraft installation and its account folders.</summary>
/// <param name="Folder">The installation's folder, the one holding <c>WTF\Account</c>.</param>
/// <param name="Accounts">The account folders under <c>WTF\Account</c>, by name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: An installation card of board 1 of <c>companion-sync</c> (#550).
/// </remarks>
public sealed record WowInstallation(string Folder, IReadOnlyList<WowAccount> Accounts);
