var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var database = sql.AddDatabase("Database", "RaidManager");

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
