namespace RaidManager.ApiService.Features.Identity;

/// <summary>Returns the local user a Discord identity resolved to.</summary>
/// <param name="UserId">The local user identifier the website stores in its session.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The same Discord account always yields the same user identifier.
/// </remarks>
public sealed record DiscordSignInResponse(Guid UserId);
