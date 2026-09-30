using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Shared;

/// <summary>Verifies that the interactive API reference is not served outside Development.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The Scalar page is a developer tool; deployed environments publish only the OpenAPI document (ADR-0013).
/// </remarks>
public sealed class ApiReferenceProductionTests : IClassFixture<ProductionApiFactory>
{
    #region Fields
    /// <summary>Stores the API factory running as Production.</summary>
    private readonly ProductionApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceProductionTests"/> class.</summary>
    /// <param name="api">The API factory running as Production.</param>
    public ApiReferenceProductionTests(ProductionApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Requests the Scalar page in Production.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReferencePageIsNotServedInProduction()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync("/scalar");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    #endregion Tests
}
