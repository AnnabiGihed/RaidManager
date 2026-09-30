using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Support;

/// <summary>Starts one SQL Server container for the persistence tests and applies the RaidManager migrations to it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Tests persistence against the real database engine; the conventions forbid in-memory providers.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    #region Constants
    /// <summary>Defines the SQL Server image, pinned to the major version the Aspire AppHost runs.</summary>
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
    #endregion Constants

    #region Fields
    /// <summary>Stores the SQL Server container.</summary>
    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    /// <summary>Stores the root service provider built over the container.</summary>
    private ServiceProvider? _services;
    #endregion Fields

    #region Properties
    /// <summary>Gets the connection string of the migrated test database.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Gets the root service provider, wired as a host would wire RaidManager.</summary>
    public IServiceProvider Services => _services ?? throw new InvalidOperationException("The SQL Server fixture has not started.");
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly));
        services.AddRaidManagerPersistence(ConnectionString);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>Sends a command through MediatR in a new scope, as a request would.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <returns>The handler's response.</returns>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }
    #endregion Public Methods
}
