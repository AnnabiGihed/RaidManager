using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

namespace RaidManager.AppHost.Deployment;

/// <summary>Shapes the Docker Compose project that each deployed environment runs (ADR-0027, ADR-0029).</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: The same app model runs locally and on the server. These settings apply only when the AppHost publishes,
/// so the local run is unchanged; values that differ per environment become <c>.env</c> keys the deploy workflow fills.
/// </remarks>
internal static class DeploymentExtensions
{
    #region Constants
    /// <summary>Defines the external network the shared Caddy reaches the websites and APIs on.</summary>
    public const string SharedNetwork = "web";

    /// <summary>Defines the network Aspire puts every service of the Compose project on.</summary>
    public const string ProjectNetwork = "aspire";

    /// <summary>Defines the parameter holding the environment's short name in container names: dev, test or prod.</summary>
    public const string EnvironmentSlugParameter = "deploy-environment";

    /// <summary>Defines the parameter holding the ASP.NET Core environment name: Dev, Test or Production.</summary>
    public const string AspNetCoreEnvironmentParameter = "aspnetcore-environment";

    /// <summary>Defines the parameter holding PostgreSQL's container memory limit, such as 192M.</summary>
    public const string PostgresMemoryLimitParameter = "postgres-memory-limit";

    /// <summary>Defines the parameter holding PostgreSQL's shared buffers, such as 64MB.</summary>
    public const string PostgresSharedBuffersParameter = "postgres-shared-buffers";

    /// <summary>Defines the volume that keeps the website's Data Protection keys.</summary>
    public const string DataProtectionVolume = "data-protection";

    /// <summary>
    /// Defines the database's volume when published. A fixed name, because the generated one hashes the folder the
    /// AppHost is built in, and a new hash on another build machine would start an empty database.
    /// </summary>
    public const string PostgresDataVolume = "postgres-data";

    /// <summary>Defines where the website's Data Protection keys live inside its container.</summary>
    public const string DataProtectionKeysPath = "/home/app/data-protection-keys";

    /// <summary>Defines the restart policy of every deployed service.</summary>
    private const string RestartPolicy = "unless-stopped";

    /// <summary>
    /// Defines the folder the website's key volume is mounted on. The images run as the non-root <c>app</c> user, and
    /// Docker gives a new volume the owner of its mount folder only when the image already has it, as here.
    /// </summary>
    private const string WebsiteHomeFolder = "/home/app";
    #endregion Constants

    #region Public Methods
    /// <summary>Adds the Compose environment: no dashboard (ADR-0029), and the shared network as an external one.</summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The Compose environment.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> AddDeploymentEnvironment(this IDistributedApplicationBuilder builder) =>
        builder.AddDockerComposeEnvironment("raidmanager")
            .WithDashboard(false)
            .ConfigureComposeFile(file =>
            {
                file.AddNetwork(new Network { Name = SharedNetwork, External = true });
                file.AddVolume(new Volume { Name = DataProtectionVolume });
            });

    /// <summary>Publishes the website or the API as reachable by the shared Caddy under its environment's name.</summary>
    /// <param name="project">The project.</param>
    /// <param name="role">The name Caddy's site forwards to: <c>web</c> or <c>api</c>.</param>
    /// <param name="slug">The environment's short name.</param>
    /// <param name="environmentName">The ASP.NET Core environment name.</param>
    /// <returns>The same project.</returns>
    public static IResourceBuilder<ProjectResource> PublishBehindTheSharedProxy(
        this IResourceBuilder<ProjectResource> project,
        string role,
        IResourceBuilder<ParameterResource> slug,
        IResourceBuilder<ParameterResource> environmentName) =>
        project
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
            .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
            .PublishAsDockerComposeService((resource, service) =>
            {
                // Caddy's sites forward to raidmanager-<environment>-web and -api (ADR-0027).
                service.ContainerName = $"raidmanager-{slug.AsEnvironmentPlaceholder(resource)}-{role}";
                service.Networks = [ProjectNetwork, SharedNetwork];
                service.Restart = RestartPolicy;
            });

    /// <summary>Keeps the website's Data Protection keys on a volume, so a deployment signs nobody out (#388).</summary>
    /// <param name="website">The website project.</param>
    /// <returns>The same project.</returns>
    public static IResourceBuilder<ProjectResource> KeepDataProtectionKeys(this IResourceBuilder<ProjectResource> website) =>
        website
            .WithEnvironment("DataProtection__KeysPath", DataProtectionKeysPath)
            .PublishAsDockerComposeService((_, service) => service.AddVolume(new Volume
            {
                Name = DataProtectionVolume,
                Source = DataProtectionVolume,
                Target = WebsiteHomeFolder,
                Type = "volume",
            }));

    /// <summary>Caps PostgreSQL's memory as ADR-0029 plans for each environment.</summary>
    /// <param name="postgres">The PostgreSQL server.</param>
    /// <param name="memoryLimit">The container memory limit.</param>
    /// <param name="sharedBuffers">The shared buffers.</param>
    /// <returns>The same server.</returns>
    public static IResourceBuilder<PostgresServerResource> CapMemory(
        this IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<ParameterResource> memoryLimit,
        IResourceBuilder<ParameterResource> sharedBuffers) =>
        postgres.PublishAsDockerComposeService((resource, service) =>
        {
            service.Command = ["postgres", "-c", $"shared_buffers={sharedBuffers.AsEnvironmentPlaceholder(resource)}"];
            service.Deploy = new Deploy { Resources = new Resources { Limits = new ResourceSpec { Memory = memoryLimit.AsEnvironmentPlaceholder(resource) } } };
            service.Restart = RestartPolicy;
        });

    /// <summary>Restarts a service that has no public name, such as the bot, if it stops.</summary>
    /// <param name="project">The project.</param>
    /// <returns>The same project.</returns>
    public static IResourceBuilder<ProjectResource> RestartUnlessStopped(this IResourceBuilder<ProjectResource> project) =>
        project.PublishAsDockerComposeService((_, service) => service.Restart = RestartPolicy);
    #endregion Public Methods
}
