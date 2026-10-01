namespace RaidManager.ViewModels.Features.Shared.Shell;

/// <summary>Decides what the app shell shows: the sidebar sections, the breadcrumb and the user card.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements the shell of ADR-0019 with the owner's rule that the sidebar lists only pages that exist
/// (2026-10-01): each page's pull request registers its entry, and an empty section is left out.
/// </remarks>
public sealed class ShellViewModel
{
    #region Constants
    /// <summary>Defines the breadcrumb root shown while the player has no community.</summary>
    public const string ProductName = "RaidManager";

    /// <summary>Defines the community card's name while the player has no community.</summary>
    public const string NoCommunityName = "No community yet";

    /// <summary>Defines the community card's hint while the player has no community.</summary>
    public const string NoCommunityHint = "Link a Discord server";
    #endregion Constants

    #region Fields
    /// <summary>Stores the pages the shell knows, in sidebar order.</summary>
    private readonly IReadOnlyList<ShellEntry> _entries;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ShellViewModel"/> class.</summary>
    /// <param name="entries">The pages the shell knows, in sidebar order.</param>
    public ShellViewModel(IEnumerable<ShellEntry> entries)
    {
        _entries = [.. entries];
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Gets the initials shown when the player has no avatar: the first letters of the first two words.</summary>
    /// <param name="name">The player's display name.</param>
    /// <returns>One or two capital letters, or <c>?</c> for a blank name.</returns>
    public static string Initials(string? name)
    {
        var words = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length == 0
            ? "?"
            : string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    /// <summary>Gets the role shown under the player's name.</summary>
    /// <param name="isOfficer">Whether the player is an officer of their community.</param>
    /// <returns><c>Community officer</c> or <c>Player</c>.</returns>
    public static string RoleLabel(bool isOfficer) => isOfficer ? "Community officer" : "Player";

    /// <summary>Gets the sidebar sections the player sees, leaving out empty ones.</summary>
    /// <param name="isOfficer">Whether the player is an officer; only officers see the Officer section.</param>
    /// <returns>The sections in order.</returns>
    public IReadOnlyList<ShellNavigationGroup> Navigation(bool isOfficer)
    {
        var sections = isOfficer ? [ShellSection.Player, ShellSection.Officer] : new[] { ShellSection.Player };
        return [.. sections
            .Select(section => new ShellNavigationGroup(
                section.ToString().ToUpperInvariant(),
                [.. _entries.Where(entry => entry.Section == section && entry.InSidebar)]))
            .Where(group => group.Entries.Count > 0)];
    }

    /// <summary>Gets the title of the page at a URL, for the breadcrumb.</summary>
    /// <param name="relativeUri">The URL relative to the website's base, with or without a leading slash or a query.</param>
    /// <returns>The page title, or <see langword="null"/> when the shell doesn't know the page.</returns>
    public string? PageTitle(string relativeUri)
    {
        var path = Normalize(relativeUri);
        return _entries.FirstOrDefault(entry => string.Equals(Normalize(entry.Route), path, StringComparison.OrdinalIgnoreCase))?.Title;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Reduces a URL to its path without the query, the fragment, or leading and trailing slashes.</summary>
    /// <param name="uri">The URL.</param>
    /// <returns>The path, empty for the home page.</returns>
    private static string Normalize(string uri)
    {
        var end = uri.IndexOfAny(['?', '#']);
        return (end < 0 ? uri : uri[..end]).Trim('/');
    }
    #endregion Private Helpers
}
