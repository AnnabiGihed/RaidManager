using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Home.Pages;

/// <summary>Shows the signed-in player's Overview, or offers a visitor Discord sign-in.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The Overview entry of the shell's navigation, and the signed-out landing page of the sign-in mockup. A
/// player without a community sees the steps to link one (board 1 of the community mockup), with the reason the last
/// attempt stopped when they come back from it.
/// </remarks>
public sealed partial class Home : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets why adding RaidManager to a server stopped, when the user comes back from it.</summary>
    [SupplyParameterFromQuery(Name = CommunityRoutes.FailureParameter)]
    public string? LinkFailure { get; set; }

    /// <summary>Gets the steps of linking a community as step list items.</summary>
    private static IReadOnlyList<StepListItem> Steps { get; } =
        [.. OverviewViewModel.Steps.Select(step => new StepListItem(step.Title, step.Detail))];

    /// <summary>Gets or sets the signed-in user's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the Overview's view model.</summary>
    [Inject]
    private OverviewViewModel Overview { get; set; } = default!;
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadAsync();
        }
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Starts Discord sign-in; a full page load is required for the redirect to Discord.</summary>
    private void SignIn() => Navigation.NavigateTo(AuthenticationRoutes.SignIn, forceLoad: true);

    /// <summary>Starts adding RaidManager to a Discord server; a full page load is required for the redirect to Discord.</summary>
    private void AddBot() => Navigation.NavigateTo(CommunityRoutes.AddBot, forceLoad: true);

    /// <summary>Loads the signed-in player's community; a visitor has nothing to load.</summary>
    /// <returns>A task that completes when the Overview is loaded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        if (!Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var userId))
        {
            return;
        }

        await Overview.LoadAsync(userId, LinkFailure, _lifetime.Token);
        if (LinkFailure is not null)
        {
            // The notice is shown once: a clean address highlights Overview and a refresh doesn't repeat it.
            Navigation.NavigateTo("/", replace: true);
        }

        StateHasChanged();
    }
    #endregion Private Helpers
}
