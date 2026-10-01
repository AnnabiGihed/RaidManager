using Microsoft.AspNetCore.Components;
using RaidManager.ViewModels.Features.Authentication;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Authentication.Pages;

/// <summary>Explains why a Discord sign-in did not complete and offers a retry.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The destination of every failed or cancelled sign-in; the view model chooses the wording from the reason.
/// </remarks>
public partial class SignInFailed
{
    #region Properties
    /// <summary>Gets or sets the failure reason from the query string.</summary>
    [SupplyParameterFromQuery]
    public string? Reason { get; set; }

    /// <summary>Gets or sets the view model that explains the failure.</summary>
    [Inject]
    private SignInFailedViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
    #endregion Properties

    #region Overrides
    /// <inheritdoc />
    protected override void OnParametersSet() => ViewModel.Describe(Reason);
    #endregion Overrides

    #region Private Helpers
    /// <summary>Starts Discord sign-in again; a full page load is required for the redirect to Discord.</summary>
    private void TryAgain() => Navigation.NavigateTo(AuthenticationRoutes.SignIn, forceLoad: true);

    /// <summary>Returns to the home page.</summary>
    private void GoHome() => Navigation.NavigateTo("/");
    #endregion Private Helpers
}
