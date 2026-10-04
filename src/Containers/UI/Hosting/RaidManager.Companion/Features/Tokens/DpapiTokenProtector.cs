using System.Runtime.Versioning;
using System.Security.Cryptography;
using RaidManager.Companion.Client.Features.Tokens;

namespace RaidManager.Companion.Features.Tokens;

/// <summary>Encrypts the device token with the Windows Data Protection API for the current Windows user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The token storage of ADR-0030: another Windows user, or a copy of the file on another computer, can't
/// decrypt it. Windows only, so the composition root registers it only on Windows (avalonia-desktop §9).
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class DpapiTokenProtector : ITokenProtector
{
    #region Fields
    /// <summary>Stores the entropy that ties the protected bytes to the companion.</summary>
    private static readonly byte[] Entropy = "RaidManager.Companion.DeviceToken"u8.ToArray();
    #endregion Fields

    #region Public Methods
    /// <inheritdoc />
    public byte[] Protect(byte[] data) => ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);

    /// <inheritdoc />
    public byte[] Unprotect(byte[] data) => ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
    #endregion Public Methods
}
