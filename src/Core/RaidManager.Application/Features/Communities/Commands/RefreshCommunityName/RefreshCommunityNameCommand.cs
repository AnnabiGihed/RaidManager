using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Communities.Commands.RefreshCommunityName;

/// <summary>Takes the Discord server's current name for a community.</summary>
/// <param name="CommunityId">The community.</param>
/// <param name="Name">The server's name as Discord reports it now.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the stored server name current whenever RaidManager reads the server from Discord (owner decision on #14); an unchanged name saves nothing.
/// </remarks>
public sealed record RefreshCommunityNameCommand(Guid CommunityId, string Name) : ICommand;
