using Microsoft.AspNetCore.Components;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Home.Pages;

/// <summary>Greets the signed-in player, or offers a visitor Discord sign-in.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The Overview entry of the shell's navigation, and the signed-out landing page of the sign-in mockup.
/// </remarks>
public partial class Home
{
    #region Properties
    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
    #endregion Properties

    #region Private Helpers
    /// <summary>Starts Discord sign-in; a full page load is required for the redirect to Discord.</summary>
    private void SignIn() => Navigation.NavigateTo(AuthenticationRoutes.SignIn, forceLoad: true);
    #endregion Private Helpers
}
