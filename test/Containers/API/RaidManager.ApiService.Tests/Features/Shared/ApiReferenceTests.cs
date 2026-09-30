using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Shared;

/// <summary>Verifies that the interactive API reference is served in Development.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Developers explore and try the API from the Scalar page (ADR-0013).
/// </remarks>
public sealed class ApiReferenceTests : IClassFixture<ApiFactory>
{
    #region Fields
    /// <summary>Stores the API factory.</summary>
    private readonly ApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceTests"/> class.</summary>
    /// <param name="api">The API factory.</param>
    public ApiReferenceTests(ApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Requests the Scalar page in Development.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReferencePageIsServedInDevelopment()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync("/scalar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("RaidManager API");
    }
    #endregion Tests
}
