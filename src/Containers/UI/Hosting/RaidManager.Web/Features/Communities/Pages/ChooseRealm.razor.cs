using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Communities.Pages;

/// <summary>Lets the user who just added the bot choose the community's Warmane realm and finish linking (board 2).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Reached only from Discord's return after adding the bot (CommunityLinkEndpoints), with a protected pending link; an invalid or expired link goes back to the Overview, which says so.
/// </remarks>
public sealed partial class ChooseRealm : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the protected pending link from Discord's return.</summary>
    [SupplyParameterFromQuery(Name = CommunityRoutes.LinkParameter)]
    public string? Link { get; set; }

    /// <summary>Gets the realms as choice options.</summary>
    private static IReadOnlyList<ChoiceOption<string>> RealmOptions { get; } =
        [.. ChooseRealmViewModel.Realms.Select(realm => new ChoiceOption<string>(realm, realm))];

    /// <summary>Gets or sets the signed-in user's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model.</summary>
    [Inject]
    private ChooseRealmViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the protector that reads the pending link.</summary>
    [Inject]
    private CommunityLinkProtector Protector { get; set; } = default!;
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
    protected override async Task OnInitializedAsync()
    {
        var user = await SignedInUserAsync();
        ViewModel.Initialize(user is { } signedIn ? Protector.Unprotect(Link, signedIn.Id) : null, user?.Name);
        if (ViewModel.Status == CommunityPageStatus.Missing)
        {
            Navigation.NavigateTo(CommunityRoutes.OverviewAfter(CommunityLinkFailure.Expired));
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

    /// <summary>Records the chosen realm.</summary>
    /// <param name="realm">The realm's name.</param>
    private void SelectRealm(string realm) => ViewModel.SelectedRealm = realm;

    /// <summary>Links the server and opens the community, or the existing one if it was linked in the meantime.</summary>
    /// <returns>A task that completes when the link was tried.</returns>
    private async Task FinishAsync()
    {
        switch (await ViewModel.FinishAsync(_lifetime.Token))
        {
            case ChooseRealmOutcome.Linked:
                // A full load starts a new circuit, so the sidebar shows the new community.
                Navigation.NavigateTo(CommunityRoutes.SettingsAfterLinking(), forceLoad: true);
                break;
            case ChooseRealmOutcome.AlreadyLinked:
                Navigation.NavigateTo(CommunityRoutes.AlreadyLinkedFor(ViewModel.CommunityId!.Value));
                break;
            default:
                break;
        }
    }

    /// <summary>Goes back to the Overview without linking.</summary>
    private void Cancel() => Navigation.NavigateTo("/");
    #endregion Private Helpers
}
