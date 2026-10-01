using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="TagChip"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The chip shows its text and tone, and a labelled × only when the parent can remove it.
/// </remarks>
public sealed class TagChipTests : BunitContext
{
    #region Tests
    /// <summary>Shows an information chip without a × when nothing can remove it.</summary>
    [Fact]
    public void ChipWithoutRemoveHasNoCross()
    {
        var chip = Render<TagChip>(parameters => parameters.Add(component => component.Text, "@Officier"));

        chip.Find(".tag-chip").ClassList.ShouldContain("tag-chip-info");
        chip.Find(".tag-chip-text").TextContent.ShouldBe("@Officier");
        chip.FindAll(".tag-chip-remove").ShouldBeEmpty();
    }

    /// <summary>Shows a danger chip whose labelled × raises the callback.</summary>
    [Fact]
    public void RemovableChipRaisesItsCallback()
    {
        var removed = false;
        var chip = Render<TagChip>(parameters => parameters
            .Add(component => component.Text, "Deleted role")
            .Add(component => component.Tone, TagChipTone.Danger)
            .Add(component => component.RemoveLabel, "Remove Deleted role")
            .Add(component => component.OnRemove, () => removed = true));

        chip.Find(".tag-chip").ClassList.ShouldContain("tag-chip-danger");
        var cross = chip.Find(".tag-chip-remove");
        cross.GetAttribute("aria-label").ShouldBe("Remove Deleted role");
        cross.GetAttribute("type").ShouldBe("button");
        cross.Click();

        removed.ShouldBeTrue();
    }

    /// <summary>Gives each tone its own class.</summary>
    /// <param name="tone">The tone.</param>
    /// <param name="expectedClass">The class it adds.</param>
    [Theory]
    [InlineData(TagChipTone.Success, "tag-chip-success")]
    [InlineData(TagChipTone.Neutral, "tag-chip-neutral")]
    public void EachToneHasItsClass(TagChipTone tone, string expectedClass) =>
        Render<TagChip>(parameters => parameters.Add(component => component.Text, "Officer").Add(component => component.Tone, tone))
            .Find(".tag-chip").ClassList.ShouldContain(expectedClass);
    #endregion Tests
}
