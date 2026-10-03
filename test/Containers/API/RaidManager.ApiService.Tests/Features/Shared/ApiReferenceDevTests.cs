using System.Net;
using Shouldly;
using Xunit;
using RaidManager.ApiService.Tests.Support;

namespace RaidManager.ApiService.Tests.Features.Shared;

/// <summary>Verifies that the deployed dev environment keeps the developer-only API reference off.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: The deployed environment named <c>Dev</c> isn't Development, so it serves no Scalar page (ADR-0013), as
/// <c>Test</c> and <c>Production</c> don't.
/// </remarks>
public sealed class ApiReferenceDevTests : IClassFixture<DevApiFactory>
{
    #region Fields
    /// <summary>Stores the API factory running as Dev.</summary>
    private readonly DevApiFactory _api;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceDevTests"/> class.</summary>
    /// <param name="api">The API factory running as Dev.</param>
    public ApiReferenceDevTests(DevApiFactory api)
    {
        _api = api;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Requests the Scalar page in the deployed dev environment.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ReferencePageIsNotServedInDev()
    {
        using var client = _api.CreateClient();

        var response = await client.GetAsync("/scalar");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    #endregion Tests
}
