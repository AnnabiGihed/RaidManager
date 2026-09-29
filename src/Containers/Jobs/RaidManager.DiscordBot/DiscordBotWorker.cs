namespace RaidManager.DiscordBot;

/// <summary>Hosts the future Discord bot connection and interaction loop.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Reserves the Discord bot process boundary while all raid behavior remains in Application and Domain layers.
/// </remarks>
public sealed partial class DiscordBotWorker : BackgroundService
{
    #region Fields
    /// <summary>Writes lifecycle information for the Discord bot host.</summary>
    private readonly ILogger<DiscordBotWorker> _logger;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordBotWorker"/> class.</summary>
    /// <param name="logger">The structured logger.</param>
    public DiscordBotWorker(ILogger<DiscordBotWorker> logger)
    {
        _logger = logger;
    }
    #endregion Constructors

    #region Protected Methods
    /// <summary>Keeps the bot host alive until the Discord interaction slice is implemented.</summary>
    /// <param name="stoppingToken">A token that is cancelled when the host is stopping.</param>
    /// <returns>A task representing the background worker lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogDiscordBotHostStarted(_logger);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
    #endregion Protected Methods

    #region Private Methods
    /// <summary>Logs that the Discord bot placeholder host has started.</summary>
    /// <param name="logger">The structured logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Discord bot host started; interaction gateway is not configured yet.")]
    private static partial void LogDiscordBotHostStarted(ILogger logger);
    #endregion Private Methods
}
