using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Tests.Support;
using RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

namespace RaidManager.ApiService.Tests.Features.Characters;

/// <summary>Verifies that the dev environment, as deployed, offers the removal of a player's characters.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: deploy-dev.yml names the environment <c>Dev</c>, not Development; the owner checks story #597 there. The
/// test database of this factory isn't migrated, so the test stops at the validation, before any query.
/// </remarks>
public sealed class CharacterRemovalDevTests : IClassFixture<DevApiFactory>
{
    #region Fields
    /// <summary>Stores the API on Dev.</summary>
    private readonly DevApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterRemovalDevTests"/> class.</summary>
    /// <param name="api">The API on Dev.</param>
    public CharacterRemovalDevTests(DevApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>The removal is mapped on Dev: an empty player reaches its validation, where production refuses the method.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RemovalIsOfferedOnDev()
    {
        using var client = _api.CreateClient();
        client.DefaultRequestHeaders.Add(WebsiteServiceDefaults.HeaderName, ApiFactory.WebsiteServiceKey);
        var route = CharacterProfileEndpoints.CharactersRoute.Replace("{userId:guid}", Guid.Empty.ToString(), StringComparison.Ordinal);

        var response = await client.DeleteAsync(route);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()).ShouldNotBeNull()
            .Errors.Keys.ShouldContain(nameof(RemoveMyCharactersCommand.UserId));
    }
    #endregion Tests
}
