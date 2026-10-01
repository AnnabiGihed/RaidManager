using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Infrastructure.Features.Communities;

/// <summary>Registers RaidManager's Discord REST access in a dependency-injection container.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Gives hosts one call that wires the bot-authenticated Discord client and its member cache (ADR-0022).
/// </remarks>
public static class DiscordServiceCollectionExtensions
{
    #region Constants
    /// <summary>Defines the User-Agent Discord requires from bots.</summary>
    private const string UserAgent = "DiscordBot (https://github.com/AnnabiGihed/RaidManager, 1.0)";
    #endregion Constants

    #region Public Methods
    /// <summary>Adds the Discord member lookup the role check uses, cached as <see cref="DiscordOptions.MemberCacheDuration"/> says.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the <c>Discord</c> section.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>The host fails at startup when the bot token is missing, rather than at the first officer action.</remarks>
    public static IServiceCollection AddRaidManagerDiscord(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DiscordOptions>()
            .Bind(configuration.GetSection(DiscordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddMemoryCache();
        services.AddHttpClient<DiscordServerMembersClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<DiscordOptions>>().Value;
            client.BaseAddress = options.ApiBaseAddress;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", options.BotToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddScoped<IDiscordServerMembers>(provider => new CachedDiscordServerMembers(
            provider.GetRequiredService<DiscordServerMembersClient>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IOptions<DiscordOptions>>()));
        return services;
    }
    #endregion Public Methods
}
