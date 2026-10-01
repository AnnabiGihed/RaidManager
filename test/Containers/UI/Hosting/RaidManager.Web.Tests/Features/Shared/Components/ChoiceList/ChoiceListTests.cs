using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="ChoiceList{TValue}"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The list is one radio group under its legend, marks the chosen row, and raises the newly chosen value.
/// </remarks>
public sealed class ChoiceListTests : BunitContext
{
    #region Tests
    /// <summary>Marks the chosen option and raises a new choice.</summary>
    [Fact]
    public void ChoosingAnOptionRaisesItsValue()
    {
        int? chosen = null;
        var list = Render<ChoiceList<int>>(parameters => parameters
            .Add(component => component.Legend, "Size")
            .Add(component => component.Options, [new ChoiceOption<int>(10, "10 players"), new ChoiceOption<int>(25, "25 players")])
            .Add(component => component.Value, 10)
            .Add(component => component.ValueChanged, (int value) => chosen = value));

        list.Find("legend").TextContent.ShouldBe("Size");
        list.FindAll(".choice-list-option-selected .choice-list-label").Select(label => label.TextContent).ShouldBe(["10 players"]);
        var inputs = list.FindAll("input[type=radio]");
        inputs.Select(input => input.GetAttribute("name")).Distinct().ShouldHaveSingleItem();
        inputs[0].HasAttribute("checked").ShouldBeTrue();

        inputs[1].Change(true);

        chosen.ShouldBe(25);
    }
    #endregion Tests
}
