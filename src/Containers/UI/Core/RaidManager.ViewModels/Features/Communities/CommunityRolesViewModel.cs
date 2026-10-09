using System.Globalization;

namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the roles card of the community page: its rows, the role picker, the role form and the outcome of each change (boards 9 to 15).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the roles card's loading, wording and changes testable without a browser; the page only lays it out.
/// </remarks>
public sealed class CommunityRolesViewModel
{
    #region Constants
    /// <summary>Defines the confirmation title after a change.</summary>
    public const string SavedTitle = "Roles saved";

    /// <summary>Defines the confirmation message after a change.</summary>
    public const string SavedMessage = "Members get them at their next check.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;

    /// <summary>Stores the signed-in user.</summary>
    private Guid _userId;

    /// <summary>Stores the community.</summary>
    private Guid _communityId;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityRolesViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    public CommunityRolesViewModel(ICommunitiesApiClient api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the card's state.</summary>
    public CommunityPageStatus Status { get; private set; } = CommunityPageStatus.Loading;

    /// <summary>Gets the roles card once loaded.</summary>
    public CommunityRoleSettings? Settings { get; private set; }

    /// <summary>Gets the rows as the page shows them.</summary>
    public IReadOnlyList<RoleRowView> Rows { get; private set; } = [];

    /// <summary>Gets the notice explaining why the card or the last change failed, if any.</summary>
    public LinkNotice? Problem { get; private set; }

    /// <summary>Gets a value indicating whether the last change was saved, so the page shows the confirmation.</summary>
    public bool JustSaved { get; private set; }

    /// <summary>Gets the role whose picker is open, if any.</summary>
    public string? PickingFor { get; private set; }

    /// <summary>Gets or sets the Discord role chosen in the open picker.</summary>
    public string? PickedRoleId { get; set; }

    /// <summary>Gets a value indicating whether a change is being saved.</summary>
    public bool IsSaving { get; private set; }

    /// <summary>Gets the open role form, if any.</summary>
    public RoleForm? Form { get; private set; }

    /// <summary>Gets a value indicating whether the delete confirmation is open over the form.</summary>
    public bool ConfirmingDelete { get; private set; }

    /// <summary>Gets a value indicating whether the user may create roles.</summary>
    public bool CanCreate => Settings?.CanEdit == true;

    /// <summary>Gets a value indicating whether some roles are the Administrator's to change, which the page explains.</summary>
    public bool HasLockedRoles => Rows.Any(row => row.Locked);

    /// <summary>Gets the delete confirmation's title.</summary>
    public string DeleteTitle => $"Delete {Form?.OriginalName}?";

    /// <summary>Gets the delete confirmation's message: how many members lose the role.</summary>
    public string DeleteMessage
    {
        get
        {
            var members = Settings?.Rows.FirstOrDefault(row => row.RoleId == Form?.RoleId)?.Members ?? 0;
            return string.Create(CultureInfo.InvariantCulture, $"{members} member{(members == 1 ? string.Empty : "s")} lose what it allows.");
        }
    }
    #endregion Properties

    #region Public Methods
    /// <summary>Gives the Discord roles the picker of a row offers: the mappable roles not already on that row.</summary>
    /// <param name="role">The row's RaidManager role.</param>
    /// <returns>The roles, highest first.</returns>
    public IReadOnlyList<DiscordRoleOption> PickerOptions(string role)
    {
        var mapped = Settings?.Rows.FirstOrDefault(row => row.Key == role)?.DiscordRoles.Select(discordRole => discordRole.DiscordRoleId).ToHashSet(StringComparer.Ordinal) ?? [];
        return [.. (Settings?.MappableRoles ?? []).Where(option => !mapped.Contains(option.Id))];
    }

    /// <summary>Loads the roles card.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the card is loaded.</returns>
    public async Task LoadAsync(Guid userId, Guid communityId, CancellationToken cancellationToken)
    {
        _userId = userId;
        _communityId = communityId;
        Status = CommunityPageStatus.Loading;
        try
        {
            var answer = await _api.GetRoleSettingsAsync(userId, communityId, cancellationToken);
            Settings = answer.Settings;
            Rows = answer.Settings is { } settings ? [.. settings.Rows.Select(View)] : [];
            Problem = answer.Status == CommunityApiStatus.Succeeded ? null : NoticeFor(answer.Status, "The roles couldn't be shown");
            Status = answer.Status == CommunityApiStatus.Succeeded ? CommunityPageStatus.Ready : CommunityPageStatus.Failed;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Problem = NoticeFor(CommunityApiStatus.DiscordUnavailable, "The roles couldn't be shown");
            Status = CommunityPageStatus.Failed;
        }
    }

    /// <summary>Opens the picker of a row.</summary>
    /// <param name="role">The row's RaidManager role.</param>
    public void OpenPicker(string role)
    {
        PickingFor = role;
        var options = PickerOptions(role);
        PickedRoleId = options.Count > 0 ? options[0].Id : null;
        JustSaved = false;
    }

    /// <summary>Forgets the last change's confirmation once the page's notification closes.</summary>
    public void ForgetSaved() => JustSaved = false;

    /// <summary>Closes the picker without changing anything.</summary>
    public void ClosePicker()
    {
        PickingFor = null;
        PickedRoleId = null;
    }

    /// <summary>Maps the picked Discord role to the picker's RaidManager role, then reloads the card.</summary>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    public Task AddPickedAsync(CancellationToken cancellationToken) =>
        PickingFor is { } role && PickedRoleId is { } discordRoleId
            ? ChangeAsync(() => _api.MapRoleAsync(_userId, _communityId, discordRoleId, Guid.Parse(role), cancellationToken), cancellationToken)
            : Task.CompletedTask;

    /// <summary>Stops a Discord role giving a row's RaidManager role, then reloads the card; the role stays on other rows.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The row's RaidManager role.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    public Task RemoveAsync(string discordRoleId, string role, CancellationToken cancellationToken) =>
        ChangeAsync(() => _api.UnmapRoleAsync(_userId, _communityId, discordRoleId, Guid.Parse(role), cancellationToken), cancellationToken);

    /// <summary>Opens the form for a new role.</summary>
    public void OpenCreate()
    {
        Form = new RoleForm(null, string.Empty, [], Settings?.CanGrantRoleManagement == true);
        JustSaved = false;
    }

    /// <summary>Opens the form for one of the community's roles.</summary>
    /// <param name="role">The row's key.</param>
    public void OpenEdit(string role)
    {
        if (Settings?.Rows.FirstOrDefault(row => row.Key == role) is { RoleId: { } roleId } row)
        {
            Form = new RoleForm(roleId, row.Name, row.Permissions, Settings.CanGrantRoleManagement);
            JustSaved = false;
        }
    }

    /// <summary>Closes the form without saving.</summary>
    public void CloseForm()
    {
        Form = null;
        ConfirmingDelete = false;
    }

    /// <summary>Saves the form: creates the role or changes it, then reloads the card.</summary>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the save was tried.</returns>
    public Task SaveFormAsync(CancellationToken cancellationToken)
    {
        if (Form is not { CanSave: true } form)
        {
            return Task.CompletedTask;
        }

        var name = form.Name.Trim();
        var permissions = form.Permissions.ToList();
        return SubmitFormAsync(
            () => form.RoleId is { } roleId
                ? _api.UpdateRoleAsync(_userId, _communityId, roleId, name, permissions, cancellationToken)
                : _api.CreateRoleAsync(_userId, _communityId, name, permissions, cancellationToken),
            "The role wasn't saved",
            cancellationToken);
    }

    /// <summary>Asks to confirm deleting the role being edited.</summary>
    public void AskDelete() => ConfirmingDelete = Form?.IsEditing == true;

    /// <summary>Closes the delete confirmation, back to the form.</summary>
    public void CancelDelete() => ConfirmingDelete = false;

    /// <summary>Deletes the role being edited, then reloads the card.</summary>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the deletion was tried.</returns>
    public Task DeleteAsync(CancellationToken cancellationToken)
    {
        ConfirmingDelete = false;
        return Form?.RoleId is { } roleId
            ? SubmitFormAsync(() => _api.DeleteRoleAsync(_userId, _communityId, roleId, cancellationToken), "The role wasn't deleted", cancellationToken)
            : Task.CompletedTask;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Says what a role allows, with the card's short words.</summary>
    /// <param name="permissions">The permissions, by API name.</param>
    /// <returns>For example "Allows raids, rosters"; a role allowing nothing says so.</returns>
    private static string AllowsLine(IReadOnlyCollection<string> permissions)
    {
        var words = RolePermissionOption.All.Where(option => permissions.Contains(option.Name)).Select(option => option.Word).ToList();
        return words.Count == 0 ? "Allows nothing more than a Member" : $"Allows {string.Join(", ", words)}";
    }

    /// <summary>Tells whether an exception means the API couldn't answer, rather than the page being closed.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns><see langword="true"/> for a failed or timed-out call.</returns>
    private static bool IsApiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or System.Text.Json.JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>Words why a read or a change didn't happen.</summary>
    /// <param name="status">How the API answered.</param>
    /// <param name="title">The notice title.</param>
    /// <returns>The notice.</returns>
    private static LinkNotice NoticeFor(CommunityApiStatus status, string title) => status switch
    {
        CommunityApiStatus.Refused => new LinkNotice(title, "Only the Administrator, or a member whose role manages roles, can change them."),
        CommunityApiStatus.BotRemoved => new LinkNotice(title, "The RaidManager bot isn't in this Discord server any more. Add it again."),
        _ => new LinkNotice(title, "Discord didn't answer. Nothing changed; try again in a minute."),
    };

    /// <summary>Builds a row's view.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The view.</returns>
    private RoleRowView View(CommunityRoleRow row)
    {
        var source = row.Kind switch
        {
            "Administrator" => "Added RaidManager to the server; allows everything",
            "Member" => "Everyone in the Discord server; allows signing up",
            _ => null,
        };
        var isRole = row.Kind == CommunityRoleRow.RoleKind;
        return new RoleRowView(
            row.Key,
            row.Name,
            source,
            [.. row.DiscordRoles.Select(role => new RoleChipView(role.DiscordRoleId, role.Missing ? "Deleted role" : $"@{role.Name}", role.Missing))],
            string.Create(CultureInfo.InvariantCulture, $"{row.Members} member{(row.Members == 1 ? string.Empty : "s")}"),
            row.CanChange,
            isRole ? AllowsLine(row.Permissions) : null,
            isRole && !row.CanChange && Settings?.CanEdit == true);
    }

    /// <summary>Sends the form's change, closes the form and reloads the card when it was saved, or says in the form why not.</summary>
    /// <param name="change">The API call.</param>
    /// <param name="failedTitle">The notice title when it wasn't saved.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    private async Task SubmitFormAsync(Func<Task<CommunityApiStatus>> change, string failedTitle, CancellationToken cancellationToken)
    {
        IsSaving = true;
        JustSaved = false;
        try
        {
            var status = await change();
            if (status != CommunityApiStatus.Succeeded)
            {
                Form!.Problem = status == CommunityApiStatus.NameTaken ? "Another role already has this name." : NoticeFor(status, failedTitle).Message;
                return;
            }

            CloseForm();
            await LoadAsync(_userId, _communityId, cancellationToken);
            JustSaved = Status == CommunityPageStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Form!.Problem = NoticeFor(CommunityApiStatus.DiscordUnavailable, failedTitle).Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Sends a change, reloads the card when it was saved, and words the outcome.</summary>
    /// <param name="change">The API call.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    private async Task ChangeAsync(Func<Task<CommunityApiStatus>> change, CancellationToken cancellationToken)
    {
        IsSaving = true;
        JustSaved = false;
        try
        {
            var status = await change();
            if (status != CommunityApiStatus.Succeeded)
            {
                Problem = NoticeFor(status, "The roles weren't changed");
                return;
            }

            ClosePicker();
            await LoadAsync(_userId, _communityId, cancellationToken);
            JustSaved = Status == CommunityPageStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Problem = NoticeFor(CommunityApiStatus.DiscordUnavailable, "The roles weren't changed");
        }
        finally
        {
            IsSaving = false;
        }
    }
    #endregion Private Helpers
}
