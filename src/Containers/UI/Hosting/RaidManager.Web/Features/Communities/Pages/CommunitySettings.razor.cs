using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Communities.Pages;

/// <summary>Shows the signed-in user's community: its Discord server, realm and Administrator (board 4).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Reached from the sidebar's community card, and right after linking with a confirmation. The officer roles card reads the roles from Discord through the API; only the Administrator can change them (boards 4 and 8).
/// </remarks>
public sealed partial class CommunitySettings : IDisposable
{
    #region Fields
    /// <summary>Stores the token cancelled when the page goes away, so its calls stop with it.</summary>
    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>Stores whether the player closed the confirmation of linking.</summary>
    private bool _linkedClosed;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets a value indicating whether the server was just linked.</summary>
    [SupplyParameterFromQuery(Name = CommunityRoutes.LinkedParameter)]
    public bool JustLinked { get; set; }

    /// <summary>Gets or sets the signed-in user's authentication state.</summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the view model.</summary>
    [Inject]
    private CommunityViewModel ViewModel { get; set; } = default!;

    /// <summary>Gets or sets the view model of the officer roles card.</summary>
    [Inject]
    private CommunityRolesViewModel Roles { get; set; } = default!;
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
    /// <summary>Gives a chip's tone: a role deleted in Discord stands out.</summary>
    /// <param name="chip">The chip.</param>
    /// <returns>The tone.</returns>
    private static TagChipTone ChipTone(RoleChipView chip) => chip.Missing ? TagChipTone.Danger : TagChipTone.Info;

    /// <summary>Says what a chip's × does.</summary>
    /// <param name="chip">The chip.</param>
    /// <returns>The label, such as "Remove @Officier".</returns>
    private static string RemoveLabel(RoleChipView chip) => $"Remove {chip.Text}";

    /// <summary>Names a row's picker for screen readers.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The label, such as "Discord role for Officer".</returns>
    private static string PickerLabel(RoleRowView row) => $"Discord role for {row.Label}";

    /// <summary>Names a row's Edit link for screen readers.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The label, such as "Edit Veteran".</returns>
    private static string EditLabel(RoleRowView row) => $"Edit {row.Label}";

    /// <summary>Reads the signed-in user from the session.</summary>
    /// <returns>The user's id, display name and matched communities, or <see langword="null"/> when the session has no user id.</returns>
    private async Task<(Guid Id, string? Name, IReadOnlyList<Guid> MemberOf)?> SignedInUserAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        return Guid.TryParse(state?.User.FindFirst(RaidManagerClaimTypes.UserId)?.Value, out var id)
            ? (id, state!.User.Identity?.Name, state.User.MemberCommunityIds())
            : null;
    }

    /// <summary>Loads the user's community, or goes back to the Overview when they have none.</summary>
    /// <returns>A task that completes when the community is loaded.</returns>
    private async Task LoadAsync()
    {
        if (await SignedInUserAsync() is not { } user)
        {
            return;
        }

        await ViewModel.LoadForUserAsync(user.Id, user.MemberOf, _lifetime.Token);
        if (ViewModel.Status == CommunityPageStatus.Missing)
        {
            Navigation.NavigateTo("/");
            return;
        }

        StateHasChanged();
        await LoadRolesAsync();
    }

    /// <summary>Loads the officer roles card from Discord through the API.</summary>
    /// <returns>A task that completes when the card is loaded.</returns>
    private async Task LoadRolesAsync()
    {
        if (ViewModel.Community is { } community && await SignedInUserAsync() is { } user)
        {
            await Roles.LoadAsync(user.Id, community.CommunityId, _lifetime.Token);
            StateHasChanged();
        }
    }

    /// <summary>Gives the options of a row's picker: the mappable roles not already on it, written with "@".</summary>
    /// <param name="role">The row's RaidManager role.</param>
    /// <returns>The options.</returns>
    private IReadOnlyList<ChoiceOption<string>> PickerOptions(string role) =>
        [.. Roles.PickerOptions(role).Select(option => new ChoiceOption<string>(option.Id, $"@{option.Name}"))];

    /// <summary>Opens the members page.</summary>
    private void ViewMembers() => Navigation.NavigateTo(CommunityRoutes.Members);

    /// <summary>Records the Discord role chosen in the picker.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    private void PickRole(string discordRoleId) => Roles.PickedRoleId = discordRoleId;

    /// <summary>Maps the picked Discord role.</summary>
    /// <returns>A task that completes when the change was tried.</returns>
    private Task AddAsync() => Roles.AddPickedAsync(_lifetime.Token);

    /// <summary>Stops a Discord role giving a row's RaidManager role.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The row's RaidManager role.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    private Task RemoveAsync(string discordRoleId, string role) => Roles.RemoveAsync(discordRoleId, role, _lifetime.Token);

    /// <summary>Saves the role form.</summary>
    /// <returns>A task that completes when the save was tried.</returns>
    private Task SaveAsync() => Roles.SaveFormAsync(_lifetime.Token);

    /// <summary>Deletes the role being edited, once confirmed.</summary>
    /// <returns>A task that completes when the deletion was tried.</returns>
    private Task DeleteAsync() => Roles.DeleteAsync(_lifetime.Token);

    /// <summary>Keeps the confirmation of linking closed once it closes.</summary>
    private void ForgetLinked() => _linkedClosed = true;
    #endregion Private Helpers
}
