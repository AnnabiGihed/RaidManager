using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Authentication;

namespace RaidManager.ViewModels.Tests.Features.Authentication;

/// <summary>Verifies the wording the failure page shows for each sign-in failure reason.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: A cancelled consent and a technical failure need different explanations; unknown reasons fall back safely.
/// </remarks>
public sealed class SignInFailedViewModelTests
{
    #region Tests
    /// <summary>Describes a declined Discord consent.</summary>
    [Fact]
    public void DeniedConsentExplainsThatDiscordDidNotShareTheAccount()
    {
        var viewModel = new SignInFailedViewModel();

        viewModel.Describe("denied");

        viewModel.Title.ShouldBe("Sign-in cancelled");
        viewModel.Message.ShouldContain("Authorize");
    }

    /// <summary>Describes any other failure as temporary.</summary>
    /// <param name="reason">The failure reason.</param>
    [Theory]
    [InlineData("failed")]
    [InlineData(null)]
    [InlineData("<script>")]
    public void OtherReasonsAreAGeneralFailureWithARetry(string? reason)
    {
        var viewModel = new SignInFailedViewModel();

        viewModel.Describe(reason);

        viewModel.Title.ShouldBe("Sign-in did not complete");
        viewModel.Message.ShouldContain("try again");
    }
    #endregion Tests
}
