using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="StepList"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The list numbers its steps in order from its parameter alone.
/// </remarks>
public sealed class StepListTests : BunitContext
{
    #region Tests
    /// <summary>Numbers each step and shows its title and detail.</summary>
    [Fact]
    public void StepsAreNumberedInOrder()
    {
        var list = Render<StepList>(parameters => parameters.Add(component => component.Steps, [new StepListItem("First", "One"), new StepListItem("Second", "Two")]));

        list.FindAll(".step-list-number").Select(number => number.TextContent).ShouldBe(["1", "2"]);
        list.FindAll(".step-list-title").Select(title => title.TextContent).ShouldBe(["First", "Second"]);
        list.FindAll(".step-list-detail").Select(detail => detail.TextContent).ShouldBe(["One", "Two"]);
        list.Find("ol").ShouldNotBeNull();
    }
    #endregion Tests
}
