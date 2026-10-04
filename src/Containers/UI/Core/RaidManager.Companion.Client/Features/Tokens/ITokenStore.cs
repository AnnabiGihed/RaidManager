using RaidManager.Companion.Client.Features.Pairing;

namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>Keeps this computer's pairing between two starts of the companion.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The only place the device token is kept (ADR-0030): never in addon files, never logged.
/// </remarks>
public interface ITokenStore
{
    #region Public Methods
    /// <summary>Reads the stored pairing.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The paired companion, or <see langword="null"/> when this computer isn't paired.</returns>
    Task<PairedCompanion?> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Stores a pairing, replacing any earlier one.</summary>
    /// <param name="companion">The paired companion.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the pairing is stored.</returns>
    Task SaveAsync(PairedCompanion companion, CancellationToken cancellationToken);

    /// <summary>Forgets the stored pairing.</summary>
    void Delete();
    #endregion Public Methods
}
