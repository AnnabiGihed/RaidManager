using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Shared.Layout;

/// <summary>Frames signed-in pages with the app shell and signed-out pages with the public frame.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Composes the shell of ADR-0019 from shared components: a sidebar with the logo, the community card, the
/// navigation and the user card, and a top bar with the breadcrumb and Sign out. Signed-out pages show only the logo
/// above a centered column. The theme's dark class colors both; page contents keep the Radzen theme until #203.
/// </remarks>
public sealed partial class MainLayout : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the layout goes away, so its loading stops with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the page error boundary, so a player can recover from an unexpected error.</summary>
    private ErrorBoundary? _errorBoundary;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the authentication state provider, which names the signed-in user.</summary>
    [Inject]
    private AuthenticationStateProvider Authentication { get; set; } = default!;

    /// <summary>Gets or sets the view model of the sidebar's community card and the user's role.</summary>
    [Inject]
    private ShellCommunityViewModel Community { get; set; } = default!;

    /// <summary>Gets where the community card leads: the community page, or the Overview to link one.</summary>
    private string CommunityHref => Community.Community is null ? "/" : CommunityRoutes.Settings;

    /// <summary>Gets the community card's glyph look: initials in the community color, or a neutral "+".</summary>
    private IconTileAppearance CommunityGlyphAppearance => Community.Community is null ? IconTileAppearance.Neutral : IconTileAppearance.Community;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model that decides the sidebar and the breadcrumb.</summary>
    [Inject]
    private ShellViewModel Shell { get; set; } = default!;

    /// <summary>Gets the sidebar sections; nobody is an officer until community linking (#14).</summary>
    private IReadOnlyList<ShellNavigationGroup> NavigationGroups => Shell.Navigation(isOfficer: false);

    /// <summary>Gets the breadcrumb: the product, then the current page when the shell knows it.</summary>
    private IReadOnlyList<string> BreadcrumbItems =>
        Shell.PageTitle(Navigation.ToBaseRelativePath(Navigation.Uri)) is { } page
            ? [ShellViewModel.ProductName, page]
            : [ShellViewModel.ProductName];
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // After the first render, so the prerender doesn't ask the API a second time.
        if (!firstRender)
        {
            return;
        }

        var user = (await Authentication.GetAuthenticationStateAsync()).User;
        if (Guid.TryParse(user.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var userId))
        {
            await Community.LoadAsync(userId, user.MemberCommunityIds(), _lifetime.Token);
            StateHasChanged();
        }
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Renders again so the breadcrumb follows navigation between pages.</summary>
    /// <param name="sender">The navigation manager.</param>
    /// <param name="args">The new location.</param>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => InvokeAsync(StateHasChanged);

    /// <summary>Clears the error and renders the page again.</summary>
    private void Recover() => _errorBoundary?.Recover();
    #endregion Private Helpers
}
