namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Checks the player's claims while the website is open and tells which characters a sync brought.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: The website learns about a sync by asking the claims API every <see cref="CheckInterval"/> while a page is
/// open, without a push connection (owner decision on #595). The first check only remembers the claims already
/// waiting, so a sign-in shows no notification (#18 opens the review page then); later checks report the pending
/// claims not seen before, once each. A failed check reports nothing, and the next one tries again.
/// </remarks>
public sealed class CharacterArrivalsViewModel
{
    #region Fields
    /// <summary>Gets how often the website asks for new claims while a page is open.</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    /// <summary>Stores the claims API client.</summary>
    private readonly ICharacterClaimsApiClient _api;

    /// <summary>Stores the characters whose pending claims were already seen.</summary>
    private readonly HashSet<Guid> _seen = [];

    /// <summary>Stores whether the claims waiting at the first check are known.</summary>
    private bool _started;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterArrivalsViewModel"/> class.</summary>
    /// <param name="api">The claims API client.</param>
    public CharacterArrivalsViewModel(ICharacterClaimsApiClient api) => _api = api;
    #endregion Constructors

    #region Events
    /// <summary>Occurs when a check finds characters a sync brought, so an open review page shows them.</summary>
    public event EventHandler<CharacterArrival>? Arrived;
    #endregion Events

    #region Public Methods
    /// <summary>Asks the API for the player's claims and reports the pending ones not seen before.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="cancellationToken">A token tied to the page's lifetime.</param>
    /// <returns>The characters that arrived, or <see langword="null"/> when none did, at the first check, or when
    /// the check failed.</returns>
    public async Task<CharacterArrival?> CheckAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<CharacterClaim> claims;
        try
        {
            claims = await _api.GetPendingAsync(userId, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return null;
        }

        var arrived = claims.Where(claim => claim.IsPending && _seen.Add(claim.CharacterId)).Select(claim => claim.Name).ToList();
        if (!_started)
        {
            _started = true;
            return null;
        }

        if (arrived.Count == 0)
        {
            return null;
        }

        var arrival = new CharacterArrival(arrived);
        Arrived?.Invoke(this, arrival);
        return arrival;
    }
    #endregion Public Methods
}
