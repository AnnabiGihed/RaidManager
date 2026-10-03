using Microsoft.AspNetCore.DataProtection;

namespace RaidManager.Web.Features.Shared.Hosting;

/// <summary>Registers the keys that protect the session cookie and the community link state.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: A container loses its files when it is replaced, so a deployed website keeps its keys in a directory on a
/// volume (ADR-0027). Otherwise every deployment would sign everyone out and break pending community links.
/// </remarks>
public static class DataProtectionExtensions
{
    #region Constants
    /// <summary>Defines the configuration key of the directory that holds the keys in a deployed environment.</summary>
    public const string KeysPathKey = "DataProtection:KeysPath";

    /// <summary>Defines the application name the keys belong to, the same in every container of an environment.</summary>
    public const string ApplicationName = "RaidManager.Web";
    #endregion Constants

    #region Public Methods
    /// <summary>Adds Data Protection, keeping its keys in the configured directory when one is set.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration that may hold <see cref="KeysPathKey"/>.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>Without a directory, as in the local run, the keys stay in the user profile as before.</remarks>
    public static IServiceCollection AddRaidManagerDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName(ApplicationName);
        var keysPath = configuration[KeysPathKey];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        return services;
    }
    #endregion Public Methods
}
