using System.Globalization;

namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the roles card of the community page: its rows, the role picker, and the outcome of each change (boards 4 and 8).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the roles card's loading, wording and changes testable without a browser; the page only lays it out.
/// </remarks>
public sealed class CommunityRolesViewModel
{
    #region Constants
    /// <summary>Defines the confirmation title after a change.</summary>
    public const string SavedTitle = "Officer roles saved";

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
    #endregion Properties

    #region Public Methods
    /// <summary>Gives the Discord roles the picker of a row offers: the mappable roles not already on that row.</summary>
    /// <param name="role">The row's RaidManager role.</param>
    /// <returns>The roles, highest first.</returns>
    public IReadOnlyList<DiscordRoleOption> PickerOptions(string role)
    {
        var mapped = Settings?.Rows.FirstOrDefault(row => row.Role == role)?.DiscordRoles.Select(discordRole => discordRole.DiscordRoleId).ToHashSet(StringComparer.Ordinal) ?? [];
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
            Rows = answer.Settings is { } settings ? [.. settings.Rows.Select(row => View(row, settings.CanEdit))] : [];
            Problem = answer.Status == CommunityApiStatus.Succeeded ? null : NoticeFor(answer.Status, "The officer roles couldn't be shown");
            Status = answer.Status == CommunityApiStatus.Succeeded ? CommunityPageStatus.Ready : CommunityPageStatus.Failed;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Problem = NoticeFor(CommunityApiStatus.DiscordUnavailable, "The officer roles couldn't be shown");
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
            ? ChangeAsync(() => _api.MapRoleAsync(_userId, _communityId, discordRoleId, role, cancellationToken), cancellationToken)
            : Task.CompletedTask;

    /// <summary>Stops a Discord role giving a row's RaidManager role, then reloads the card; the role stays on other rows.</summary>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="role">The row's RaidManager role.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the change was tried.</returns>
    public Task RemoveAsync(string discordRoleId, string role, CancellationToken cancellationToken) =>
        ChangeAsync(() => _api.UnmapRoleAsync(_userId, _communityId, discordRoleId, role, cancellationToken), cancellationToken);
    #endregion Public Methods

    #region Private Helpers
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
        CommunityApiStatus.Refused => new LinkNotice(title, "Only the community's Administrator can change them, with a role the server has."),
        CommunityApiStatus.BotRemoved => new LinkNotice(title, "The RaidManager bot isn't in this Discord server any more. Add it again."),
        _ => new LinkNotice(title, "Discord didn't answer. Nothing changed; try again in a minute."),
    };

    /// <summary>Builds a row's view.</summary>
    /// <param name="row">The row.</param>
    /// <param name="canEdit">Whether the user is the Administrator.</param>
    /// <returns>The view.</returns>
    private static RoleRowView View(CommunityRoleRow row, bool canEdit)
    {
        var source = row.Role switch
        {
            "Administrator" => "Added RaidManager to the server",
            "Member" => "Everyone in the Discord server",
            _ => null,
        };
        return new RoleRowView(
            row.Role,
            CommunityRoleLabels.For(row.Role),
            source,
            [.. row.DiscordRoles.Select(role => new RoleChipView(role.DiscordRoleId, role.Missing ? "Deleted role" : $"@{role.Name}", role.Missing))],
            string.Create(CultureInfo.InvariantCulture, $"{row.Members} member{(row.Members == 1 ? string.Empty : "s")}"),
            canEdit && source is null);
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
                Problem = NoticeFor(status, "The officer roles weren't changed");
                return;
            }

            ClosePicker();
            await LoadAsync(_userId, _communityId, cancellationToken);
            JustSaved = Status == CommunityPageStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Problem = NoticeFor(CommunityApiStatus.DiscordUnavailable, "The officer roles weren't changed");
        }
        finally
        {
            IsSaving = false;
        }
    }
    #endregion Private Helpers
}
