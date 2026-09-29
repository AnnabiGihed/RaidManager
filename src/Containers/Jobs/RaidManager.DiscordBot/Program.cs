using RaidManager.DiscordBot;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<DiscordBotWorker>();

var host = builder.Build();
await host.RunAsync();
