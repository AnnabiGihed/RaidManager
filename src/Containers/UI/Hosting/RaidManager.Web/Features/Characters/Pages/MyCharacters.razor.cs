using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Characters.Pages;

/// <summary>Lists the signed-in player's characters, each opening its profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Board 1 of the character profile mockup (story #19), built from the design system's components. The list loads after the first interactive render, so prerendering doesn't call the API twice.
/// </remarks>
[Authorize]
public sealed partial class MyCharacters : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets the table's column headings.</summary>
    private static IReadOnlyList<string> Headings { get; } = ["Character", "Level", "Primary loadout", "Raid saves", "Last sync"];

    /// <summary>Gets the table's column widths, as the mockup places the columns; Last sync takes the rest.</summary>
    private static IReadOnlyList<int> ColumnWidths { get; } = [236, 80, 300, 180, 0];

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that loads the characters.</summary>
    [Inject]
    private MyCharactersViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the check that tells when a sync brought characters, so the notice follows it.</summary>
    [Inject]
    private CharacterArrivalsViewModel Arrivals { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public void Dispose()
    {
        Arrivals.Arrived -= OnArrived;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void OnInitialized() => Arrivals.Arrived += OnArrived;

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
    /// <summary>Maps how fresh a character's data is to the last sync badge's tone.</summary>
    /// <param name="freshness">The freshness.</param>
    /// <returns>Success when fresh, warning when stale, neutral when never synchronized.</returns>
    private static TagChipTone ToneOf(SyncFreshness freshness) => freshness switch
    {
        SyncFreshness.Fresh => TagChipTone.Success,
        SyncFreshness.Stale => TagChipTone.Warning,
        _ => TagChipTone.Neutral,
    };

    /// <summary>Loads the characters of the player in the session.</summary>
    /// <returns>A task that completes when the characters are shown or the failure is recorded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        var userId = Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : (Guid?)null;
        await ViewModel.LoadAsync(userId, _lifetime.Token);
        await ViewModel.LoadWaitingAsync(userId, _lifetime.Token);
    }

    /// <summary>Counts the characters waiting again when a sync brought some, without a refresh (#595).</summary>
    /// <param name="sender">The check.</param>
    /// <param name="arrival">The characters that arrived.</param>
    private void OnArrived(object? sender, CharacterArrival arrival) => _ = InvokeAsync(RecountAsync);

    /// <summary>Counts the characters waiting for review and shows the notice.</summary>
    /// <returns>A task that completes when the notice is shown.</returns>
    private async Task RecountAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        var userId = Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : (Guid?)null;
        await ViewModel.LoadWaitingAsync(userId, _lifetime.Token);
        StateHasChanged();
    }

    /// <summary>Opens the review page, returning here when the player continues.</summary>
    private void Review() => Navigation.NavigateTo(CharacterRoutes.ReviewFor(CharacterRoutes.Mine));
    #endregion Private Helpers
}
