using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Identity.Events;

/// <summary>Signals that a user Discord profile was refreshed.</summary>
/// <param name="UserId">The application user identifier.</param>
/// <param name="DisplayName">The Discord display name.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps user-facing identity projections synchronized with Discord.
/// </remarks>
public sealed record DiscordProfileUpdated(UserId UserId, string DisplayName) : DomainEvent;
