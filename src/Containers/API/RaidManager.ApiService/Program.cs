using FluentValidation;
using Pivot.Framework.Application.Behaviors;
using RaidManager.ApiService.Features.Characters;
using RaidManager.ApiService.Features.Communities;
using RaidManager.ApiService.Features.Companions;
using RaidManager.ApiService.Features.Identity;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Hosting;
using RaidManager.ApiService.Features.Shared.OpenApi;
using RaidManager.Infrastructure.Features.Communities;
using RaidManager.Infrastructure.Features.Companions;
using RaidManager.Persistence.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<WebsiteServiceKeyTransformer>();
    options.AddOperationTransformer<WebsiteServiceKeyTransformer>();
    options.AddDocumentTransformer<CompanionTokenTransformer>();
    options.AddOperationTransformer<CompanionTokenTransformer>();
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
builder.Services.AddRaidManagerDiscord(builder.Configuration);
builder.Services.AddRaidManagerCompanions();
builder.Services.AddWebsiteServiceAuthentication(builder.Configuration);
builder.Services.AddCompanionTokenAuthentication();
builder.Services.AddCompanionRateLimits(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Local development and tests start from an empty database; a deployment runs the migrations as a step of its own.
var migrateAndExit = app.Configuration.GetValue<bool>(DatabaseMigration.MigrateAndExitKey);
if (app.Environment.IsDevelopment() || migrateAndExit)
{
    await DatabaseMigration.MigrateAsync(app.Services);
}

if (migrateAndExit)
{
    return;
}

app.MapOpenApi();

// The interactive reference is a developer tool; other environments publish only the OpenAPI document (ADR-0013).
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options => options
        .WithTitle("RaidManager API")
        .AddPreferredSecuritySchemes(WebsiteServiceDefaults.Scheme, CompanionTokenDefaults.Scheme));
}

app.MapGet("/", () => Results.Ok(new { service = "RaidManager.ApiService", status = "foundation" }));
app.MapIdentityEndpoints();
app.MapCharacterClaimEndpoints();
app.MapCharacterProfileEndpoints();
app.MapCommunityEndpoints();
app.MapCompanionEndpoints();
app.MapCompanionManagementEndpoints();
app.MapDefaultEndpoints();
await app.RunAsync();

/// <summary>Exposes the API entry point to the integration tests' web application factory.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets <c>WebApplicationFactory&lt;Program&gt;</c> host the real API in tests.
/// </remarks>
public partial class Program;
