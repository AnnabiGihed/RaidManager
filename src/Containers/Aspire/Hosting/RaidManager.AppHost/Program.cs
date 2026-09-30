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

builder.AddProject<Projects.RaidManager_Web>("web")
    .WithReference(api)
    .WithEnvironment("Website__ServiceKey", websiteServiceKey)
    .WaitFor(api);

builder.AddProject<Projects.RaidManager_DiscordBot>("discord-bot")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
