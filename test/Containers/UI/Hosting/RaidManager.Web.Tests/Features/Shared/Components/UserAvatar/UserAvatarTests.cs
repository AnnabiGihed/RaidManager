using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="UserAvatar"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The avatar shows the image it is given at the size it is given.
/// </remarks>
public sealed class UserAvatarTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UserAvatarTests"/> class.</summary>
    public UserAvatarTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the avatar image when there is one.</summary>
    [Fact]
    public void UserAvatarShowsTheImageAtItsSize()
    {
        var avatar = Render<UserAvatar>(parameters => parameters
            .Add(component => component.Name, "Arthas Menethil")
            .Add(component => component.ImageUrl, "https://cdn.discordapp.com/avatars/1/a.png")
            .Add(component => component.Size, 40));

        var image = avatar.Find("img.user-avatar");
        image.GetAttribute("width").ShouldBe("40");
        avatar.FindAll(".user-avatar-initials").ShouldBeEmpty();
    }
    #endregion Tests
}
