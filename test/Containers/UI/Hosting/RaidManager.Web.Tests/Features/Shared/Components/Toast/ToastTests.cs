using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="Toast"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The notification shows its title and message as a status screen readers announce.
/// </remarks>
public sealed class ToastTests : BunitContext
{
    #region Tests
    /// <summary>Shows the title and the message as a status.</summary>
    [Fact]
    public void ToastShowsItsTitleAndMessage()
    {
        var toast = Render<Toast>(parameters => parameters.Add(component => component.Title, "Officer roles saved").Add(component => component.Message, "Members get them at their next check."));

        toast.Find(".toast").GetAttribute("role").ShouldBe("status");
        toast.Find(".toast-title").TextContent.ShouldBe("Officer roles saved");
        toast.Find(".toast-message").TextContent.ShouldBe("Members get them at their next check.");
    }

    /// <summary>Gives each tone its own class, teal by default.</summary>
    /// <param name="tone">The tone, or <see langword="null"/> for the default.</param>
    /// <param name="expectedClass">The class it adds.</param>
    [Theory]
    [InlineData(null, "toast-success")]
    [InlineData(ToastTone.Info, "toast-info")]
    [InlineData(ToastTone.Warning, "toast-warning")]
    [InlineData(ToastTone.Danger, "toast-danger")]
    public void EachToneHasItsClass(ToastTone? tone, string expectedClass)
    {
        var toast = Render<Toast>(parameters =>
        {
            parameters.Add(component => component.Title, "Saved").Add(component => component.Message, "Done.");
            if (tone is { } value)
            {
                parameters.Add(component => component.Tone, value);
            }
        });

        toast.Find(".toast").ClassList.ShouldContain(expectedClass);
    }
    #endregion Tests
}
