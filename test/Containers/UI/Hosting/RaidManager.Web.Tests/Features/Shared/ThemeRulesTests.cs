using System.Net;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Shared;

/// <summary>Guards the rule that colors and Radzen overrides live only in the project-wide theme.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The owner made one project-wide theme mandatory (task #268): component and page stylesheets use the
/// <c>--rm-*</c> tokens, and the theme is linked once on every page.
/// </remarks>
public sealed partial class ThemeRulesTests
{
    #region Constants
    /// <summary>Defines the website project path from the repository root.</summary>
    private const string WebProject = "src/Containers/UI/Hosting/RaidManager.Web";
    #endregion Constants

    #region Tests
    /// <summary>Scans every component stylesheet for color values and Radzen variable declarations.</summary>
    [Fact]
    public void ComponentStylesheetsUseOnlyThemeTokens()
    {
        var stylesheets = Directory.GetFiles(Path.Combine(RepositoryRoot(), WebProject), "*.razor.css", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        stylesheets.ShouldNotBeEmpty();
        var offenders = stylesheets
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, number: index + 1)))
            .Where(entry => ColorValue().IsMatch(entry.line) || RadzenVariableDeclaration().IsMatch(entry.line))
            .Select(entry => $"{Path.GetFileName(entry.path)}:{entry.number}: {entry.line.Trim()}")
            .ToList();
        offenders.ShouldBeEmpty(string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Checks that the page links the theme and that it is served.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task PageLinksTheProjectTheme()
    {
        await using var site = new WebsiteFactory();
        using var browser = site.CreateBrowser();

        var page = await browser.GetStringAsync("/");
        var theme = ThemeStylesheet().Match(page).Value;

        theme.ShouldNotBeNullOrEmpty();
        var response = await browser.GetAsync("/" + theme);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain(".rm-theme-dark");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Finds the repository root by walking up to the solution file.</summary>
    /// <returns>The repository root.</returns>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RaidManager.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("RaidManager.sln was not found above the test output.");
    }

    /// <summary>Matches a hex, rgb or hsl color value.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(rgb|rgba|hsl|hsla)\(")]
    private static partial Regex ColorValue();

    /// <summary>Matches a declaration of a Radzen variable.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"--rz-[a-z0-9-]+\s*:")]
    private static partial Regex RadzenVariableDeclaration();

    /// <summary>Finds the theme stylesheet the page links, fingerprinted or not.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"theme/raidmanager-theme[^""]*\.css")]
    private static partial Regex ThemeStylesheet();
    #endregion Private Helpers
}
