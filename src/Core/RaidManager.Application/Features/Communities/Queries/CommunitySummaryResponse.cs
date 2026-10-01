using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Features.Communities.Queries;

/// <summary>Describes a linked community as the website shows it: its server, realm and Administrator.</summary>
/// <param name="CommunityId">The community identifier.</param>
/// <param name="DiscordGuildId">The linked Discord server snowflake.</param>
/// <param name="Name">The community name, taken from the Discord server.</param>
/// <param name="Realm">The Warmane realm the community raids on.</param>
/// <param name="AdministratorId">The user who added the bot.</param>
/// <param name="AdministratorName">The Administrator's display name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The one read shape of a community, shared by the community queries (ADR-0010).
/// </remarks>
public sealed record CommunitySummaryResponse(
    Guid CommunityId,
    string DiscordGuildId,
    string Name,
    WarmaneRealm Realm,
    Guid AdministratorId,
    string AdministratorName);
