using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Communities.Pages;

/// <summary>Tells the user that the server they added the bot to already links to a community (board 3).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Reached only from Discord's return, or from the realm choice when the server was linked in the meantime; nothing changes on this page.
/// </remarks>
public sealed partial class AlreadyLinked : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the community the server links to.</summary>
    [SupplyParameterFromQuery(Name = CommunityRoutes.CommunityParameter)]
    public Guid? CommunityId { get; set; }

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
    /// <summary>Loads the community, or goes back to the Overview when the address names none.</summary>
    /// <returns>A task that completes when the community is loaded.</returns>
    private async Task LoadAsync()
    {
        await ViewModel.LoadAsync(CommunityId, _lifetime.Token);
        if (ViewModel.Status == CommunityPageStatus.Missing)
        {
            OpenOverview();
            return;
        }

        StateHasChanged();
    }

    /// <summary>Opens the Overview.</summary>
    private void OpenOverview() => Navigation.NavigateTo("/");
    #endregion Private Helpers
}
