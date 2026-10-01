using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Features.Communities.Pages;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Communities.Pages;

/// <summary>Verifies the realm choice of board 2.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The page reads the protected pending link, links only after a realm is chosen, and handles an expired link, a server linked in the meantime and an unreachable API.
/// </remarks>
public sealed class ChooseRealmTests : BunitContext
{
    #region Fields
    /// <summary>Stores the signed-in user.</summary>
    private readonly Guid _userId = Guid.NewGuid();

    /// <summary>Stores the fake API's community endpoints.</summary>
    private readonly FakeCommunitiesApiClient _communities = new();
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ChooseRealmTests"/> class.</summary>
    public ChooseRealmTests()
    {
        Services.AddRadzenComponents();
        Services.AddSingleton<ICommunitiesApiClient>(_communities);
        Services.AddScoped<ChooseRealmViewModel>();
        Services.AddSingleton(new CommunityLinkProtector(new EphemeralDataProtectionProvider()));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the server and the realms, and links once a realm is chosen.</summary>
    [Fact]
    public void ChoosingARealmLinksTheServerAndOpensTheCommunity()
    {
        SignIn();
        var navigation = OpenWith(Pending(_userId));

        var page = Render<ChooseRealm>();

        page.Find("h1").TextContent.ShouldBe("Set up Dark Templars");
        page.Find(".page-heading-eyebrow").TextContent.ShouldBe("Almost done");
        page.Find(".labeled-value-value").TextContent.ShouldBe("Dark Templars");
        page.Find(".labeled-value-note").TextContent.ShouldBe("Added by Gihed Annabi, who becomes its Administrator");
        page.FindAll(".choice-list-label").Select(label => label.TextContent).ShouldBe(["Icecrown", "Lordaeron", "Blackrock", "Onyxia"]);
        FinishButton(page).HasAttribute("disabled").ShouldBeTrue();

        page.FindAll(".choice-list-option input")[1].Change(true);
        FinishButton(page).HasAttribute("disabled").ShouldBeFalse();
        FinishButton(page).Click();

        var link = _communities.Links.ShouldHaveSingleItem();
        link.Realm.ShouldBe("Lordaeron");
        link.Link.ShouldBe(Pending(_userId));
        navigation.Uri.ShouldEndWith("/community?linked=true");
    }

    /// <summary>Opens the existing community when the server was linked in the meantime.</summary>
    [Fact]
    public void AServerLinkedInTheMeantimeOpensTheExistingCommunity()
    {
        var existing = FakeCommunitiesApiClient.Community(Guid.NewGuid()) with { DiscordGuildId = "555" };
        _communities.Communities.Add(existing);
        SignIn();
        var navigation = OpenWith(Pending(_userId) with { DiscordGuildId = "555" });
        var page = Render<ChooseRealm>();

        page.FindAll(".choice-list-option input")[0].Change(true);
        FinishButton(page).Click();

        navigation.Uri.ShouldEndWith($"/communities/link/already-linked?community={existing.CommunityId}");
    }

    /// <summary>Keeps the page with a notice when the API can't link the server.</summary>
    [Fact]
    public void AnUnreachableApiKeepsThePageWithANotice()
    {
        _communities.Fails = true;
        SignIn();
        OpenWith(Pending(_userId));
        var page = Render<ChooseRealm>();

        page.FindAll(".choice-list-option input")[0].Change(true);
        FinishButton(page).Click();

        page.Find("[data-testid=link-failed] .notice-title").TextContent.ShouldBe("The community wasn't linked");
        page.Find("h1").TextContent.ShouldBe("Set up Dark Templars");
    }

    /// <summary>Sends an invalid, tampered or another user's link back to the Overview, which says it expired.</summary>
    /// <param name="variant">How the link is wrong.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("tampered")]
    [InlineData("another user")]
    public void AnUnusableLinkGoesBackToTheOverview(string variant)
    {
        SignIn();
        var protector = Services.GetRequiredService<CommunityLinkProtector>();
        var link = variant switch
        {
            "missing" => null,
            "tampered" => "not-a-protected-link",
            _ => protector.Protect(Pending(Guid.NewGuid())),
        };
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(link is null ? CommunityRoutes.ChooseRealm : $"{CommunityRoutes.ChooseRealm}?link={Uri.EscapeDataString(link)}");

        Render<ChooseRealm>();

        navigation.Uri.ShouldEndWith("/?link=expired");
    }

    /// <summary>Goes back to the Overview without linking.</summary>
    [Fact]
    public void CancelLinksNothing()
    {
        SignIn();
        var navigation = OpenWith(Pending(_userId));
        var page = Render<ChooseRealm>();

        page.FindAll("button").First(button => button.TextContent.Contains("Cancel", StringComparison.Ordinal)).Click();

        _communities.Links.ShouldBeEmpty();
        new Uri(navigation.Uri).AbsolutePath.ShouldBe("/");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates the pending link of a server the user added the bot to.</summary>
    /// <param name="userId">The user who added the bot.</param>
    /// <returns>The pending link.</returns>
    private static PendingCommunityLink Pending(Guid userId) => new("987654321098765432", "Dark Templars", userId);

    /// <summary>Finds the Finish linking button.</summary>
    /// <param name="page">The rendered page.</param>
    /// <returns>The button.</returns>
    private static AngleSharp.Dom.IElement FinishButton(IRenderedComponent<ChooseRealm> page) =>
        page.FindAll("button").First(button => button.TextContent.Contains("Finish linking", StringComparison.Ordinal));

    /// <summary>Signs the user in with their RaidManager user id.</summary>
    private void SignIn() =>
        AddAuthorization().SetAuthorized("Gihed Annabi").SetClaims(new Claim(RaidManagerClaimTypes.UserId, _userId.ToString()));

    /// <summary>Opens the page's address with a protected pending link.</summary>
    /// <param name="link">The pending link.</param>
    /// <returns>The navigation manager.</returns>
    private NavigationManager OpenWith(PendingCommunityLink link)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var protectedLink = Services.GetRequiredService<CommunityLinkProtector>().Protect(link);
        navigation.NavigateTo(CommunityRoutes.ChooseRealmFor(protectedLink));
        return navigation;
    }
    #endregion Private Helpers
}
