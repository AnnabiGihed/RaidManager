using System.ComponentModel.DataAnnotations;

namespace RaidManager.Infrastructure.Features.Communities;

/// <summary>Configures how RaidManager calls Discord's REST API with its bot.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Binds the <c>Discord</c> configuration section. The Aspire AppHost passes the bot token from the
/// <c>discord-bot-token</c> secret parameter; it is never kept in a settings file (ADR-0022).
/// </remarks>
public sealed class DiscordOptions
{
    #region Constants
    /// <summary>Defines the configuration section bound to these options.</summary>
    public const string SectionName = "Discord";
    #endregion Constants

    #region Properties
    /// <summary>Gets or sets the bot token used to ask Discord about server members.</summary>
    [Required]
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the address of Discord's REST API, version included.</summary>
    [Required]
    public Uri ApiBaseAddress { get; set; } = new("https://discord.com/api/v10/");

    /// <summary>Gets or sets how long a member's Discord answer is reused before Discord is asked again.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan MemberCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
    #endregion Properties
}
