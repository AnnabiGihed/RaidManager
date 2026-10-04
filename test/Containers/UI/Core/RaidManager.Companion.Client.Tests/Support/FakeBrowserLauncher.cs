using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Records the addresses the companion opens.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the tests see that the website opens with the shown code, without a browser.
/// </remarks>
internal sealed class FakeBrowserLauncher : IBrowserLauncher
{
    #region Public Properties
    /// <summary>Gets the opened addresses.</summary>
    public List<Uri> Opened { get; } = [];

    /// <summary>Gets or sets a value indicating whether the system accepts the request.</summary>
    public bool Accepts { get; set; } = true;
    #endregion Public Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<bool> OpenAsync(Uri address)
    {
        Opened.Add(address);
        return Task.FromResult(Accepts);
    }
    #endregion Public Methods
}
