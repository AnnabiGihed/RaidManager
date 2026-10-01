using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaidManager.Application.Features.Communities.Abstractions;
using Testcontainers.MsSql;
using Xunit;

namespace RaidManager.ApiService.Tests.Support;

/// <summary>Hosts the real API against a SQL Server container with a known website key.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Tests the API as the website calls it: real authentication, real MediatR pipeline and a real database.
/// </remarks>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    #region Constants
    /// <summary>Defines the website key the tests present; long enough to pass the startup check.</summary>
    public const string WebsiteServiceKey = "test-website-service-key-0123456789abcdefghijkl";

    /// <summary>Defines a placeholder bot token: startup requires one, and these tests never call Discord.</summary>
    private const string PlaceholderBotToken = "not-a-real-bot-token";

    /// <summary>Defines the SQL Server image, matching the persistence tests.</summary>
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
    #endregion Constants

    #region Fields
    /// <summary>Stores the SQL Server container.</summary>
    private readonly MsSqlContainer _database = new MsSqlBuilder(SqlServerImage).Build();
    #endregion Fields

    #region Properties
    /// <summary>Gets the fake Discord every Discord call goes to.</summary>
    public FakeDiscord Discord { get; } = new();

    /// <summary>Gets the hosting environment the API runs in; Development applies the migrations at startup.</summary>
    protected virtual string EnvironmentName => "Development";
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public async Task InitializeAsync() => await _database.StartAsync();

    /// <inheritdoc />
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Website:ServiceKey", WebsiteServiceKey);
        builder.UseSetting("Discord:BotToken", PlaceholderBotToken);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDiscordServerMembers>();
            services.AddSingleton<IDiscordServerMembers>(Discord);
            services.RemoveAll<IDiscordServers>();
            services.AddSingleton<IDiscordServers>(Discord);
        });
    }
    #endregion Overrides
}
