using Radzen;
using RaidManager.ViewModels.Features.Authentication;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.Web.Components;
using RaidManager.Web.Features.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddRaidManagerAuthentication(builder.Configuration);
builder.Services.AddScoped<SignInFailedViewModel>();
builder.Services.AddScoped<CharacterReviewViewModel>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapAuthenticationEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapDefaultEndpoints();
await app.RunAsync();

/// <summary>Exposes the website entry point to the integration tests' web application factory.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets <c>WebApplicationFactory&lt;Program&gt;</c> host the real website in tests.
/// </remarks>
public partial class Program;
