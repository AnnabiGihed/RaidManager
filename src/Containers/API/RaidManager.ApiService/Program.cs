using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Application.Behaviors;
using RaidManager.ApiService.Features.Identity;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.OpenApi;
using RaidManager.Persistence.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<WebsiteServiceKeyTransformer>();
    options.AddOperationTransformer<WebsiteServiceKeyTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(RaidManager.Application.AssemblyReference.Assembly);
    configuration.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(RaidManager.Application.AssemblyReference.Assembly, includeInternalTypes: true);
builder.Services.AddRaidManagerPersistence(
    builder.Configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Configure the 'Database' connection string; the Aspire AppHost provides it."));
builder.Services.AddWebsiteServiceAuthentication(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Local development and tests start from an empty database; deployments apply migrations as a release step.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<RaidManagerDbContext>().Database.MigrateAsync();
}

app.MapOpenApi();
app.MapGet("/", () => Results.Ok(new { service = "RaidManager.ApiService", status = "foundation" }));
app.MapIdentityEndpoints();
app.MapDefaultEndpoints();
await app.RunAsync();

/// <summary>Exposes the API entry point to the integration tests' web application factory.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets <c>WebApplicationFactory&lt;Program&gt;</c> host the real API in tests.
/// </remarks>
public partial class Program;
