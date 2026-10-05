using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Characters.Pages;

/// <summary>Shows what RaidManager knows about one of the signed-in player's characters.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Board 2 of the character profile mockup (story #19), built from the design system's components. Only the character's owner sees it (owner decision on #19, 2026-10-05); editing comes with #545. The profile loads after the first interactive render, so prerendering doesn't call the API twice.
/// </remarks>
[Authorize]
public sealed partial class CharacterProfilePage : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the character, from the route.</summary>
    [Parameter]
    public Guid CharacterId { get; set; }

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that loads the profile.</summary>
    [Inject]
    private CharacterProfileViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets the browser tab's title: the character's name once loaded.</summary>
    private string PageTitleText => ViewModel.Profile?.Name ?? "Character profile";
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
            StateHasChanged();
        }
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Loads the profile for the player in the session.</summary>
    /// <returns>A task that completes when the profile is shown or the outcome is recorded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        var userId = Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : (Guid?)null;
        await ViewModel.LoadAsync(userId, CharacterId, _lifetime.Token);
    }

    /// <summary>Goes back to the player's characters.</summary>
    private void BackToList() => Navigation.NavigateTo(CharacterRoutes.Mine);
    #endregion Private Helpers
}
