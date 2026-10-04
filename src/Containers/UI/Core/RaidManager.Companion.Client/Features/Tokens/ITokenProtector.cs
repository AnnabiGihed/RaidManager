namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>Encrypts the device token for the current user of this computer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The seam between the token store and the platform's protection: DPAPI on Windows (ADR-0030), a fake in
/// tests.
/// </remarks>
public interface ITokenProtector
{
    #region Public Methods
    /// <summary>Encrypts data so only the current user of this computer can read it.</summary>
    /// <param name="data">The plain bytes.</param>
    /// <returns>The protected bytes.</returns>
    byte[] Protect(byte[] data);

    /// <summary>Decrypts data protected by <see cref="Protect"/>.</summary>
    /// <param name="data">The protected bytes.</param>
    /// <returns>The plain bytes.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when the data can't be decrypted here.</exception>
    byte[] Unprotect(byte[] data);
    #endregion Public Methods
}
