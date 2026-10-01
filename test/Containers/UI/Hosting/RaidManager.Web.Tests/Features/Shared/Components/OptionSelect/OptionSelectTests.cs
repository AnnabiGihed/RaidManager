using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="OptionSelect{TValue}"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The select is labelled, marks the chosen option, and raises the value of the option the user picks.
/// </remarks>
public sealed class OptionSelectTests : BunitContext
{
    #region Tests
    /// <summary>Marks the chosen option and raises the newly picked value; ignores a value that isn't an option.</summary>
    [Fact]
    public void PickingAnOptionRaisesItsValue()
    {
        string? picked = null;
        var select = Render<OptionSelect<string>>(parameters => parameters
            .Add(component => component.Label, "Discord role")
            .Add(component => component.Options, [new ChoiceOption<string>("11", "@Guild Master"), new ChoiceOption<string>("13", "@Veteran")])
            .Add(component => component.Value, "11")
            .Add(component => component.ValueChanged, (string value) => picked = value));

        var element = select.Find("select");
        element.GetAttribute("aria-label").ShouldBe("Discord role");
        select.FindAll("option").Select(option => option.TextContent).ShouldBe(["@Guild Master", "@Veteran"]);
        select.FindAll("option")[0].HasAttribute("selected").ShouldBeTrue();

        element.Change("1");
        picked.ShouldBe("13");

        picked = null;
        element.Change("7");
        element.Change("not a number");
        picked.ShouldBeNull();
    }
    #endregion Tests
}
