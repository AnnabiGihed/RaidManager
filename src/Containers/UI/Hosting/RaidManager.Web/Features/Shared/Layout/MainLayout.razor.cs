using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Shared.Layout;

/// <summary>Frames every page with the header, the player's session controls, and an error boundary.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Shows "Sign in with Discord" to visitors and the player's name with "Sign out" once signed in.
/// </remarks>
public partial class MainLayout
{
    #region Fields
    /// <summary>Stores the page error boundary, so a player can recover from an unexpected error.</summary>
    private ErrorBoundary? _errorBoundary;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
    #endregion Properties

    #region Private Helpers
    /// <summary>Starts Discord sign-in, returning to the current page afterwards.</summary>
    /// <remarks>A full page load is required: sign-in is an HTTP redirect to Discord, not a Blazor route.</remarks>
    private void SignIn()
    {
        var returnUrl = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        Navigation.NavigateTo($"{AuthenticationRoutes.SignIn}?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }

    /// <summary>Clears the error and renders the page again.</summary>
    private void Recover() => _errorBoundary?.Recover();
    #endregion Private Helpers
}
