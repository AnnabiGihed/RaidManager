using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Identity.Events;

/// <summary>Signals that a Discord-authenticated user profile was registered.</summary>
/// <param name="UserId">The application user identifier.</param>
/// <param name="DiscordUserId">The Discord snowflake identifier.</param>
/// <param name="DisplayName">The Discord display name.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows projections and integrations to react to first-time Discord authentication.
/// </remarks>
public sealed record UserRegistered(UserId UserId, string DiscordUserId, string DisplayName) : DomainEvent;
