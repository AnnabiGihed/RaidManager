using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Characters;

namespace RaidManager.Web.Features.Characters.Pages;

/// <summary>Lets a signed-in player approve or reject the characters their companion found.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page sign-in opens while claims await the player's decision (story #18). It wires the view model to
/// Radzen; the claims load after the first interactive render, so prerendering doesn't call the API twice.
/// </remarks>
[Authorize]
public sealed partial class CharacterReview : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the page to continue to, from the query string.</summary>
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that loads and decides the claims.</summary>
    [Inject]
    private CharacterReviewViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the Radzen notification service.</summary>
    [Inject]
    private NotificationService Notifications { get; set; } = default!;

    /// <summary>Gets or sets the Radzen dialog service.</summary>
    [Inject]
    private DialogService Dialogs { get; set; } = default!;

    /// <summary>Gets a value indicating whether a decision is being sent.</summary>
    private bool IsDeciding => ViewModel.Deciding is not null;

    /// <summary>Gets a value indicating whether "Decide later" is offered: while something is left to decide or unknown.</summary>
    private bool CanDecideLater => ViewModel.Status != CharacterReviewStatus.Ready || ViewModel.PendingCount > 0;
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
    /// <summary>Gets the badge style of a claim's status.</summary>
    /// <param name="claim">The claim.</param>
    /// <returns>Warning for a pending claim, danger for a conflict.</returns>
    private static BadgeStyle StatusStyle(CharacterClaim claim) => claim.IsPending ? BadgeStyle.Warning : BadgeStyle.Danger;

    /// <summary>Maps a review notification kind to a Radzen severity.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The severity.</returns>
    private static NotificationSeverity SeverityOf(ReviewNoticeKind kind) => kind switch
    {
        ReviewNoticeKind.Success => NotificationSeverity.Success,
        ReviewNoticeKind.Warning => NotificationSeverity.Warning,
        ReviewNoticeKind.Error => NotificationSeverity.Error,
        _ => NotificationSeverity.Info,
    };

    /// <summary>Loads the claims of the player in the session.</summary>
    /// <returns>A task that completes when the claims are shown or the failure is recorded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        var userId = Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : (Guid?)null;
        await ViewModel.LoadAsync(userId, _lifetime.Token);
    }

    /// <summary>Approves a claim and shows the outcome.</summary>
    /// <param name="claim">The claim.</param>
    /// <returns>A task that completes when the outcome is shown.</returns>
    private async Task ApproveAsync(CharacterClaim claim) => Notify(await ViewModel.ApproveAsync(claim, _lifetime.Token));

    /// <summary>Asks for confirmation, then rejects a claim and shows the outcome.</summary>
    /// <param name="claim">The claim.</param>
    /// <returns>A task that completes when the outcome is shown, or when the player cancels.</returns>
    private async Task RejectAsync(CharacterClaim claim)
    {
        var confirmed = await Dialogs.Confirm(
            CharacterReviewViewModel.RejectMessage(claim),
            CharacterReviewViewModel.RejectTitle(claim),
            new ConfirmOptions { OkButtonText = "Reject", CancelButtonText = "Cancel" });
        if (confirmed == true)
        {
            Notify(await ViewModel.RejectAsync(claim, _lifetime.Token));
        }
    }

    /// <summary>Leaves the page for the requested page without deciding anything.</summary>
    private void Continue() => Navigation.NavigateTo(CharacterRoutes.ContinueUrl(ReturnUrl));

    /// <summary>Shows a review notification.</summary>
    /// <param name="notice">The notification.</param>
    private void Notify(ReviewNotice notice) => Notifications.Notify(new NotificationMessage
    {
        Severity = SeverityOf(notice.Kind),
        Summary = notice.Title,
        Detail = notice.Detail,
        Duration = 6000,
    });
    #endregion Private Helpers
}
