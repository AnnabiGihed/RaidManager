using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="Notice"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The notice shows its title and message, names its tone with a mark as well as color, and interrupts screen readers only for a danger.
/// </remarks>
public sealed class NoticeTests : BunitContext
{
    #region Tests
    /// <summary>Shows an information notice as a status with an "i" mark.</summary>
    [Fact]
    public void InfoNoticeIsAStatus()
    {
        var notice = Render<Notice>(parameters => parameters.Add(component => component.Title, "Heads up").Add(component => component.Message, "Details"));

        notice.Find(".notice").ClassList.ShouldContain("notice-info");
        notice.Find(".notice").GetAttribute("role").ShouldBe("status");
        notice.Find(".notice-mark").TextContent.ShouldBe("i");
        notice.Find(".notice-title").TextContent.ShouldBe("Heads up");
        notice.Find(".notice-message").TextContent.ShouldBe("Details");
    }

    /// <summary>Shows a danger notice as an alert with a "!" mark, and leaves out an empty message.</summary>
    [Fact]
    public void DangerNoticeIsAnAlert()
    {
        var notice = Render<Notice>(parameters => parameters.Add(component => component.Title, "Refused").Add(component => component.Tone, NoticeTone.Danger));

        notice.Find(".notice").ClassList.ShouldContain("notice-danger");
        notice.Find(".notice").GetAttribute("role").ShouldBe("alert");
        notice.Find(".notice-mark").TextContent.ShouldBe("!");
        notice.FindAll(".notice-message").ShouldBeEmpty();
    }
    #endregion Tests
}
