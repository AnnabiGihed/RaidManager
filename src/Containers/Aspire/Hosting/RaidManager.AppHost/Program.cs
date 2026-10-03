using RaidManager.AppHost.Deployment;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(builder.ExecutionContext.IsPublishMode ? DeploymentExtensions.PostgresDataVolume : null);
var database = postgres.AddDatabase("Database", "raidmanager");

// The website proves itself to the API with this key (ADR-0011). Aspire generates it once and keeps it in user secrets.
var websiteServiceKey = builder.AddParameter(
    "website-service-key",
    new GenerateParameterDefault { MinLength = 48, Special = false },
    secret: true,
    persist: true);

// The Discord bot's token; the API asks Discord for members' roles with it (ADR-0022). Keep it in the AppHost user secrets.
var discordBotToken = builder.AddParameter("discord-bot-token", secret: true);

var api = builder.AddProject<Projects.RaidManager_ApiService>("api")
    .WithReference(database)
    .WithEnvironment("Website__ServiceKey", websiteServiceKey)
    .WithEnvironment("Discord__BotToken", discordBotToken)
    .WithUrlForEndpoint("https", url => url.DisplayText = "API (https)")
    .WaitFor(database);

api.WithUrl(ReferenceExpression.Create($"{api.GetEndpoint("https")}/scalar"), "API reference");

// The Discord application's OAuth2 credentials; keep them in the AppHost user secrets (README, "Sign in with Discord").
var discordClientId = builder.AddParameter("discord-client-id");
var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

var web = builder.AddProject<Projects.RaidManager_Web>("web")
    .WithReference(api)
    .WithEnvironment("Website__ServiceKey", websiteServiceKey)
    .WithEnvironment("Authentication__Discord__ClientId", discordClientId)
    .WithEnvironment("Authentication__Discord__ClientSecret", discordClientSecret)
    .WaitFor(api);

var bot = builder.AddProject<Projects.RaidManager_DiscordBot>("discord-bot")
    .WithReference(api)
    .WaitFor(api);

// Each deployed environment runs the Compose project this app model publishes (ADR-0027); the local run is unchanged.
if (builder.ExecutionContext.IsPublishMode)
{
    builder.AddDeploymentEnvironment();
    var slug = builder.AddParameter(DeploymentExtensions.EnvironmentSlugParameter);
    var environmentName = builder.AddParameter(DeploymentExtensions.AspNetCoreEnvironmentParameter);
    postgres.CapMemory(
        builder.AddParameter(DeploymentExtensions.PostgresMemoryLimitParameter),
        builder.AddParameter(DeploymentExtensions.PostgresSharedBuffersParameter));
    api.PublishBehindTheSharedProxy("api", slug, environmentName);
    web.PublishBehindTheSharedProxy("web", slug, environmentName).KeepDataProtectionKeys();
    bot.RestartUnlessStopped();
}

builder.Build().Run();
