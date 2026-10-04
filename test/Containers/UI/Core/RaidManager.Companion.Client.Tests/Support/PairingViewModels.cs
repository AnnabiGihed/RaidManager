using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Creates pairing view models over the test doubles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the view model's construction, the fixed start time and the test addresses in one place.
/// </remarks>
internal static class PairingViewModels
{
    #region Fields
    /// <summary>Stores the time every test starts at.</summary>
    public static readonly DateTimeOffset Start = new(2026, 10, 4, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Stores the website address of the tests.</summary>
    public static readonly Uri Website = new("https://raidmanager.test/");
    #endregion Fields

    #region Public Methods
    /// <summary>Creates a view model.</summary>
    /// <param name="api">The API double.</param>
    /// <param name="store">The token store double.</param>
    /// <param name="browser">The browser double.</param>
    /// <param name="time">The clock.</param>
    /// <returns>The view model.</returns>
    public static PairingViewModel Create(ICompanionApi api, ITokenStore store, IBrowserLauncher browser, FakeTimeProvider time) =>
        new(
            api,
            store,
            browser,
            time,
            Options.Create(new CompanionOptions { ApiBaseUrl = new Uri("https://api.raidmanager.test/"), WebsiteBaseUrl = Website }),
            NullLogger<PairingViewModel>.Instance);

    /// <summary>Creates a started pairing that expires after a number of minutes.</summary>
    /// <param name="code">The pairing code.</param>
    /// <param name="minutes">The minutes before it expires.</param>
    /// <param name="intervalSeconds">The polling interval the API asks for.</param>
    /// <returns>The started pairing.</returns>
    public static StartedPairing Pairing(string code, int minutes, int intervalSeconds) =>
        new("device-code", code, Start.AddMinutes(minutes), TimeSpan.FromSeconds(intervalSeconds));
    #endregion Public Methods
}
