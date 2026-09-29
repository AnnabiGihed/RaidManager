var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var database = sql.AddDatabase("Database", "WarmaneRaidManager");

var api = builder.AddProject<Projects.WarmaneRaidManager_ApiService>("api")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.WarmaneRaidManager_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.AddProject<Projects.WarmaneRaidManager_DiscordBot>("discord-bot")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
