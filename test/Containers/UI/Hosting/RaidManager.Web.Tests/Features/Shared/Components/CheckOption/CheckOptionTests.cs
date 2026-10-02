using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="CheckOption"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The option shows its title and detail, reports ticks, and stays visible but fixed when disabled.
/// </remarks>
public sealed class CheckOptionTests : BunitContext
{
    #region Tests
    /// <summary>Reports a tick and a clear.</summary>
    [Fact]
    public void TicksAreReported()
    {
        var states = new List<bool>();
        var option = Render<CheckOption>(parameters => parameters
            .Add(component => component.Title, "Manage raids")
            .Add(component => component.Detail, "Create and edit raids.")
            .Add(component => component.CheckedChanged, state => states.Add(state)));

        option.Find(".check-option-title").TextContent.ShouldBe("Manage raids");
        option.Find(".check-option-detail").TextContent.ShouldBe("Create and edit raids.");
        option.Find("input").Change(true);
        option.Find("input").Change(false);

        states.ShouldBe([true, false]);
    }

    /// <summary>Shows a disabled option as such, ticked when given, without a detail line when none is given.</summary>
    [Fact]
    public void ADisabledOptionIsMarked()
    {
        var option = Render<CheckOption>(parameters => parameters
            .Add(component => component.Title, "Manage community roles")
            .Add(component => component.Checked, true)
            .Add(component => component.Disabled, true));

        option.Find("label").ClassList.ShouldContain("check-option-disabled");
        option.Find("input").HasAttribute("disabled").ShouldBeTrue();
        option.Find("input").HasAttribute("checked").ShouldBeTrue();
        option.FindAll(".check-option-detail").ShouldBeEmpty();
    }
    #endregion Tests
}
