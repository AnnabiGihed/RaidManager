using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Communities.Pages;

/// <summary>Lists the people in the user's community's Discord server and the RaidManager role each one gets (board 5).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Reached from View members on the community page; the sidebar's community card leads back. A user without a community goes to the Overview.
/// </remarks>
public sealed partial class MembersAndRoles : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    #endregion Fields

    #region Properties
    /// <summary>Gets the table's column headings.</summary>
    private static IReadOnlyList<string> Headings { get; } = ["Member", "Discord roles", "RaidManager role"];

    /// <summary>Gets or sets the signed-in user's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model of the user's community.</summary>
    [Inject]
    private CommunityViewModel Community { get; set; } = default!;

    /// <summary>Gets or sets the view model of the members.</summary>
    [Inject]
    private CommunityMembersViewModel Members { get; set; } = default!;
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
    /// <summary>Gives a role's badge tone: the Administrator and Officer stand out, other roles are informational, Member is plain.</summary>
    /// <param name="role">The role's name.</param>
    /// <returns>The tone.</returns>
    private static TagChipTone RoleTone(string role) => role switch
    {
        "Administrator" or "Officer" => TagChipTone.Success,
        "Member" => TagChipTone.Neutral,
        _ => TagChipTone.Info,
    };

    /// <summary>Loads the user's community and its members, or goes back to the Overview when they have none.</summary>
    /// <returns>A task that completes when the members are loaded.</returns>
    private async Task LoadAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        if (!Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var userId))
        {
            return;
        }

        await Community.LoadForUserAsync(userId, state!.User.MemberCommunityIds(), _lifetime.Token);
        if (Community.Status == CommunityPageStatus.Missing)
        {
            Navigation.NavigateTo("/");
            return;
        }

        if (Community.Community is { } community)
        {
            await Members.LoadAsync(userId, community.CommunityId, _lifetime.Token);
        }
        else
        {
            Members.ShowUnavailable();
        }

        StateHasChanged();
    }
    #endregion Private Helpers
}
