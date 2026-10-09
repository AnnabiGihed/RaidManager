using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Characters.Components;

/// <summary>Tells a signed-in player, wherever they are on the website, that a sync brought characters to review.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: The layout holds it on every signed-in page. Every <see cref="CharacterArrivalsViewModel.CheckInterval"/>
/// it asks for new claims; when a sync brought some, it shows the notification of <c>character-sync</c> board 1, whose
/// "Review them" link opens the review page, or on the review page itself the notification of board 4 while the page
/// updates its list (story #595). The checks start after the first interactive render, so prerendering asks nothing.
/// </remarks>
public sealed partial class CharacterArrivalsWatcher : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that stops the checks when the player leaves.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the characters of the notification shown, if any.</summary>
    private CharacterArrival? _arrival;

    /// <summary>Stores whether the notification was raised on the review page.</summary>
    private bool _onReviewPage;

    /// <summary>Stores the review page's address, returning to the page the player was on.</summary>
    private string _reviewHref = CharacterRoutes.Review;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that checks the claims.</summary>
    [Inject]
    private CharacterArrivalsViewModel Arrivals { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the clock the checks wait on.</summary>
    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;
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
            await WatchAsync();
        }
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Checks the claims now and every interval after, until the player leaves.</summary>
    /// <returns>A task that completes when the player leaves or the session holds no user id.</returns>
    private async Task WatchAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        if (!Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var userId))
        {
            return;
        }

        try
        {
            await Arrivals.CheckAsync(userId, _lifetime.Token);

            // Ends when the player leaves: the wait throws once the layout's token is canceled.
            while (true)
            {
                await Task.Delay(CharacterArrivalsViewModel.CheckInterval, TimeProvider, _lifetime.Token);
                if (await Arrivals.CheckAsync(userId, _lifetime.Token) is { } arrival)
                {
                    Show(arrival);
                }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // The player left; nothing is left to check.
        }
    }

    /// <summary>Shows the notification for the page the player is on.</summary>
    /// <param name="arrival">The characters that arrived.</param>
    private void Show(CharacterArrival arrival)
    {
        var here = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        var path = here.Split('?', '#')[0];
        _onReviewPage = string.Equals(path, CharacterRoutes.Review, StringComparison.OrdinalIgnoreCase);
        _reviewHref = CharacterRoutes.ReviewFor(here);
        _arrival = arrival;
        StateHasChanged();
    }

    /// <summary>Forgets the notification once it closes.</summary>
    private void Forget() => _arrival = null;
    #endregion Private Helpers
}
