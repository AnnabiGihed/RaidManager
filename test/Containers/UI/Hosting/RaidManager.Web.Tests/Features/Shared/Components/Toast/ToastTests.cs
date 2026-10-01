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
    #endregion Tests
}
