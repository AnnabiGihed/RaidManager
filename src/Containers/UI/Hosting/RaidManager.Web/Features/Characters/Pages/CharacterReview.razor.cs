using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Characters;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Characters.Pages;

/// <summary>Lets a signed-in player approve or reject the characters their companion found.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page sign-in opens while claims await the player's decision (story #18), built from the design system's
/// components (character review mockup). The claims load after the first interactive render, so prerendering doesn't
/// call the API twice.
/// </remarks>
[Authorize]
public sealed partial class CharacterReview : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the outcome of the latest decision, shown as a notification.</summary>
    private ReviewNotice? _notice;

    /// <summary>Stores the claim whose rejection waits for confirmation, if any.</summary>
    private CharacterClaim? _rejecting;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the page to continue to, from the query string.</summary>
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>Gets the table's column headings.</summary>
    private static IReadOnlyList<string> Headings { get; } = ["Character", "Class", "Race", "Level", "Found", "Status", "Decision"];

    /// <summary>Gets the table's column widths, as the mockup places the columns; Decision takes the rest.</summary>
    private static IReadOnlyList<int> ColumnWidths { get; } = [240, 160, 120, 80, 160, 120, 0];

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that loads and decides the claims.</summary>
    [Inject]
    private CharacterReviewViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

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
    /// <summary>Gets the badge tone of a claim's status.</summary>
    /// <param name="claim">The claim.</param>
    /// <returns>Warning for a pending claim, danger for a conflict.</returns>
    private static TagChipTone StatusTone(CharacterClaim claim) => claim.IsPending ? TagChipTone.Warning : TagChipTone.Danger;

    /// <summary>Maps a review notification kind to a notification tone.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The tone.</returns>
    private static ToastTone ToneOf(ReviewNoticeKind kind) => kind switch
    {
        ReviewNoticeKind.Success => ToastTone.Success,
        ReviewNoticeKind.Warning => ToastTone.Warning,
        ReviewNoticeKind.Error => ToastTone.Danger,
        _ => ToastTone.Info,
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
    private async Task ApproveAsync(CharacterClaim claim) => _notice = await ViewModel.ApproveAsync(claim, _lifetime.Token);

    /// <summary>Asks for confirmation before rejecting a claim.</summary>
    /// <param name="claim">The claim.</param>
    private void AskToReject(CharacterClaim claim) => _rejecting = claim;

    /// <summary>Rejects the claim the player confirmed and shows the outcome.</summary>
    /// <returns>A task that completes when the outcome is shown.</returns>
    private async Task RejectAsync()
    {
        if (_rejecting is not { } claim)
        {
            return;
        }

        _rejecting = null;
        _notice = await ViewModel.RejectAsync(claim, _lifetime.Token);
    }

    /// <summary>Closes the confirmation without rejecting anything.</summary>
    private void CancelReject() => _rejecting = null;

    /// <summary>Leaves the page for the requested page without deciding anything.</summary>
    private void Continue() => Navigation.NavigateTo(CharacterRoutes.ContinueUrl(ReturnUrl));
    #endregion Private Helpers
}
