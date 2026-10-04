using System.Reflection;

namespace RaidManager.Companion.Composition;

/// <summary>Names the environment this build of the companion talks to.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: A publish selects its environment (ADR-0032) through the <c>CompanionEnvironment</c> build property, which
/// the project writes into the assembly; the companion then reads <c>appsettings.&lt;Environment&gt;.json</c>.
/// </remarks>
internal static class CompanionEnvironment
{
    #region Constants
    /// <summary>Defines the assembly metadata key the project writes.</summary>
    private const string MetadataKey = "CompanionEnvironment";

    /// <summary>Defines the environment of a build that names none.</summary>
    private const string DefaultEnvironment = "Development";
    #endregion Constants

    #region Public Methods
    /// <summary>Reads the environment an assembly was built for.</summary>
    /// <param name="assembly">The companion's assembly.</param>
    /// <returns>The environment's name, such as <c>Dev</c>.</returns>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var environment = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == MetadataKey)?.Value;
        return string.IsNullOrWhiteSpace(environment) ? DefaultEnvironment : environment;
    }
    #endregion Public Methods
}
