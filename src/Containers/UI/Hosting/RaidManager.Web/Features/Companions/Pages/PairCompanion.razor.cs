using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Companions.Pages;

/// <summary>Lets the signed-in player check a companion's code and confirm it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The page the companion opens with its code (companion pairing boards 1, 9 to 13 and 17, story #15). The code loads after the first interactive render, so prerendering doesn't call the API twice; a confirmed pairing moves on to the paired companions list.
/// </remarks>
[Authorize]
public sealed partial class PairCompanion : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the notification of a confirmation that failed.</summary>
    private CompanionNotice? _notice;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the code, from the companion's link.</summary>
    [SupplyParameterFromQuery(Name = "code")]
    public string? Code { get; set; }

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that checks and confirms the code.</summary>
    [Inject]
    private PairCompanionViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
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
    /// <summary>Checks the code for the player in the session.</summary>
    /// <returns>A task that completes when the answer is shown or the failure is recorded.</returns>
    private async Task LoadAsync()
    {
        _notice = null;
        await ViewModel.LoadAsync(await SessionUserIdAsync(), Code, _lifetime.Token);
    }

    /// <summary>Confirms the code, then shows the list on success or the notification on failure.</summary>
    /// <returns>A task that completes when the outcome is shown.</returns>
    private async Task ConfirmAsync()
    {
        var label = ViewModel.Pairing?.ComputerLabel ?? string.Empty;
        _notice = await ViewModel.ConfirmAsync(_lifetime.Token);
        if (ViewModel.CodeStatus == PairingCodeStatus.Paired)
        {
            Navigation.NavigateTo(CompanionRoutes.ListAfterPairing(label));
        }
    }

    /// <summary>Leaves for the paired companions list without confirming anything.</summary>
    private void ShowCompanions() => Navigation.NavigateTo(CompanionRoutes.List);

    /// <summary>Reads the player's id from the session.</summary>
    /// <returns>The id, or <see langword="null"/>.</returns>
    private async Task<Guid?> SessionUserIdAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        return Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : null;
    }
    #endregion Private Helpers
}
