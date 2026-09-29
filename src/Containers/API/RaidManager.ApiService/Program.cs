var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapOpenApi();
app.MapGet("/", () => Results.Ok(new { service = "RaidManager.ApiService", status = "foundation" }));
app.MapDefaultEndpoints();
app.Run();
