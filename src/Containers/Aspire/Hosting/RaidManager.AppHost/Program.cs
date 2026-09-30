var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var database = sql.AddDatabase("Database", "RaidManager");

// The website proves itself to the API with this key (ADR-0011). Aspire generates it once and keeps it in user secrets.
var websiteServiceKey = builder.AddParameter(
    "website-service-key",
    new GenerateParameterDefault { MinLength = 48, Special = false },
    secret: true,
    persist: true);

var api = builder.AddProject<Projects.RaidManager_ApiService>("api")
    .WithReference(database)
    .WithEnvironment("Website__ServiceKey", websiteServiceKey)
    .WaitFor(database);

// The Discord application's OAuth2 credentials; keep them in the AppHost user secrets (README, "Sign in with Discord").
var discordClientId = builder.AddParameter("discord-client-id");
var discordClientSecret = builder.AddParameter("discord-client-secret", secret: true);

builder.AddProject<Projects.RaidManager_Web>("web")
    .WithReference(api)
    .WithEnvironment("Website__ServiceKey", websiteServiceKey)
    .WithEnvironment("Authentication__Discord__ClientId", discordClientId)
    .WithEnvironment("Authentication__Discord__ClientSecret", discordClientSecret)
    .WaitFor(api);

builder.AddProject<Projects.RaidManager_DiscordBot>("discord-bot")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
