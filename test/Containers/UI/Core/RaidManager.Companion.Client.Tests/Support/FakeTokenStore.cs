using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Keeps the pairing in memory.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the pairing flow's tests see what was stored or deleted without touching the user profile.
/// </remarks>
internal sealed class FakeTokenStore : ITokenStore
{
    #region Public Properties
    /// <summary>Gets or sets the stored pairing.</summary>
    public PairedCompanion? Stored { get; set; }

    /// <summary>Gets a value indicating whether the pairing was deleted.</summary>
    public bool Deleted { get; private set; }
    #endregion Public Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<PairedCompanion?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Stored);

    /// <inheritdoc />
    public Task SaveAsync(PairedCompanion companion, CancellationToken cancellationToken)
    {
        Stored = companion;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Delete()
    {
        Stored = null;
        Deleted = true;
    }
    #endregion Public Methods
}
