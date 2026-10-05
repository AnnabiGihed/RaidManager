using Shouldly;
using Xunit;
using RaidManager.Web.Features.Characters;

namespace RaidManager.Web.Tests.Features.Characters;

/// <summary>Verifies where the review page continues to.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The return URL stays local and never points back at the review page.
/// </remarks>
public sealed class CharacterRoutesTests
{
    #region Tests
    /// <summary>Chooses the page to continue to.</summary>
    /// <param name="returnUrl">The requested return URL.</param>
    /// <param name="expected">The expected page.</param>
    [Theory]
    [InlineData("/raids?week=40", "/raids?week=40")]
    [InlineData(null, "/")]
    [InlineData("https://evil.example/steal", "/")]
    [InlineData("/characters/review?returnUrl=%2F", "/")]
    public void ContinueUrlStaysLocalAndLeavesTheReviewPage(string? returnUrl, string expected) =>
        CharacterRoutes.ContinueUrl(returnUrl).ShouldBe(expected);

    /// <summary>Builds the review page URL.</summary>
    [Fact]
    public void ReviewForCarriesTheReturnUrl() =>
        CharacterRoutes.ReviewFor("/raids?week=40").ShouldBe("/characters/review?returnUrl=%2Fraids%3Fweek%3D40");

    /// <summary>Builds a profile URL under the My characters page.</summary>
    [Fact]
    public void ProfileSitsUnderMyCharacters() =>
        CharacterRoutes.Profile(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")).ShouldBe("/characters/0f8fad5b-d9cb-469f-a165-70867728950e");
    #endregion Tests
}
