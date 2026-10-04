using Microsoft.Extensions.DependencyInjection;
using RaidManager.Application.Features.Companions.Abstractions;

namespace RaidManager.Infrastructure.Features.Companions;

/// <summary>Registers the companion pairing services in a dependency-injection container.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives hosts one call for the cryptographic generator the pairing handlers use.
/// </remarks>
public static class CompanionServiceCollectionExtensions
{
    #region Public Methods
    /// <summary>Adds the companion secret and code generator.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddRaidManagerCompanions(this IServiceCollection services)
    {
        services.AddSingleton<ICompanionCredentialGenerator, CompanionCredentialGenerator>();
        return services;
    }
    #endregion Public Methods
}
