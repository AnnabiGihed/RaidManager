namespace RaidManager.Web.Features.Communities;

/// <summary>Groups the services Discord's return endpoint uses.</summary>
/// <param name="Protector">The link protector.</param>
/// <param name="Discord">The Discord install client.</param>
/// <param name="Api">The API's community client.</param>
/// <param name="Logger">The logger.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the endpoint's parameter list short; ASP.NET Core binds it with <c>[AsParameters]</c>.
/// </remarks>
public sealed record ReturnServices(
    CommunityLinkProtector Protector,
    IDiscordInstallClient Discord,
    ViewModels.Features.Communities.ICommunitiesApiClient Api,
    ILogger<ReturnServices> Logger);
