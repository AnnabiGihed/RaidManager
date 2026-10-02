namespace RaidManager.ApiService.Features.Communities;

/// <summary>Asks the API which of a user's Discord servers are linked communities.</summary>
/// <param name="DiscordGuildIds">The Discord server snowflakes, as Discord listed them at sign-in.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The API contract the website sends at sign-in (ADR-0023).
/// </remarks>
public sealed record FindCommunitiesByDiscordServersRequest(IReadOnlyList<string> DiscordGuildIds);
