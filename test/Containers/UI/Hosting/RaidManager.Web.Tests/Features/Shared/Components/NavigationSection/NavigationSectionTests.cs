using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="NavigationSection"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The section lists the entries it is given, so the shell builds its navigation from view models.
/// </remarks>
public sealed class NavigationSectionTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="NavigationSectionTests"/> class.</summary>
    public NavigationSectionTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Lists the section's pages under its heading.</summary>
    [Fact]
    public void NavigationSectionLinksItsPages()
    {
        var section = Render<NavigationSection>(parameters => parameters
            .Add(component => component.Heading, "OFFICER")
            .Add(component => component.Entries, [new ShellEntry(ShellSection.Officer, "Schedule", "/schedule"), new ShellEntry(ShellSection.Officer, "Conflicts", "/conflicts")]));

        section.Find(".navigation-section-heading").TextContent.ShouldBe("OFFICER");
        section.FindAll("a").Select(link => link.GetAttribute("href")).ShouldBe(["/schedule", "/conflicts"]);
    }

    #endregion Tests
}
