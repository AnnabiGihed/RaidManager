using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Communities.Pages;

/// <summary>Shows the signed-in user's community: its Discord server, realm and Administrator (board 4).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Reached from the sidebar's community card, and right after linking with a confirmation. The officer roles card arrives with #289.
/// </remarks>
public sealed partial class CommunitySettings : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets a value indicating whether the server was just linked.</summary>
    [SupplyParameterFromQuery(Name = CommunityRoutes.LinkedParameter)]
    public bool JustLinked { get; set; }

    /// <summary>Gets or sets the signed-in user's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model.</summary>
    [Inject]
    private CommunityViewModel ViewModel { get; set; } = default!;
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
    /// <summary>Reads the signed-in user from the session.</summary>
    /// <returns>The user's id and display name, or <see langword="null"/> when the session has no user id.</returns>
    private async Task<(Guid Id, string? Name)?> SignedInUserAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        return Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id)
            ? (id, state!.User.Identity?.Name)
            : null;
    }

    /// <summary>Loads the user's community, or goes back to the Overview when they have none.</summary>
    /// <returns>A task that completes when the community is loaded.</returns>
    private async Task LoadAsync()
    {
        if (await SignedInUserAsync() is not { } user)
        {
            return;
        }

        await ViewModel.LoadForUserAsync(user.Id, _lifetime.Token);
        if (ViewModel.Status == CommunityPageStatus.Missing)
        {
            Navigation.NavigateTo("/");
            return;
        }

        StateHasChanged();
    }
    #endregion Private Helpers
}
