namespace RaidManager.ApiService.Features.Identity;

/// <summary>Carries a Discord identity the website has confirmed through OAuth.</summary>
/// <param name="DiscordUserId">The Discord account identifier (a numeric snowflake).</param>
/// <param name="DisplayName">The player's current Discord display name.</param>
/// <param name="AvatarUrl">The player's current Discord avatar URL, if any.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The body of the website-only sign-in endpoint; only the website can call it, after a completed OAuth exchange.
/// </remarks>
public sealed record DiscordSignInRequest(string DiscordUserId, string DisplayName, string? AvatarUrl);
