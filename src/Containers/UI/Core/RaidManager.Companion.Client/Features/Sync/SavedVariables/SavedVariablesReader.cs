using System.Text.Json.Nodes;

namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Reads the character snapshots of the addon's <c>RaidManager.lua</c>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Applies the addon contract (<c>docs/reference/addon-savedvariables.md</c>, schema 1) to a parsed file:
/// <c>RaidManagerDB</c> holds <c>schemaVersion</c>, <c>addonVersion</c> and <c>characters</c> keyed
/// <c>"realm|name"</c>. Only a file of schema 1 gives snapshots; a cut file gives the characters before the cut and
/// names the one it cut (#550).
/// </remarks>
internal static class SavedVariablesReader
{
    #region Constants
    /// <summary>Defines the global the addon saves.</summary>
    public const string GlobalName = "RaidManagerDB";

    /// <summary>Defines the only schema version this companion uploads.</summary>
    public const int SupportedSchemaVersion = 1;

    /// <summary>Defines the key of the characters table.</summary>
    private const string CharactersKey = "characters";
    #endregion Constants

    #region Public Methods
    /// <summary>Reads a file's text.</summary>
    /// <param name="text">The text of <c>RaidManager.lua</c>.</param>
    /// <returns>The status, versions and snapshots read.</returns>
    public static SavedVariablesFile Read(string text)
    {
        LuaDocument document;
        try
        {
            document = LuaTableParser.Parse(text);
        }
        catch (FormatException)
        {
            return SavedVariablesFile.Unreadable();
        }

        if (!document.Globals.TryGetValue(GlobalName, out var node) || node is not JsonObject database)
        {
            return document.Truncated ? Cut(null, null, [], []) : SavedVariablesFile.Unreadable();
        }

        var schemaVersion = Integer(database["schemaVersion"]);
        var addonVersion = Text(database["addonVersion"]);
        if (schemaVersion is null)
        {
            // Without its version a file can't be trusted: cut before it, or not the addon's file.
            return document.Truncated ? Cut(null, addonVersion, [], []) : SavedVariablesFile.Unreadable();
        }

        if (schemaVersion != SupportedSchemaVersion)
        {
            return new SavedVariablesFile(SavedVariablesReadStatus.UnsupportedSchema, schemaVersion, addonVersion, [], []);
        }

        var cutCharacter = CutCharacterKey(document);
        var characters = new List<CharacterSnapshot>();
        var incomplete = new List<CharacterKey>();
        if (database[CharactersKey] is JsonObject entries)
        {
            foreach (var (key, value) in entries)
            {
                var snapshot = key == cutCharacter ? null : Snapshot(value);
                if (snapshot is null)
                {
                    incomplete.Add(CharacterKey.FromAddonKey(key));
                }
                else
                {
                    characters.Add(snapshot);
                }
            }
        }

        return document.Truncated || incomplete.Count > 0
            ? Cut(schemaVersion, addonVersion, characters, incomplete)
            : new SavedVariablesFile(SavedVariablesReadStatus.Complete, schemaVersion, addonVersion, characters, []);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the result for a cut file.</summary>
    /// <param name="schemaVersion">The schema version, when read.</param>
    /// <param name="addonVersion">The addon version, when read.</param>
    /// <param name="characters">The complete characters.</param>
    /// <param name="incomplete">The cut characters.</param>
    /// <returns>The result.</returns>
    private static SavedVariablesFile Cut(long? schemaVersion, string? addonVersion, List<CharacterSnapshot> characters, List<CharacterKey> incomplete) =>
        new(SavedVariablesReadStatus.Incomplete, schemaVersion, addonVersion, characters, incomplete);

    /// <summary>Finds the key of the character entry the end of the file cut.</summary>
    /// <param name="document">The parsed file.</param>
    /// <returns>The <c>"realm|name"</c> key, or <see langword="null"/> when no character was cut.</returns>
    private static string? CutCharacterKey(LuaDocument document) =>
        document.CutPath is [GlobalName, CharactersKey, var key, ..] ? key : null;

    /// <summary>Reads one character entry; it needs its realm, name and capture time.</summary>
    /// <param name="value">The entry.</param>
    /// <returns>The snapshot, or <see langword="null"/> when the entry lacks one of them.</returns>
    private static CharacterSnapshot? Snapshot(JsonNode? value)
    {
        if (value is not JsonObject body)
        {
            return null;
        }

        var realm = Text(body["realm"]);
        var name = Text(body["name"]);
        var capturedAt = Integer(body["capturedAt"]);
        return string.IsNullOrEmpty(realm) || string.IsNullOrEmpty(name) || capturedAt is null
            ? null
            : new CharacterSnapshot(new CharacterKey(realm, name), capturedAt.Value, body);
    }

    /// <summary>Reads a string value.</summary>
    /// <param name="node">The value.</param>
    /// <returns>The string, or <see langword="null"/> for any other value.</returns>
    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    /// <summary>Reads an integer value.</summary>
    /// <param name="node">The value.</param>
    /// <returns>The integer, or <see langword="null"/> for any other value.</returns>
    private static long? Integer(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out long integer) ? integer : null;
    #endregion Private Helpers
}
