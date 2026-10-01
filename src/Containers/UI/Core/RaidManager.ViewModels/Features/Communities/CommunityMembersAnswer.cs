namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Holds the API's answer to listing a community's members.</summary>
/// <param name="Status">How the API answered.</param>
/// <param name="Members">The members when the read succeeded.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the members page tell a refusal and Discord's failures apart from its content.
/// </remarks>
public sealed record CommunityMembersAnswer(CommunityApiStatus Status, CommunityMembers? Members);
