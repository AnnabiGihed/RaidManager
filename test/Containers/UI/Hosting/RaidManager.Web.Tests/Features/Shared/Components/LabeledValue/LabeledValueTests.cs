using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="LabeledValue"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The block shows its label, value and optional note from its parameters alone.
/// </remarks>
public sealed class LabeledValueTests : BunitContext
{
    #region Tests
    /// <summary>Shows the label, the value and the note.</summary>
    [Fact]
    public void LabelValueAndNoteAreShown()
    {
        var block = Render<LabeledValue>(parameters => parameters.Add(component => component.Label, "Warmane realm").Add(component => component.Value, "Icecrown").Add(component => component.Note, "Chosen by you"));

        block.Find(".labeled-value-label").TextContent.ShouldBe("Warmane realm");
        block.Find(".labeled-value-value").TextContent.ShouldBe("Icecrown");
        block.Find(".labeled-value-note").TextContent.ShouldBe("Chosen by you");
    }

    /// <summary>Leaves out an empty note.</summary>
    [Fact]
    public void EmptyNoteIsLeftOut()
    {
        var block = Render<LabeledValue>(parameters => parameters.Add(component => component.Label, "Administrator").Add(component => component.Value, "Gihed"));

        block.FindAll(".labeled-value-note").ShouldBeEmpty();
    }
    #endregion Tests
}
