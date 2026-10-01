using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Shared.Layout;

/// <summary>Frames signed-in pages with the app shell and signed-out pages with the public frame.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Implements the shell of ADR-0019: a sidebar with the logo, the community card, the navigation and the
/// user card, and a top bar with the breadcrumb and Sign out. Signed-out pages show only the logo above a centered
/// column. The shell is dark; page contents keep the Radzen theme until the rest of #203.
/// </remarks>
public sealed partial class MainLayout : IDisposable
{
    #region Fields
    /// <summary>Stores the page error boundary, so a player can recover from an unexpected error.</summary>
    private ErrorBoundary? _errorBoundary;
    #endregion Fields

    #region Properties
    /// <summary>Gets the role shown in the user card; officer roles arrive with community linking (#14).</summary>
    private static string RoleLabel => ShellViewModel.RoleLabel(isOfficer: false);

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model that decides the sidebar and the breadcrumb.</summary>
    [Inject]
    private ShellViewModel Shell { get; set; } = default!;

    /// <summary>Gets the sidebar sections; nobody is an officer until community linking (#14).</summary>
    private IReadOnlyList<ShellNavigationGroup> NavigationGroups => Shell.Navigation(isOfficer: false);

    /// <summary>Gets the current page's title for the breadcrumb, if the shell knows the page.</summary>
    private string? PageTitle => Shell.PageTitle(Navigation.ToBaseRelativePath(Navigation.Uri));
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
    /// <summary>Reads the player's Discord avatar URL from the session.</summary>
    /// <param name="user">The signed-in player.</param>
    /// <returns>The URL, or <see langword="null"/> when the player has no avatar.</returns>
    private static string? AvatarUrl(ClaimsPrincipal user) => user.FindFirst(RaidManagerClaimTypes.AvatarUrl)?.Value;

    /// <summary>Renders again so the breadcrumb follows navigation between pages.</summary>
    /// <param name="sender">The navigation manager.</param>
    /// <param name="args">The new location.</param>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => InvokeAsync(StateHasChanged);

    /// <summary>Clears the error and renders the page again.</summary>
    private void Recover() => _errorBoundary?.Recover();
    #endregion Private Helpers
}
