using System.Security.Cryptography;
using System.Text;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Tokens;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Tokens;

/// <summary>Verifies the token file in a temporary folder, with a reversible fake protector.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="TokenStore"/>: the token never reaches the file in plain text, a saved pairing
/// loads back, a damaged or foreign file counts as no pairing, and deleting forgets it (ADR-0030).
/// </remarks>
public sealed class TokenStoreTests : IDisposable
{
    #region Fields
    /// <summary>Stores the test's temporary folder.</summary>
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"companion-tests-{Guid.NewGuid():N}");

    /// <summary>Stores the protector double.</summary>
    private readonly ReversingProtector _protector = new();

    /// <summary>Stores the store under test.</summary>
    private readonly TokenStore _store;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="TokenStoreTests"/> class.</summary>
    public TokenStoreTests()
    {
        _store = new TokenStore(_protector, new TokenFileLocation(FilePath));
    }
    #endregion Constructors

    #region Private Properties
    /// <summary>Gets the token file's path, in a folder that doesn't exist yet.</summary>
    private string FilePath => Path.Combine(_folder, "RaidManager", "companion.dat");
    #endregion Private Properties

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
    #endregion Public Methods

    #region Tests
    /// <summary>Without a file, there is no pairing.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task LoadAsyncWithoutAFileReturnsNull() => (await _store.LoadAsync(CancellationToken.None)).ShouldBeNull();

    /// <summary>A saved pairing loads back, and its token is only in the file protected.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task SaveAsyncThenLoadAsyncReturnsThePairingWithoutAPlainToken()
    {
        var companion = new PairedCompanion(Guid.NewGuid(), "Bryn", "secret-device-token");

        await _store.SaveAsync(companion, CancellationToken.None);

        (await _store.LoadAsync(CancellationToken.None)).ShouldBe(companion);
        (await File.ReadAllTextAsync(FilePath)).ShouldNotContain("secret-device-token");
        File.Exists(FilePath + ".tmp").ShouldBeFalse();
    }

    /// <summary>Saving again replaces the earlier pairing.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task SaveAsyncTwiceKeepsTheLatestPairing()
    {
        await _store.SaveAsync(new PairedCompanion(Guid.NewGuid(), "Bryn", "first"), CancellationToken.None);
        var latest = new PairedCompanion(Guid.NewGuid(), "Bryn", "second");

        await _store.SaveAsync(latest, CancellationToken.None);

        (await _store.LoadAsync(CancellationToken.None)).ShouldBe(latest);
    }

    /// <summary>A damaged file, an empty one, or one another user protected counts as no pairing.</summary>
    /// <param name="content">The file's content.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"companionId":"6d1f8d5e-3e8e-4a33-9f2c-6f0b7d2d8e11","playerName":"Bryn","protectedToken":"%%%"}""")]
    [InlineData("""{"companionId":"6d1f8d5e-3e8e-4a33-9f2c-6f0b7d2d8e11","playerName":"Bryn","protectedToken":"Zm9yZWlnbg=="}""")]
    public async Task LoadAsyncWhenTheFileCantGiveATokenReturnsNull(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllTextAsync(FilePath, content);

        (await _store.LoadAsync(CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>Deleting forgets the pairing, and deleting nothing is fine.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task DeleteForgetsThePairing()
    {
        await _store.SaveAsync(new PairedCompanion(Guid.NewGuid(), "Bryn", "token"), CancellationToken.None);

        _store.Delete();
        _store.Delete();

        (await _store.LoadAsync(CancellationToken.None)).ShouldBeNull();
    }

    /// <summary>The current user's file is in the local application data.</summary>
    [Fact]
    public void ForCurrentUserPointsToTheLocalApplicationData() =>
        TokenFileLocation.ForCurrentUser().FilePath.ShouldBe(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaidManager", "companion.dat"));
    #endregion Tests

    #region Nested Types
    /// <summary>Reverses the bytes, and refuses data it didn't protect, as DPAPI refuses another user's data.</summary>
    private sealed class ReversingProtector : ITokenProtector
    {
        /// <summary>Defines the marker this protector adds.</summary>
        private static readonly byte[] Marker = Encoding.UTF8.GetBytes("protected:");

        /// <inheritdoc />
        public byte[] Protect(byte[] data) => [.. Marker, .. data.Reverse()];

        /// <inheritdoc />
        public byte[] Unprotect(byte[] data) => data.AsSpan().StartsWith(Marker)
            ? data[Marker.Length..].Reverse().ToArray()
            : throw new CryptographicException("Protected by someone else.");
    }
    #endregion Nested Types
}
