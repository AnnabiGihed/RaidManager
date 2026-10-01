namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the API's answer to reading a community's officer roles.</summary>
/// <param name="Status">How the API answered.</param>
/// <param name="Settings">The roles card when the read succeeded.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the roles card tell a refusal and Discord's failures apart from its content.
/// </remarks>
public sealed record CommunityRoleSettingsAnswer(CommunityApiStatus Status, CommunityRoleSettings? Settings);
