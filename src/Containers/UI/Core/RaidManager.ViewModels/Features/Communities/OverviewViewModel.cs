namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the Overview's state for a signed-in user: their community, or the steps to link one (board 1).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the Overview's loading and wording testable without a browser; the page only lays it out.
/// </remarks>
public sealed class OverviewViewModel
{
    #region Constants
    /// <summary>Defines the label of the button that adds RaidManager to a Discord server.</summary>
    public const string AddBotLabel = "Add RaidManager to a Discord server";

    /// <summary>Defines the note for members of a server that is already linked.</summary>
    public const string MemberNote = "Is your server already linked? Its community appears here once you're a member of the server.";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="OverviewViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    public OverviewViewModel(ICommunitiesApiClient api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the steps of linking a community, as board 1 lists them.</summary>
    public static IReadOnlyList<(string Title, string Detail)> Steps { get; } =
    [
        ("Add RaidManager to your Discord server", "Discord asks which server. You need the Manage Server permission there."),
        ("Choose your Warmane realm", "Characters, raids and lockouts are checked against this realm."),
        ("Map your officer roles", "Pick the Discord roles that make someone an Officer or a Raid leader."),
    ];

    /// <summary>Gets the page state.</summary>
    public CommunityPageStatus Status { get; private set; } = CommunityPageStatus.Loading;

    /// <summary>Gets the user's community, or <see langword="null"/> when they have none.</summary>
    public CommunitySummary? Community { get; private set; }

    /// <summary>Gets the notice explaining why adding the bot didn't finish, if the user just came back from it.</summary>
    public LinkNotice? Failure { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Explains why adding RaidManager to a server didn't finish.</summary>
    /// <param name="failure">The reason, as the return address names it; <see langword="null"/> for none.</param>
    /// <returns>The notice, or <see langword="null"/> when the name is empty or unknown.</returns>
    public static LinkNotice? NoticeFor(string? failure) =>
        Enum.TryParse<CommunityLinkFailure>(failure, ignoreCase: true, out var reason) && !int.TryParse(failure, out _)
            ? reason switch
            {
                CommunityLinkFailure.Cancelled => new LinkNotice("RaidManager wasn't added", "You cancelled on Discord's page. Nothing was linked."),
                CommunityLinkFailure.Expired => new LinkNotice("That took too long", "Adding RaidManager started too long ago or in another window. Start again."),
                CommunityLinkFailure.OtherAccount => new LinkNotice(
                    "Another Discord account added the bot",
                    "Add RaidManager with the Discord account you signed in with. Nothing was linked."),
                _ => new LinkNotice("RaidManager couldn't finish adding the bot", "Discord didn't confirm the server. Nothing was linked; try again."),
            }
            : null;

    /// <summary>Loads the user's community and the reason the last attempt stopped, if any.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="failure">The reason the return address names, if any.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the state is loaded.</returns>
    public async Task LoadAsync(Guid userId, string? failure, CancellationToken cancellationToken)
    {
        Failure = NoticeFor(failure);
        Status = CommunityPageStatus.Loading;
        try
        {
            var communities = await _api.GetUserCommunitiesAsync(userId, cancellationToken);
            Community = communities.Count > 0 ? communities[0] : null;
            Status = CommunityPageStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Status = CommunityPageStatus.Failed;
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Tells whether an exception means the API couldn't answer, rather than the page being closed.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns><see langword="true"/> for a failed or timed-out call.</returns>
    private static bool IsApiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or System.Text.Json.JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    #endregion Private Helpers
}
