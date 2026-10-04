using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Companions.Repositories;

/// <summary>Defines persistence operations for companion pairing requests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Finds a request by what each side holds: the companion its device code, the player the code on screen.
/// </remarks>
public interface ICompanionPairingRepository : IAsyncCommandRepository<CompanionPairing, CompanionPairingId>
{
    #region Methods
    /// <summary>Finds the request a companion's device code belongs to.</summary>
    /// <param name="deviceCodeHash">The hash of the device code.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The request, or <see langword="null"/>.</returns>
    Task<CompanionPairing?> FindByDeviceCodeHashAsync(CredentialHash deviceCodeHash, CancellationToken cancellationToken);

    /// <summary>Finds the latest request that shows a code and hasn't been collected.</summary>
    /// <param name="code">The code the player entered.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The request, or <see langword="null"/>.</returns>
    Task<CompanionPairing?> FindLatestUncollectedByCodeAsync(PairingCode code, CancellationToken cancellationToken);

    /// <summary>Tells whether a request that hasn't expired already shows a code.</summary>
    /// <param name="code">The code.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the code is in use.</returns>
    Task<bool> IsCodeInUseAsync(PairingCode code, DateTimeOffset nowUtc, CancellationToken cancellationToken);
    #endregion Methods
}
