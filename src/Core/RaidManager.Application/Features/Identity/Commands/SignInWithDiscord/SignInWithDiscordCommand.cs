using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;

/// <summary>Resolves a Discord identity, confirmed by the sign-in flow, to its single local user.</summary>
/// <param name="DiscordUserId">The Discord account identifier (a numeric snowflake).</param>
/// <param name="DisplayName">The player's current Discord display name.</param>
/// <param name="AvatarUrl">The player's current Discord avatar URL, if any.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Registers a first-time player or returns the existing user, so a returning player never gets a duplicate account.
/// The Discord identity must come from a completed OAuth exchange, never from user input.
/// </remarks>
public sealed record SignInWithDiscordCommand(string DiscordUserId, string DisplayName, string? AvatarUrl) : ICommand<Guid>;
