namespace Microsoft.Extensions.Hosting;

/// <summary>Tells whether an environment offers the tools that reset test data.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: "Remove all my characters" exists on a developer's machine (Development) and in the dev and test
/// environments only, never in production (owner decision on #597). The API maps its endpoint and the website shows its
/// action only where this says so; deployments set the environment name (<c>Dev</c> on dev, see deploy-dev.yml).
/// </remarks>
public static class TestEnvironments
{
    #region Constants
    /// <summary>Names the dev environment.</summary>
    public const string Dev = "Dev";

    /// <summary>Names the test environment.</summary>
    public const string Test = "Test";
    #endregion Constants

    #region Public Methods
    /// <summary>Determines whether the environment offers the tools that reset test data.</summary>
    /// <param name="environment">The host environment.</param>
    /// <returns><see langword="true"/> on Development, Dev and Test; <see langword="false"/> anywhere else.</returns>
    public static bool OffersTestTools(this IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.IsDevelopment() || environment.IsEnvironment(Dev) || environment.IsEnvironment(Test);
    }
    #endregion Public Methods
}
