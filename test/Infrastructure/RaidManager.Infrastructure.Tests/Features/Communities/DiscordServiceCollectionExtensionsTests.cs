using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Infrastructure.Features.Communities;
using RaidManager.Infrastructure.Tests.Support;

namespace RaidManager.Infrastructure.Tests.Features.Communities;

/// <summary>Verifies how hosts get the Discord member lookup.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The lookup is cached, calls Discord as the bot, and can't be configured without a bot token.
/// </remarks>
public sealed class DiscordServiceCollectionExtensionsTests
{
    #region Constants
    /// <summary>Defines a placeholder bot token for the registered client.</summary>
    private const string PlaceholderBotToken = "not-a-real-bot-token";
    #endregion Constants

    #region Tests
    /// <summary>Calls Discord as the bot, with Discord's required User-Agent, through the cache.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task TheLookupCallsDiscordAsTheBotThroughTheCache()
    {
        var discord = new StubDiscordHandler().Answering(HttpStatusCode.OK, """{"roles":["111"]}""");
        await using var provider = BuildProvider(new Dictionary<string, string?> { ["Discord:BotToken"] = PlaceholderBotToken }, discord);
        await using var scope = provider.CreateAsyncScope();
        var lookup = scope.ServiceProvider.GetRequiredService<IDiscordServerMembers>();

        var result = await lookup.FindAsync("123", "456", CancellationToken.None);
        await lookup.FindAsync("123", "456", CancellationToken.None);

        result.Value.RoleIds.ShouldBe(["111"]);
        lookup.ShouldBeOfType<CachedDiscordServerMembers>();
        var request = discord.Requests.ShouldHaveSingleItem();
        request.RequestUri.ShouldBe(new Uri("https://discord.com/api/v10/guilds/123/members/456"));
        request.Headers.Authorization!.Scheme.ShouldBe("Bot");
        request.Headers.Authorization.Parameter.ShouldBe(PlaceholderBotToken);
        request.Headers.UserAgent.ToString().ShouldStartWith("DiscordBot (");
    }

    /// <summary>Refuses to start without a bot token.</summary>
    [Fact]
    public void AMissingBotTokenIsRejected()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>(), new StubDiscordHandler());

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DiscordOptions>>().Value);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a container the way a host does, with the stub in place of Discord.</summary>
    /// <param name="settings">The configuration settings.</param>
    /// <param name="discord">The stub standing in for Discord.</param>
    /// <returns>The service provider.</returns>
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings, StubDiscordHandler discord)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRaidManagerDiscord(configuration);
        services.AddHttpClient<DiscordServerMembersClient>().ConfigurePrimaryHttpMessageHandler(() => discord);
        return services.BuildServiceProvider();
    }
    #endregion Private Helpers
}
