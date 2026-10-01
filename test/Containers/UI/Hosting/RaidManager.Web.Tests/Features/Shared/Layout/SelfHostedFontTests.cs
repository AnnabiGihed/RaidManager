using System.Net;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Shared.Layout;

/// <summary>Verifies that the website serves Open Sans itself instead of loading it from Google Fonts.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The owner decided the font files are served by the website (#265): no page may request Google's servers.
/// </remarks>
public sealed partial class SelfHostedFontTests
{
    #region Tests
    /// <summary>Checks that the page links the local font stylesheet and no Google Fonts URL.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PageLinksTheLocalFontStylesheetOnly()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var page = await browser.GetStringAsync("/");

        page.ShouldNotContain("fonts.googleapis.com");
        page.ShouldNotContain("fonts.gstatic.com");
        var stylesheet = FontStylesheetPattern().Match(page).Value;
        stylesheet.ShouldNotBeNullOrEmpty();
        var css = await browser.GetStringAsync("/" + stylesheet);
        css.ShouldContain("font-family: \"Open Sans\"");
        css.ShouldContain("open-sans-latin.woff2");
    }

    /// <summary>Requests the font files and the license.</summary>
    /// <param name="path">The file path under the website root.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("fonts/open-sans-latin.woff2")]
    [InlineData("fonts/open-sans-latin-ext.woff2")]
    [InlineData("fonts/OFL.txt")]
    public async Task FontFilesAreServed(string path)
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var response = await browser.GetAsync("/" + path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Length.ShouldBeGreaterThan(1000);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Finds the font stylesheet the page links, fingerprinted or not.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"fonts/open-sans[^""]*\.css")]
    private static partial Regex FontStylesheetPattern();
    #endregion Private Helpers
}
