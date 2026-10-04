using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RaidManager.Companion.Client.Features.Pairing;

namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>Keeps the pairing in a file of the user's profile, with the token encrypted by the protector.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the token storage of ADR-0030 and ADR-0032: the file is written through a temporary file and a
/// replace, so a crash never leaves half a file, and a file that can't be read here counts as no pairing.
/// </remarks>
internal sealed class TokenStore : ITokenStore
{
    #region Constants
    /// <summary>Defines the suffix of the temporary file written before the replace.</summary>
    private const string TemporarySuffix = ".tmp";
    #endregion Constants

    #region Fields
    /// <summary>Stores the protector that encrypts the token.</summary>
    private readonly ITokenProtector _protector;

    /// <summary>Stores the file's path.</summary>
    private readonly string _filePath;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="TokenStore"/> class.</summary>
    /// <param name="protector">The protector that encrypts the token.</param>
    /// <param name="location">Where the file is.</param>
    public TokenStore(ITokenProtector protector, TokenFileLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        _protector = protector;
        _filePath = location.FilePath;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<PairedCompanion?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(_filePath, cancellationToken);
            var stored = JsonSerializer.Deserialize(bytes, TokenFileJsonContext.Default.StoredTokenFile);
            if (stored is null)
            {
                return null;
            }

            var token = Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(stored.ProtectedToken)));
            return new PairedCompanion(stored.CompanionId, stored.PlayerName, token);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or CryptographicException)
        {
            // A damaged file, or one copied from another user or computer, can't give a token back: pair again.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(PairedCompanion companion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(companion);
        var protectedToken = Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(companion.DeviceToken)));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            new StoredTokenFile(companion.CompanionId, companion.PlayerName, protectedToken), TokenFileJsonContext.Default.StoredTokenFile);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + TemporarySuffix;
        await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    /// <inheritdoc />
    public void Delete() => File.Delete(_filePath);
    #endregion Public Methods
}
