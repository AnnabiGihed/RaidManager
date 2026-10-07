namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Represents what was read from one <c>RaidManager.lua</c>.</summary>
/// <param name="Status">Whether the file was complete, cut, unreadable or of another schema.</param>
/// <param name="SchemaVersion">The file's <c>schemaVersion</c>, when it was read.</param>
/// <param name="AddonVersion">The file's <c>addonVersion</c>, when it was read.</param>
/// <param name="Characters">The complete character snapshots, in the file's order.</param>
/// <param name="IncompleteCharacters">The characters whose entry was cut or lacks its realm, name or capture time.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The result of <see cref="SavedVariablesReader"/> (#550).
/// </remarks>
internal sealed record SavedVariablesFile(
    SavedVariablesReadStatus Status,
    long? SchemaVersion,
    string? AddonVersion,
    IReadOnlyList<CharacterSnapshot> Characters,
    IReadOnlyList<CharacterKey> IncompleteCharacters)
{
    #region Factory Methods
    /// <summary>Builds the result for a file that can't be read.</summary>
    /// <returns>The result.</returns>
    public static SavedVariablesFile Unreadable() => new(SavedVariablesReadStatus.Unreadable, null, null, [], []);
    #endregion Factory Methods
}
