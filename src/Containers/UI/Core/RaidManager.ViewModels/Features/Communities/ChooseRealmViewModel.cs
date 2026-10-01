using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the realm choice for a server the bot was just added to (board 2).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the realm page's state, wording and linking testable without a browser.
/// </remarks>
public sealed class ChooseRealmViewModel
{
    #region Fields
    /// <summary>Stores the API client.</summary>
    private readonly ICommunitiesApiClient _api;

    /// <summary>Stores the server waiting to be linked.</summary>
    private PendingCommunityLink? _link;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ChooseRealmViewModel"/> class.</summary>
    /// <param name="api">The API client.</param>
    public ChooseRealmViewModel(ICommunitiesApiClient api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the Warmane realms, in RaidManager's order.</summary>
    public static IReadOnlyList<string> Realms { get; } = Enum.GetNames<WarmaneRealm>();

    /// <summary>Gets the page state: ready, or missing when the pending link is invalid or expired.</summary>
    public CommunityPageStatus Status { get; private set; } = CommunityPageStatus.Loading;

    /// <summary>Gets the server name.</summary>
    public string ServerName => _link?.ServerName ?? string.Empty;

    /// <summary>Gets the page title.</summary>
    public string Title => $"Set up {ServerName}";

    /// <summary>Gets the line that says who becomes Administrator.</summary>
    public string AddedBy { get; private set; } = string.Empty;

    /// <summary>Gets or sets the chosen realm; nothing is chosen at first.</summary>
    public string? SelectedRealm { get; set; }

    /// <summary>Gets a value indicating whether the link is being saved.</summary>
    public bool IsSaving { get; private set; }

    /// <summary>Gets a value indicating whether Finish linking can be pressed.</summary>
    public bool CanFinish => Status == CommunityPageStatus.Ready && SelectedRealm is not null && !IsSaving;

    /// <summary>Gets the notice shown when linking failed, if any.</summary>
    public LinkNotice? Failure { get; private set; }

    /// <summary>Gets the community the server links to once finishing succeeded or found it already linked.</summary>
    public Guid? CommunityId { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Starts the choice for a pending link.</summary>
    /// <param name="link">The pending link, or <see langword="null"/> when it was invalid or expired.</param>
    /// <param name="userName">The signed-in user's display name.</param>
    public void Initialize(PendingCommunityLink? link, string? userName)
    {
        _link = link;
        AddedBy = $"Added by {userName ?? "you"}, who becomes its Administrator";
        Status = link is null ? CommunityPageStatus.Missing : CommunityPageStatus.Ready;
    }

    /// <summary>Links the server with the chosen realm.</summary>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns>How it ended; <see cref="CommunityId"/> names the community when it didn't fail.</returns>
    public async Task<ChooseRealmOutcome> FinishAsync(CancellationToken cancellationToken)
    {
        if (!CanFinish || _link is null || SelectedRealm is null)
        {
            return ChooseRealmOutcome.Failed;
        }

        IsSaving = true;
        Failure = null;
        try
        {
            CommunityId = await _api.LinkAsync(_link, SelectedRealm, cancellationToken);
            if (CommunityId is not null)
            {
                return ChooseRealmOutcome.Linked;
            }

            CommunityId = (await _api.FindByDiscordServerAsync(_link.DiscordGuildId, cancellationToken))?.CommunityId;
            return CommunityId is null ? Fail() : ChooseRealmOutcome.AlreadyLinked;
        }
        catch (Exception exception) when (IsApiFailure(exception, cancellationToken))
        {
            return Fail();
        }
        finally
        {
            IsSaving = false;
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

    /// <summary>Records that linking failed and nothing changed.</summary>
    /// <returns><see cref="ChooseRealmOutcome.Failed"/>.</returns>
    private ChooseRealmOutcome Fail()
    {
        Failure = new LinkNotice("The community wasn't linked", "RaidManager couldn't save it. Nothing changed; try again in a moment.");
        return ChooseRealmOutcome.Failed;
    }
    #endregion Private Helpers
}
