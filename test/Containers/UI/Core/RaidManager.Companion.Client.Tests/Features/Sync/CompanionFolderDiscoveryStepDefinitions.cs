using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for finding World of Warcraft folders.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Verifies #550's folder discovery (owner decision): <c>Wow.exe</c> beside <c>WTF\Account</c>, searched down
/// to three folders below a drive's root, the system's folders skipped, and each account with its characters. The
/// drive is a temporary folder.
/// </remarks>
[Binding]
[Scope(Feature = "Companion folder discovery")]
public sealed class CompanionFolderDiscoveryStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the fake drive.</summary>
    private readonly WowDrive _drive = new();

    /// <summary>Stores the last installation created.</summary>
    private string _installation = string.Empty;

    /// <summary>Stores the installations found.</summary>
    private IReadOnlyList<string> _found = [];

    /// <summary>Stores the installation read.</summary>
    private WowInstallation? _read;
    #endregion Fields

    #region Public Methods
    /// <summary>Deletes the fake drive.</summary>
    public void Dispose() => _drive.Dispose();
    #endregion Public Methods

    #region Given Steps
    /// <summary>Installs the game in a folder.</summary>
    /// <param name="folder">The folder below the drive's root.</param>
    [Given("World of Warcraft is installed in {string}")]
    public void GivenWorldOfWarcraftIsInstalledIn(string folder) => _installation = _drive.AddInstallation(folder);

    /// <summary>Creates account folders without the game.</summary>
    /// <param name="folder">The folder below the drive's root.</param>
    [Given("a folder {string} holds account folders without the game")]
    public void GivenAFolderHoldsAccountFoldersWithoutTheGame(string folder) => _drive.AddAccountsFolder(folder);

    /// <summary>Creates an account's character folders.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="characters">The characters as <c>Realm/Name</c>, comma-separated.</param>
    [Given("the account {string} has the characters {string}")]
    public void GivenTheAccountHasTheCharacters(string account, string characters)
    {
        foreach (var character in characters.Split(", "))
        {
            var parts = character.Split('/');
            WowDrive.AddCharacter(_installation, account, parts[0], parts[1]);
        }
    }

    /// <summary>Creates the <c>WTF\Account\SavedVariables</c> folder WoW shares between accounts.</summary>
    [Given("the installation has a shared settings folder")]
    public void GivenTheInstallationHasASharedSettingsFolder() =>
        Directory.CreateDirectory(Path.Combine(_installation, "WTF", "Account", "SavedVariables", "Shared"));
    #endregion Given Steps

    #region When Steps
    /// <summary>Searches the fake drive.</summary>
    [When("the companion searches the drives")]
    public void WhenTheCompanionSearchesTheDrives() => _found = new WowInstallationFinder(_drive).Search(CancellationToken.None);

    /// <summary>Reads the last installation created.</summary>
    [When("the companion reads the installation")]
    public void WhenTheCompanionReadsTheInstallation() => _read = WowInstallationFinder.Describe(_installation);
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the installations found, in search order.</summary>
    /// <param name="found">The folders below the root, comma-separated, or <c>none</c>.</param>
    [Then("the installations found are {string}")]
    public void ThenTheInstallationsFoundAre(string found)
    {
        var expected = found == "none" ? [] : found.Split(", ").Select(_drive.PathOf).ToList();
        _found.ShouldBe(expected, ignoreOrder: true);
    }

    /// <summary>Checks the accounts and their character counts.</summary>
    /// <param name="accounts">The accounts as <c>NAME (count)</c>, comma-separated.</param>
    [Then("the accounts are {string}")]
    public void ThenTheAccountsAre(string accounts) =>
        string.Join(", ", _read.ShouldNotBeNull().Accounts.Select(account => $"{account.Name} ({account.CharacterCount})")).ShouldBe(accounts);
    #endregion Then Steps
}
