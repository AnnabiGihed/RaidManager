using Radzen;
using RaidManager.ViewModels.Features.Authentication;
using RaidManager.ViewModels.Features.Characters;
using RaidManager.ViewModels.Features.Communities;
using RaidManager.ViewModels.Features.Companions;
using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Components;
using RaidManager.Web.Features.Authentication;
using RaidManager.Web.Features.Communities;
using RaidManager.Web.Features.Shared.Hosting;
using RaidManager.Web.Features.Shared.Layout;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddRaidManagerDataProtection(builder.Configuration);
builder.Services.AddRaidManagerAuthentication(builder.Configuration);
builder.Services.AddScoped<SignInFailedViewModel>();
builder.Services.AddScoped<CharacterReviewViewModel>();
builder.Services.AddTransient<MyCharactersViewModel>();
builder.Services.AddTransient<CharacterProfileViewModel>();
builder.Services.AddScoped<OverviewViewModel>();
builder.Services.AddScoped<ChooseRealmViewModel>();
builder.Services.AddTransient<CommunityViewModel>();
builder.Services.AddTransient<CommunityRolesViewModel>();
builder.Services.AddTransient<CommunityMembersViewModel>();
builder.Services.AddScoped<ShellCommunityViewModel>();
builder.Services.AddTransient<PairCompanionViewModel>();
builder.Services.AddTransient<PairedCompanionsViewModel>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddShellNavigation();

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
app.MapCommunityLinkEndpoints();
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
