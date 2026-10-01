using System.Globalization;

namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the members page: each person in the Discord server with the RaidManager role they get (board 5).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the members page's loading and wording testable without a browser; the page only lays it out.
/// </remarks>
public sealed class CommunityMembersViewModel
{
    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;

    /// <summary>Stores the clock that tells whether Discord was asked today.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityMembersViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    /// <param name="timeProvider">The clock.</param>
    public CommunityMembersViewModel(ICommunitiesApiClient api, TimeProvider timeProvider)
    {
        _api = api;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the page state.</summary>
    public CommunityPageStatus Status { get; private set; } = CommunityPageStatus.Loading;

    /// <summary>Gets the members as the page shows them.</summary>
    public IReadOnlyList<MemberRowView> Rows { get; private set; } = [];

    /// <summary>Gets the note saying when Discord was asked, in UTC.</summary>
    public string CheckedNote { get; private set; } = string.Empty;

    /// <summary>Gets the notice explaining why the members couldn't be shown, if any.</summary>
    public LinkNotice? Problem { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Loads the members from Discord through the API.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the members are loaded.</returns>
    public async Task LoadAsync(Guid userId, Guid communityId, CancellationToken cancellationToken)
    {
        Status = CommunityPageStatus.Loading;
        try
        {
            var answer = await _api.GetMembersAsync(userId, communityId, cancellationToken);
            if (answer.Members is { } members && answer.Status == CommunityApiStatus.Succeeded)
            {
                Rows = [.. members.Members.Select(View)];
                CheckedNote = Note(members.CheckedAtUtc);
                Problem = null;
                Status = CommunityPageStatus.Ready;
                return;
            }

            Fail(answer.Status);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Fail(CommunityApiStatus.DiscordUnavailable);
        }
    }

    /// <summary>Shows the members as unavailable without asking, when the community itself couldn't be read.</summary>
    public void ShowUnavailable() => Fail(CommunityApiStatus.DiscordUnavailable);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds a member's row.</summary>
    /// <param name="member">The member.</param>
    /// <returns>The row.</returns>
    private static MemberRowView View(CommunityMember member) => new(
        member.DiscordUserId,
        member.DisplayName,
        member.AvatarUrl,
        member.DiscordRoles.Count == 0 ? "No roles" : string.Join(", ", member.DiscordRoles.Select(role => $"@{role.Name}")),
        member.Role,
        CommunityRoleLabels.For(member.Role));

    /// <summary>Words when Discord was asked: today's time, or the date for an older answer.</summary>
    /// <param name="checkedAtUtc">When Discord was asked.</param>
    /// <returns>The note.</returns>
    private string Note(DateTimeOffset checkedAtUtc)
    {
        var at = checkedAtUtc.ToUniversalTime();
        var day = at.Date == _timeProvider.GetUtcNow().UtcDateTime.Date ? "today" : $"on {at.ToString("d MMMM", CultureInfo.InvariantCulture)}";
        return $"Last checked with Discord {day} at {at.ToString("HH:mm", CultureInfo.InvariantCulture)} UTC.";
    }

    /// <summary>Records why the members couldn't be shown; the old list isn't kept.</summary>
    /// <param name="status">How the API answered.</param>
    private void Fail(CommunityApiStatus status)
    {
        Rows = [];
        Problem = status switch
        {
            CommunityApiStatus.Refused => new LinkNotice("You can't see these members", "Only people in the community's Discord server can see its members."),
            CommunityApiStatus.BotRemoved => new LinkNotice("The members couldn't be shown", "The RaidManager bot isn't in this Discord server any more. Add it again."),
            _ => new LinkNotice("The members couldn't be shown", "Discord didn't answer. Try again in a minute."),
        };
        Status = CommunityPageStatus.Failed;
    }
    #endregion Private Helpers
}
