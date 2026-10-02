using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Features.Shared.Shell;

/// <summary>Holds the community the app shell shows in its sidebar card, and the user's role in it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives the shell the signed-in user's community once per circuit. A failed call shows no community rather than
/// breaking every page; the pages that need the community report the failure themselves.
/// </remarks>
public sealed class ShellCommunityViewModel
{
    #region Constants
    /// <summary>Defines the role label of a community's Administrator.</summary>
    public const string AdministratorLabel = "Administrator";
    #endregion Constants

    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;

    /// <summary>Stores the signed-in user.</summary>
    private Guid _userId;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ShellCommunityViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    public ShellCommunityViewModel(ICommunitiesApiClient api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the user's community, or <see langword="null"/> when they have none.</summary>
    public CommunitySummary? Community { get; private set; }

    /// <summary>Gets the card's title: the community name, or "No community yet".</summary>
    public string CardTitle => Community?.Name ?? ShellViewModel.NoCommunityName;

    /// <summary>Gets the card's second line: the realm, or the hint to link a server.</summary>
    public string CardSubtitle => Community is null ? ShellViewModel.NoCommunityHint : $"{Community.Realm} · Community";

    /// <summary>Gets the card's glyph: the community's initials, or "+".</summary>
    public string CardGlyph => Community is null ? "+" : ShellViewModel.Initials(Community.Name);

    /// <summary>Gets the user's role label in the shell.</summary>
    public string RoleLabel => Community is not null && Community.AdministratorId == _userId
        ? AdministratorLabel
        : ShellViewModel.RoleLabel(isOfficer: false);
    #endregion Properties

    #region Public Methods
    /// <summary>Loads the signed-in user's community.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="memberOf">The communities the user's Discord servers matched at sign-in.</param>
    /// <param name="cancellationToken">The shell's token.</param>
    /// <returns>A task that completes when the community is loaded.</returns>
    public async Task LoadAsync(Guid userId, IReadOnlyCollection<Guid> memberOf, CancellationToken cancellationToken)
    {
        _userId = userId;
        try
        {
            var communities = await _api.GetUserCommunitiesAsync(userId, memberOf, cancellationToken);
            Community = communities.Count > 0 ? communities[0] : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Community = null;
        }
    }
    #endregion Public Methods
}
