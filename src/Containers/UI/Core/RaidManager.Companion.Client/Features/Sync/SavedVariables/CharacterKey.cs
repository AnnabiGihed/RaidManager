namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Identifies a character the way the addon keys it: realm and name as the game returns them.</summary>
/// <param name="Realm">The realm, such as <c>Icecrown</c>.</param>
/// <param name="Name">The character's name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keys the upload queue and the sync activity, so one character has at most one queued snapshot (owner
/// decision on #550).
/// </remarks>
public sealed record CharacterKey(string Realm, string Name)
{
    #region Constants
    /// <summary>Defines the separator of the addon's <c>"realm|name"</c> keys.</summary>
    private const char Separator = '|';
    #endregion Constants

    #region Factory Methods
    /// <summary>Reads an addon key such as <c>Icecrown|Arthasdk</c>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The character key; a key without a separator is a name on an unknown realm.</returns>
    public static CharacterKey FromAddonKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var separator = key.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0 ? new CharacterKey(string.Empty, key) : new CharacterKey(key[..separator], key[(separator + 1)..]);
    }
    #endregion Factory Methods
}
