using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Moq;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Features.Pairing;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Pairing;

/// <summary>Shows each pairing state headless and checks what the player sees against its board.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: One test per state of the companion pairing mockup the view shows (boards 5 to 8, 19 and 20): its texts,
/// its actions and whether they're enabled; and one test that a click reaches its command (avalonia-tests §3).
/// </remarks>
public sealed class PairingViewTests : IDisposable
{
    #region Fields
    /// <summary>Stores the view model driver.</summary>
    private readonly PairingStates _states = new();
    #endregion Fields

    #region Public Methods
    /// <summary>Stops the view model's flow.</summary>
    public void Dispose() => _states.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>Board 19: the code card says the code is coming, and both actions are disabled.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task GettingACodeDisablesBothActions()
    {
        var window = await ShowAsync(PairingState.GettingCode);

        VisibleTexts(window).ShouldBe(["Pair with RaidManager", "Open the website, sign in with Discord, and\nconfirm this code.",
            "PAIRING CODE", "Getting a code…", "Asking RaidManager for a code", "Open the website", "Get a new code"]);
        Button(window, "OpenWebsiteButton").IsEffectivelyEnabled.ShouldBeFalse();
        Button(window, "NewCodeButton").IsEffectivelyEnabled.ShouldBeFalse();
    }

    /// <summary>Board 5: the code, its countdown and both actions.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WaitingShowsTheCodeAndItsCountdown()
    {
        var window = await ShowAsync(PairingState.Waiting);

        VisibleTexts(window).ShouldBe(["Pair with RaidManager", "Open the website, sign in with Discord, and\nconfirm this code.",
            "PAIRING CODE", PairingStates.Code, "Expires in 10:00", "Waiting for confirmation on the website", "Open the website",
            "Get a new code"]);
        Button(window, "OpenWebsiteButton").IsEffectivelyEnabled.ShouldBeTrue();
        Button(window, "NewCodeButton").IsEffectivelyEnabled.ShouldBeTrue();
        AutomationProperties.GetName(window.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "CodeText")).ShouldBe("Pairing code");
    }

    /// <summary>Board 6, without "Choose folders" until #384 (owner decision on #514).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task PairedNamesThePlayer()
    {
        var window = await ShowAsync(PairingState.Paired);

        VisibleTexts(window).ShouldBe(["✓", $"Paired with {PairingStates.Player}", "This computer can now upload your character data.",
            "You can revoke it anytime on the website, under Companion & sync."]);
        VisibleButtons(window).ShouldBeEmpty();
    }

    /// <summary>Board 7: the expired code, the notice and a new code.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task ExpiredOffersANewCode()
    {
        var window = await ShowAsync(PairingState.Expired);

        VisibleTexts(window).ShouldBe(["Pair with RaidManager", "This code can't be confirmed anymore.", "PAIRING CODE", PairingStates.Code,
            "Expired", "!", "This code expired", "Codes last 10 minutes. Get a new one to try again.", "Get a new code"]);
        VisibleButtons(window).ShouldBe(["ExpiredNewCodeButton"]);
    }

    /// <summary>Board 8: the computer's name, the danger notice and Pair again.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task RevokedOffersToPairAgain()
    {
        var window = await ShowAsync(PairingState.Revoked);

        VisibleTexts(window).ShouldBe(["Uploads stopped", $"{Environment.MachineName} isn't paired with your account anymore.", "!",
            "This companion was revoked", "It was revoked on the website, so it can't upload.", "Pair again",
            "Data it already uploaded stays in RaidManager."]);
        VisibleButtons(window).ShouldBe(["PairAgainButton"]);
    }

    /// <summary>Board 20: the danger notice and Try again.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task CodeRequestFailedOffersToTryAgain()
    {
        var window = await ShowAsync(PairingState.CodeRequestFailed);

        VisibleTexts(window).ShouldBe(["Pair with RaidManager", "The companion needs a code from RaidManager first.", "!",
            "We couldn't reach RaidManager", "Check your connection, then try again.", "Try again"]);
        VisibleButtons(window).ShouldBe(["TryAgainButton"]);
    }

    /// <summary>A click on Try again runs the request for a code, through the binding.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task TryAgainRequestsACode()
    {
        var window = await ShowAsync(PairingState.CodeRequestFailed);
        var button = Button(window, "TryAgainButton");

        button.Focus();
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        _states.Api.Verify(api => api.StartPairingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Lists the texts the player sees, in reading order.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The texts.</returns>
    private static List<string?> VisibleTexts(Window window) => [.. window.GetVisualDescendants()
        .OfType<TextBlock>()
        .Where(text => text.IsEffectivelyVisible && !string.IsNullOrEmpty(text.Text))
        .Select(text => text.Text)];

    /// <summary>Lists the names of the buttons the player sees.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The names.</returns>
    private static List<string?> VisibleButtons(Window window) => [.. window.GetVisualDescendants()
        .OfType<Button>()
        .Where(button => button.IsEffectivelyVisible)
        .Select(button => button.Name)];

    /// <summary>Finds a named button.</summary>
    /// <param name="window">The window.</param>
    /// <param name="name">The button's name.</param>
    /// <returns>The button.</returns>
    private static Button Button(Window window, string name) =>
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == name);

    /// <summary>Shows the pairing view in a window of the companion's size, in a state.</summary>
    /// <param name="state">The state.</param>
    /// <returns>The window.</returns>
    private async Task<Window> ShowAsync(PairingState state)
    {
        await _states.ShowAsync(state);
        var window = new Window { Width = 480, Height = 556, Content = new PairingView { DataContext = _states.ViewModel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
    #endregion Private Helpers
}
