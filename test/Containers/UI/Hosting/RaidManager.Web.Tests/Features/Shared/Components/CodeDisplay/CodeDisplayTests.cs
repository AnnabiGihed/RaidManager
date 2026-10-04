using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the large code block.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Covers the label, code and caption, and the muted look of a code that can't be used.
/// </remarks>
public sealed class CodeDisplayTests : BunitContext
{
    #region Tests
    /// <summary>Shows a code with its caption.</summary>
    [Fact]
    public void CodeShowsUnderItsLabelWithTheCaption()
    {
        var block = Render<CodeDisplay>(parameters => parameters
            .Add(component => component.Label, "Pairing code")
            .Add(component => component.Code, "K7M-4QX")
            .Add(component => component.Caption, "Expires in 9 minutes"));

        block.Find(".code-display").ClassList.ShouldNotContain("code-display-muted");
        block.Find(".code-display-label").TextContent.ShouldBe("Pairing code");
        block.Find(".code-display-code").TextContent.ShouldBe("K7M-4QX");
        block.Find(".code-display-caption").TextContent.ShouldBe("Expires in 9 minutes");
    }

    /// <summary>Shows a muted code without a caption.</summary>
    [Fact]
    public void MutedCodeHasNoCaptionWhenNoneIsGiven()
    {
        var block = Render<CodeDisplay>(parameters => parameters
            .Add(component => component.Label, "Pairing code")
            .Add(component => component.Code, "K7M-4QX")
            .Add(component => component.Muted, true));

        block.Find(".code-display").ClassList.ShouldContain("code-display-muted");
        block.FindAll(".code-display-caption").ShouldBeEmpty();
    }
    #endregion Tests
}
