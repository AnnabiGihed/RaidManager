using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.GetCommunityByDiscordServer;

/// <summary>Requests the community a Discord server links to.</summary>
/// <param name="DiscordGuildId">The Discord server snowflake.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Tells the website, when the bot was just added, whether the server is already linked (board 3).
/// </remarks>
public sealed record GetCommunityByDiscordServerQuery(string DiscordGuildId) : IQuery<CommunitySummaryResponse>;
