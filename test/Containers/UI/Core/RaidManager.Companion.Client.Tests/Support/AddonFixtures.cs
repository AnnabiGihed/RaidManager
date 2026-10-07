namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Reads the addon's SavedVariables fixtures copied next to the tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The companion reads the same files as the addon's tests and the API (<c>test/Fixtures/Addon/SavedVariables</c>),
/// each under the folder tree of a player's installation (#550).
/// </remarks>
internal static class AddonFixtures
{
    #region Public Properties
    /// <summary>Gets the folder holding the fixtures.</summary>
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Fixtures", "SavedVariables");
    #endregion Public Properties

    #region Public Methods
    /// <summary>Gets the path of a fixture's <c>RaidManager.lua</c>.</summary>
    /// <param name="fixture">The fixture, such as <c>one-character</c>.</param>
    /// <param name="account">The account folder, such as <c>ARTHASACCOUNT</c>.</param>
    /// <returns>The path.</returns>
    public static string PathOf(string fixture, string account) =>
        Path.Combine(Folder, fixture, "WTF", "Account", account, "SavedVariables", "RaidManager.lua");

    /// <summary>Reads a fixture's <c>RaidManager.lua</c>.</summary>
    /// <param name="fixture">The fixture, such as <c>one-character</c>.</param>
    /// <param name="account">The account folder, such as <c>ARTHASACCOUNT</c>.</param>
    /// <returns>The file's text.</returns>
    public static string Read(string fixture, string account) => File.ReadAllText(PathOf(fixture, account));
    #endregion Public Methods
}
