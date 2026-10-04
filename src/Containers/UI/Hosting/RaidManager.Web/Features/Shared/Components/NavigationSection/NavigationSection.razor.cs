using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using RaidManager.ViewModels.Features.Shared.Shell;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a headed list of page links, highlighting the current page.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One sidebar section (ADR-0019), for any heading and pages; the theme's dark class styles Radzen's menu.
/// </remarks>
public sealed partial class NavigationSection : IDisposable
{
    #region Properties
    /// <summary>Gets or sets the section heading, for example <c>PLAYER</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string Heading { get; set; } = string.Empty;

    /// <summary>Gets or sets the pages to link, in order.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ShellEntry> Entries { get; set; } = [];

    /// <summary>Gets or sets the navigation manager that tells the current page.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;
    #endregion Overrides

    #region Private Helpers
    /// <summary>
    /// Tells whether the current page sits under an entry that stays highlighted on its subpages, such as the confirm
    /// page under Companion &amp; sync; the menu itself matches whole paths only, so Overview isn't lit everywhere.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns><see langword="true"/> when the current path starts with the entry's route and a slash.</returns>
    private bool IsUnder(ShellEntry entry)
    {
        if (!entry.MatchesSubpaths)
        {
            return false;
        }

        var path = Navigation.ToBaseRelativePath(Navigation.Uri);
        var end = path.IndexOfAny(['?', '#']);
        path = end < 0 ? path : path[..end];
        return path.StartsWith($"{entry.Route.Trim('/')}/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Re-renders when the page changes, so the highlight follows it.</summary>
    /// <param name="sender">The navigation manager.</param>
    /// <param name="args">The new location.</param>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => InvokeAsync(StateHasChanged);
    #endregion Private Helpers
}
