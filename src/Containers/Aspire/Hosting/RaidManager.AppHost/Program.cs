var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var database = sql.AddDatabase("Database", "RaidManager");

var api = builder.AddProject<Projects.RaidManager_ApiService>("api")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.RaidManager_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.AddProject<Projects.RaidManager_DiscordBot>("discord-bot")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
