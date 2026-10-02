using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="EmptyState"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The card shows its title, message, extra content and actions.
/// </remarks>
public sealed class EmptyStateTests : BunitContext
{
    #region Tests
    /// <summary>Shows every part given.</summary>
    [Fact]
    public void EmptyStateShowsItsParts()
    {
        var state = Render<EmptyState>(parameters => parameters
            .Add(component => component.Title, "You're all set")
            .Add(component => component.Message, "No characters are waiting for your decision.")
            .Add(component => component.ChildContent, "Sylvanash stays in conflict review.")
            .Add(component => component.Actions, "Continue")
            .AddUnmatched("data-testid", "all-set"));

        state.Find("section").GetAttribute("data-testid").ShouldBe("all-set");
        state.Find("h2").TextContent.ShouldBe("You're all set");
        state.Find(".empty-state-message").TextContent.ShouldBe("No characters are waiting for your decision.");
        state.Markup.ShouldContain("Sylvanash stays in conflict review.");
        state.Find(".empty-state-actions").TextContent.ShouldBe("Continue");
    }

    /// <summary>Leaves out the actions when none are given.</summary>
    [Fact]
    public void ActionsAreOptional()
    {
        var state = Render<EmptyState>(parameters => parameters
            .Add(component => component.Title, "Done")
            .Add(component => component.Message, "Nothing left."));

        state.FindAll(".empty-state-actions").ShouldBeEmpty();
    }
    #endregion Tests
}
