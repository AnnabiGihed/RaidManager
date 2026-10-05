using System.Net;
using System.Text;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Features.Characters;

namespace RaidManager.Web.Tests.Features.Characters;

/// <summary>Verifies how the website calls the character profile API and reads its answers.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Checks the routes, the transport records, a 404 read as a character that isn't the player's, and failures.
/// </remarks>
public sealed class CharacterProfilesApiClientTests
{
    #region Fields
    /// <summary>Stores the player.</summary>
    private static readonly Guid UserId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000001");

    /// <summary>Stores the character.</summary>
    private static readonly Guid CharacterId = Guid.Parse("6d1f7a9e-0000-4000-8000-000000000002");
    #endregion Fields

    #region Tests
    /// <summary>Lists the characters from the API's JSON.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task CharactersAreReadFromTheApi()
    {
        var json = $$"""
            [{"characterId":"{{CharacterId}}","realm":"Icecrown","name":"Arthasdk","class":"DeathKnight","level":80,"primaryLoadout":{"name":"Frost DPS","role":"MeleeDamage","gearScore":5712},"currentRaidSaveCount":2,"lastSynchronizedAtUtc":"2026-10-05T14:05:00+00:00"}]
            """;
        var handler = new StubHandler(HttpStatusCode.OK, json);

        var characters = await Client(handler).GetCharactersAsync(UserId, CancellationToken.None);

        handler.Requests.ShouldBe([$"GET /internal/users/{UserId}/characters"]);
        var expected = new CharacterSummary(
            CharacterId,
            "Icecrown",
            "Arthasdk",
            "DeathKnight",
            80,
            new CharacterLoadoutSummary("Frost DPS", "MeleeDamage", 5712),
            2,
            new DateTimeOffset(2026, 10, 5, 14, 5, 0, TimeSpan.Zero));
        characters.ShouldBe([expected]);
    }

    /// <summary>Reads a profile from the API's JSON.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ProfileIsReadFromTheApi()
    {
        var json = $$"""
            {"characterId":"{{CharacterId}}","realm":"Icecrown","name":"Arthasdk","class":"DeathKnight","race":"Human","faction":"Alliance","level":80,"guildName":null,"visibility":"Community","addonSynchronizedAtUtc":null,"armorySynchronizedAtUtc":null,"completeRaidSaveScanAtUtc":null,
             "professions":[{"name":"Mining","rank":450,"maxRank":450}],
             "loadouts":[{"name":"Frost DPS","role":"MeleeDamage","isPrimary":true,"gearScore":5712,"talents":"0/53/18","source":"WowAddon","gear":[{"slot":"MainHand","itemId":50737,"itemLink":"|h[Havoc's Call]|h","itemLevel":264}]}],
             "raidSaves":[{"instance":"IcecrownCitadel","difficulty":"TwentyFivePlayer","lockoutId":null,"resetsAtUtc":"2026-10-07T04:00:00+00:00","isExtended":false}]}
            """;
        var handler = new StubHandler(HttpStatusCode.OK, json);

        var profile = (await Client(handler).GetProfileAsync(UserId, CharacterId, CancellationToken.None)).ShouldNotBeNull();

        handler.Requests.ShouldBe([$"GET /internal/users/{UserId}/characters/{CharacterId}"]);
        profile.Name.ShouldBe("Arthasdk");
        profile.Professions.ShouldBe([new CharacterProfession("Mining", 450, 450)]);
        profile.Loadouts.ShouldHaveSingleItem().Gear.ShouldHaveSingleItem().ItemLevel.ShouldBe(264);
        profile.RaidSaves.ShouldHaveSingleItem().Instance.ShouldBe("IcecrownCitadel");
    }

    /// <summary>Reads 404 as a character that isn't the player's.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task NotFoundIsNoProfile() =>
        (await Client(new StubHandler(HttpStatusCode.NotFound, "{}")).GetProfileAsync(UserId, CharacterId, CancellationToken.None)).ShouldBeNull();

    /// <summary>Fails on a server error and on empty bodies.</summary>
    /// <param name="status">The API status.</param>
    /// <param name="body">The body.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.OK, "null")]
    public async Task FailuresThrow(HttpStatusCode status, string body)
    {
        var client = Client(new StubHandler(status, body));

        await Should.ThrowAsync<HttpRequestException>(() => client.GetProfileAsync(UserId, CharacterId, CancellationToken.None));
        await Should.ThrowAsync<HttpRequestException>(() => client.GetCharactersAsync(UserId, CancellationToken.None));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a client addressed to a stubbed API.</summary>
    /// <param name="handler">The stub.</param>
    /// <returns>The client.</returns>
    private static CharacterProfilesApiClient Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Answers every request with one status and body, and records the requests.</summary>
    /// <param name="status">The status to answer.</param>
    /// <param name="json">The JSON body to answer.</param>
    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        /// <summary>Gets the requests received, as method and path.</summary>
        public List<string> Requests { get; } = [];

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri?.AbsolutePath}");
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
    #endregion Nested Types
}
