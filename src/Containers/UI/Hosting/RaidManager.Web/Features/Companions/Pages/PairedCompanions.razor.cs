using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.Web.Features.Authentication;

namespace RaidManager.Web.Features.Companions.Pages;

/// <summary>Lists the signed-in player's companions and lets them revoke one.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The Companion &amp; sync page (companion pairing boards 2 to 4 and 14 to 16, story #15). The companions load after the first interactive render, so prerendering doesn't call the API twice.
/// </remarks>
[Authorize]
public sealed partial class PairedCompanions : IDisposable
{
    #region Fields
    /// <summary>Stores the source of the token that cancels API calls when the player leaves the page.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores the latest notification.</summary>
    private CompanionNotice? _notice;

    /// <summary>Stores the companion whose revocation waits for confirmation, if any.</summary>
    private PairedCompanion? _revoking;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the label of the computer the player just paired, from the confirm page.</summary>
    [SupplyParameterFromQuery(Name = "paired")]
    public string? Paired { get; set; }

    /// <summary>Gets the table's column headings; the last column holds the action.</summary>
    private static IReadOnlyList<string> Headings { get; } = ["Computer", "Paired", "Last upload", "Status", string.Empty];

    /// <summary>Gets the table's column widths, as the mockup places the columns; the action takes the rest.</summary>
    private static IReadOnlyList<int> ColumnWidths { get; } = [276, 220, 240, 140, 0];

    /// <summary>Gets or sets the signed-in player's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the view model that lists and revokes the companions.</summary>
    [Inject]
    private PairedCompanionsViewModel ViewModel { get; set; } = default!;
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
    protected override void OnInitialized()
    {
        if (!string.IsNullOrWhiteSpace(Paired))
        {
            _notice = PairedCompanionsViewModel.PairedNotice(Paired);
        }
    }

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
    /// <summary>Loads the companions of the player in the session.</summary>
    /// <returns>A task that completes when the companions are shown or the failure is recorded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        var userId = Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id) ? id : (Guid?)null;
        await ViewModel.LoadAsync(userId, _lifetime.Token);
    }

    /// <summary>Asks for confirmation before revoking a companion.</summary>
    /// <param name="companion">The companion.</param>
    private void AskToRevoke(PairedCompanion companion) => _revoking = companion;

    /// <summary>Revokes the companion the player confirmed and shows the outcome.</summary>
    /// <returns>A task that completes when the outcome is shown.</returns>
    private async Task RevokeAsync()
    {
        if (_revoking is not { } companion)
        {
            return;
        }

        _revoking = null;
        _notice = await ViewModel.RevokeAsync(companion, _lifetime.Token);
    }

    /// <summary>Closes the confirmation without revoking anything.</summary>
    private void CancelRevoke() => _revoking = null;
    #endregion Private Helpers
}
