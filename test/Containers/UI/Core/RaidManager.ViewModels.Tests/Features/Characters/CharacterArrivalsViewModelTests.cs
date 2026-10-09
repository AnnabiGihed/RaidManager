using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Verifies the check that tells which characters a sync brought, and its wording.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Covers story #595's check: nothing at the first check, each new pending claim once, conflicts left out,
/// failures quiet, and the notifications' titles and lines for one or many characters.
/// </remarks>
public sealed class CharacterArrivalsViewModelTests
{
    #region Fields
    /// <summary>Stores the signed-in player.</summary>
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>Stores the fake claims API.</summary>
    private readonly FakeCharacterClaimsApi _api = new();

    /// <summary>Stores the check under test.</summary>
    private readonly CharacterArrivalsViewModel _arrivals;

    /// <summary>Stores the arrivals the check announced.</summary>
    private readonly List<CharacterArrival> _announced = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterArrivalsViewModelTests"/> class.</summary>
    public CharacterArrivalsViewModelTests()
    {
        _arrivals = new CharacterArrivalsViewModel(_api);
        _arrivals.Arrived += (_, arrival) => _announced.Add(arrival);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Gets the failures a check stays quiet about: the API down, or its call timing out.</summary>
    /// <returns>The failures.</returns>
    public static TheoryData<Exception> Failures() => [new HttpRequestException("The API is unavailable."), new TaskCanceledException()];
    #endregion Public Methods

    #region Tests
    /// <summary>The first check only remembers the claims already waiting, as a sign-in opens the review page (#18).</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task FirstCheckReportsNothing()
    {
        _api.Claims = [Claim("Arthasdk")];

        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();

        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();
        _announced.ShouldBeEmpty();
    }

    /// <summary>A sync's new pending claims are reported once, in the API's order, and announced.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NewPendingClaimsArriveOnce()
    {
        _api.Claims = [Claim("Arthasdk")];
        await _arrivals.CheckAsync(UserId, CancellationToken.None);
        _api.Claims = [.. _api.Claims, Claim("Uthertank"), Claim("Valeerarog"), Claim("Sylvanash", CharacterClaim.ConflictState)];

        var arrival = await _arrivals.CheckAsync(UserId, CancellationToken.None);

        arrival.ShouldNotBeNull().Names.ShouldBe(["Uthertank", "Valeerarog"]);
        _announced.ShouldBe([arrival]);
        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>A claim decided and left the list doesn't arrive again.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ADecidedClaimDoesNotArriveAgain()
    {
        var arthasdk = Claim("Arthasdk");
        _api.Claims = [arthasdk];
        await _arrivals.CheckAsync(UserId, CancellationToken.None);
        _api.Claims = [];
        await _arrivals.CheckAsync(UserId, CancellationToken.None);
        _api.Claims = [arthasdk];

        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>A failed check reports nothing, and the next one finds what arrived meanwhile.</summary>
    /// <param name="failure">The failure.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [MemberData(nameof(Failures))]
    public async Task AFailedCheckIsQuiet(Exception failure)
    {
        await _arrivals.CheckAsync(UserId, CancellationToken.None);
        _api.Claims = [Claim("Uthertank")];
        _api.LoadFailure = failure;

        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();

        _api.LoadFailure = null;
        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldNotBeNull().Names.ShouldBe(["Uthertank"]);
    }

    /// <summary>A failed first check still only remembers: what was waiting then arrives at the next check.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AFailedFirstCheckKeepsWaiting()
    {
        _api.Claims = [Claim("Arthasdk")];
        _api.LoadFailure = new HttpRequestException("The API is unavailable.");
        await _arrivals.CheckAsync(UserId, CancellationToken.None);
        _api.LoadFailure = null;

        (await _arrivals.CheckAsync(UserId, CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>Leaving the page stops the check instead of reporting a failure.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task LeavingStopsTheCheck()
    {
        using var left = new CancellationTokenSource();
        await left.CancelAsync();
        _api.LoadFailure = new TaskCanceledException();

        await Should.ThrowAsync<TaskCanceledException>(() => _arrivals.CheckAsync(UserId, left.Token));
    }

    /// <summary>The notifications name one character, a few, or count the rest.</summary>
    /// <param name="names">The characters that arrived, comma-separated.</param>
    /// <param name="toReview">The line on another page.</param>
    /// <param name="arrived">The line on the review page.</param>
    [Theory]
    [InlineData("Uthertank", "Your companion found Uthertank.", "Uthertank was added to the list.")]
    [InlineData("Uthertank,Valeerarog", "Your companion found Uthertank and Valeerarog.", "Uthertank and Valeerarog were added to the list.")]
    [InlineData("A,B,C", "Your companion found A, B and C.", "A, B and C were added to the list.")]
    [InlineData("A,B,C,D,E", "Your companion found A, B, C and 2 more.", "A, B, C and 2 more were added to the list.")]
    public void NotificationsNameTheCharacters(string names, string toReview, string arrived)
    {
        var arrival = new CharacterArrival(names.Split(','));

        arrival.ToReviewMessage.ShouldBe(toReview);
        arrival.ArrivedMessage.ShouldBe(arrived);
    }

    /// <summary>The titles count the characters.</summary>
    [Fact]
    public void TitlesCountTheCharacters()
    {
        var one = new CharacterArrival(["Uthertank"]);
        var two = new CharacterArrival(["Uthertank", "Valeerarog"]);

        (one.ToReviewTitle, one.ArrivedTitle).ShouldBe(("1 new character to review", "1 new character arrived"));
        (two.ToReviewTitle, two.ArrivedTitle).ShouldBe(("2 new characters to review", "2 new characters arrived"));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a claim for a test.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="state">The claim state.</param>
    /// <returns>The claim.</returns>
    private static CharacterClaim Claim(string name, string state = CharacterClaim.PendingState) =>
        new(Guid.NewGuid(), "Icecrown", name, "Paladin", "Human", 80, state, DateTimeOffset.UnixEpoch);
    #endregion Private Helpers
}
