using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="WowClassLabel"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: A class reads as players say it, with its color; an unknown class keeps its name.
/// </remarks>
public sealed class WowClassLabelTests : BunitContext
{
    #region Tests
    /// <summary>Names a known class with spaces and colors it, whatever form the API sends.</summary>
    /// <param name="className">The class as given.</param>
    /// <param name="label">The name shown.</param>
    /// <param name="cssClass">The color class.</param>
    [Theory]
    [InlineData("DeathKnight", "Death Knight", "wow-class-deathknight")]
    [InlineData("Death Knight", "Death Knight", "wow-class-deathknight")]
    [InlineData("shaman", "Shaman", "wow-class-shaman")]
    public void KnownClassesAreNamedAndColored(string className, string label, string cssClass)
    {
        var tag = Render<WowClassLabel>(parameters => parameters.Add(component => component.ClassName, className));

        tag.Find(".wow-class").ClassList.ShouldContain(cssClass);
        tag.Find(".wow-class").TextContent.Trim().ShouldBe(label);
        tag.Find(".wow-class-dot").GetAttribute("aria-hidden").ShouldBe("true");
    }

    /// <summary>Labels the class dot with the given content, such as a character's name, instead of the class name.</summary>
    [Fact]
    public void ChildContentReplacesTheClassName()
    {
        var tag = Render<WowClassLabel>(parameters => parameters
            .Add(component => component.ClassName, "DeathKnight")
            .AddChildContent("<a href=\"/characters/1\">Arthasdk</a>"));

        tag.Find(".wow-class").ClassList.ShouldContain("wow-class-deathknight");
        tag.Find(".wow-class a").TextContent.ShouldBe("Arthasdk");
        tag.Find(".wow-class").TextContent.ShouldNotContain("Death Knight");
    }

    /// <summary>Keeps an unknown class's name, without a class color.</summary>
    [Fact]
    public void UnknownClassKeepsItsName()
    {
        var tag = Render<WowClassLabel>(parameters => parameters.Add(component => component.ClassName, "Monk"));

        tag.Find(".wow-class").ClassName.ShouldBe("wow-class");
        tag.Find(".wow-class").TextContent.Trim().ShouldBe("Monk");
    }
    #endregion Tests
}
