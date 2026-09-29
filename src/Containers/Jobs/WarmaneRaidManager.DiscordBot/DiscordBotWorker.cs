namespace WarmaneRaidManager.DiscordBot;

/// <summary>Hosts the future Discord bot connection and interaction loop.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Reserves the Discord bot process boundary while all raid behavior remains in Application and Domain layers.
/// </remarks>
public sealed class DiscordBotWorker : BackgroundService
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
        _logger.LogInformation("Discord bot host started; interaction gateway is not configured yet.");
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
    #endregion Protected Methods
}
