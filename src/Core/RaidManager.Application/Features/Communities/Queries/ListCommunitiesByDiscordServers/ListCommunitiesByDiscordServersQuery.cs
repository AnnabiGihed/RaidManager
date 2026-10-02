using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Communities.Queries.ListCommunitiesByDiscordServers;

/// <summary>Requests the communities a set of Discord servers link to.</summary>
/// <param name="DiscordGuildIds">The Discord server snowflakes.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Lets the website find, at sign-in, the communities of the servers a user is in (ADR-0023).
/// </remarks>
public sealed record ListCommunitiesByDiscordServersQuery(IReadOnlyList<string> DiscordGuildIds) : IQuery<IReadOnlyList<CommunitySummaryResponse>>;
