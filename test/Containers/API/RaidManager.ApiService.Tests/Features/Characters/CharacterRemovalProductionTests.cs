using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Characters;

/// <summary>Verifies that production never offers the removal of a player's characters.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Story #597's second criterion: in production the API doesn't accept the removal, even from the website.
/// </remarks>
public sealed class CharacterRemovalProductionTests : IClassFixture<ProductionApiFactory>
{
    #region Fields
    /// <summary>Stores the API on Production.</summary>
    private readonly ProductionApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterRemovalProductionTests"/> class.</summary>
    /// <param name="api">The API on Production.</param>
    public CharacterRemovalProductionTests(ProductionApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>The removal isn't mapped, so even the website's request is refused and the API reference omits it.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RemovalIsNotAcceptedInProduction()
    {
        using var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        var route = CharacterProfileEndpoints.CharactersRoute.Replace("{userId:guid}", Guid.NewGuid().ToString(), StringComparison.Ordinal);

        var response = await client.DeleteAsync(route);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }
    #endregion Tests
}
