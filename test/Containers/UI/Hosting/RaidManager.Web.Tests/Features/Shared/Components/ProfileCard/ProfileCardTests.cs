using Bunit;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="ProfileCard"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The card shows the avatar, the name and the optional line under it.
/// </remarks>
public sealed class ProfileCardTests : BunitContext
{
    #region Tests
    /// <summary>Shows the picture, the name and the line under it.</summary>
    [Fact]
    public void CardShowsThePictureNameAndDetail()
    {
        var card = Render<ProfileCard>(parameters => parameters
            .Add(component => component.Name, "Bryn Valewood")
            .Add(component => component.Detail, "Signed in with Discord")
            .Add(component => component.ImageUrl, "https://cdn.discordapp.com/avatars/1/a.png")
            .AddUnmatched("data-testid", "account-card"));

        card.Find(".profile-card").GetAttribute("data-testid").ShouldBe("account-card");
        card.Find("img.user-avatar").GetAttribute("width").ShouldBe("40");
        card.Find(".profile-card-name").TextContent.ShouldBe("Bryn Valewood");
        card.Find(".profile-card-detail").TextContent.ShouldBe("Signed in with Discord");
    }

    /// <summary>Shows initials without a picture, and no line without a detail.</summary>
    [Fact]
    public void InitialsAndNoDetailWhenNoneGiven()
    {
        var card = Render<ProfileCard>(parameters => parameters.Add(component => component.Name, "Bryn Valewood"));

        card.Find(".user-avatar-initials").TextContent.ShouldBe("BV");
        card.FindAll(".profile-card-detail").ShouldBeEmpty();
    }
    #endregion Tests
}
