using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RaidManager.Companion.Client.Features.Sync.Settings;

/// <summary>Reads and writes the player's sync choices.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keeps <c>sync-settings.json</c> next to <c>companion.dat</c>, written through a temporary file and a
/// replace like the token file; a damaged file counts as a companion that hasn't searched yet (#550).
/// </remarks>
internal sealed partial class SyncSettingsStore
{
    #region Constants
    /// <summary>Defines the suffix of the temporary file written before the replace.</summary>
    private const string TemporarySuffix = ".tmp";
    #endregion Constants

    #region Fields
    /// <summary>Stores the settings file's path.</summary>
    private readonly string _filePath;

    /// <summary>Stores the logger.</summary>
    private readonly ILogger<SyncSettingsStore> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SyncSettingsStore"/> class.</summary>
    /// <param name="location">Where the sync's files are.</param>
    /// <param name="logger">The logger.</param>
    public SyncSettingsStore(SyncFileLocation location, ILogger<SyncSettingsStore> logger)
    {
        ArgumentNullException.ThrowIfNull(location);
        _filePath = location.SettingsPath;
        _logger = logger;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Reads the settings.</summary>
    /// <returns>The stored settings, or the initial ones when there is no readable file.</returns>
    public SyncSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return SyncSettings.Initial;
        }

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllBytes(_filePath), SyncJsonContext.Default.SyncSettings) ?? SyncSettings.Initial;
        }
        catch (JsonException exception)
        {
            LogDamagedFile(_logger, exception);
            return SyncSettings.Initial;
        }
    }

    /// <summary>Writes the settings through a temporary file and a replace.</summary>
    /// <param name="settings">The settings.</param>
    public void Save(SyncSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + TemporarySuffix;
        File.WriteAllBytes(temporaryPath, JsonSerializer.SerializeToUtf8Bytes(settings, SyncJsonContext.Default.SyncSettings));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes that the settings file couldn't be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The sync settings file is damaged; the companion searches for WoW folders again.")]
    private static partial void LogDamagedFile(ILogger logger, Exception exception);
    #endregion Private Helpers
}
