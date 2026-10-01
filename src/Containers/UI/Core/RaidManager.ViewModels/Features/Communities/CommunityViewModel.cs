namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds one community for the already-linked page (board 3) and the community page (board 4).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the loading of a community and its wording testable without a browser.
/// </remarks>
public sealed class CommunityViewModel
{
    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CommunityViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    public CommunityViewModel(ICommunitiesApiClient api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the page state.</summary>
    public CommunityPageStatus Status { get; private set; } = CommunityPageStatus.Loading;

    /// <summary>Gets the community once loaded.</summary>
    public CommunitySummary? Community { get; private set; }

    /// <summary>Gets the community page's subtitle.</summary>
    public string LinkedOn => Community is null ? string.Empty : $"Discord server linked to RaidManager on {Community.Realm}.";

    /// <summary>Gets the already-linked page's title.</summary>
    public string AlreadyLinkedTitle => Community is null ? string.Empty : $"{Community.Name} is already linked";

    /// <summary>Gets the already-linked notice's line about the Administrator.</summary>
    public string AdministratorLine => Community is null
        ? string.Empty
        : $"Its Administrator, {Community.AdministratorName}, manages the realm and the officer roles.";

    /// <summary>Gets the confirmation's title shown right after linking.</summary>
    public string LinkedTitle => Community is null ? string.Empty : $"{Community.Name} is linked";
    #endregion Properties

    #region Public Methods
    /// <summary>Loads a community by its identifier.</summary>
    /// <param name="communityId">The community, or <see langword="null"/> when the address names none.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the state is loaded.</returns>
    public Task LoadAsync(Guid? communityId, CancellationToken cancellationToken) =>
        communityId is { } id && id != Guid.Empty
            ? LoadWithAsync(() => _api.GetAsync(id, cancellationToken), cancellationToken)
            : SetMissingAsync();

    /// <summary>Loads the signed-in user's community.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the state is loaded.</returns>
    public Task LoadForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        LoadWithAsync(
            async () => await _api.GetUserCommunitiesAsync(userId, cancellationToken) is { Count: > 0 } communities ? communities[0] : null,
            cancellationToken);
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Tells whether an exception means the API couldn't answer, rather than the page being closed.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns><see langword="true"/> for a failed or timed-out call.</returns>
    private static bool IsApiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or System.Text.Json.JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>Loads the community with a lookup, recording a missing one or a failed call.</summary>
    /// <param name="lookup">The lookup.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>A task that completes when the state is loaded.</returns>
    private async Task LoadWithAsync(Func<Task<CommunitySummary?>> lookup, CancellationToken cancellationToken)
    {
        Status = CommunityPageStatus.Loading;
        try
        {
            Community = await lookup();
            Status = Community is null ? CommunityPageStatus.Missing : CommunityPageStatus.Ready;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            Status = CommunityPageStatus.Failed;
        }
    }

    /// <summary>Records that the address names no community.</summary>
    /// <returns>A completed task.</returns>
    private Task SetMissingAsync()
    {
        Status = CommunityPageStatus.Missing;
        return Task.CompletedTask;
    }
    #endregion Private Helpers
}
