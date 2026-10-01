using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.ApiService.Tests.Support;

/// <summary>Stands in for Discord in API tests: one server per id, with its name, roles and members.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets API tests run every community endpoint without calling Discord, and make Discord unavailable or
/// remove the bot from a server.
/// </remarks>
public sealed class FakeDiscord : IDiscordServerMembers, IDiscordServers
{
    #region Properties
    /// <summary>Gets the servers by id.</summary>
    public Dictionary<string, (DiscordServer Server, List<DiscordServerMember> Members)> Servers { get; } = [];

    /// <summary>Gets the servers the bot was removed from: their roles and members can't be read any more.</summary>
    public HashSet<string> BotRemovedFrom { get; } = [];

    /// <summary>Gets or sets the error every call fails with, if any.</summary>
    public Error? Failure { get; set; }
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<Result<DiscordMembership>> FindAsync(string discordGuildId, string discordUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Failure is { } error
            ? Result.Failure<DiscordMembership>(error)
            : Result.Success(Servers.TryGetValue(discordGuildId, out var server) && server.Members.Find(member => member.UserId == discordUserId) is { } member
                ? DiscordMembership.Member(member.RoleIds)
                : DiscordMembership.NotMember));

    /// <inheritdoc />
    public Task<Result<DiscordServer>> GetAsync(string discordGuildId, CancellationToken cancellationToken) =>
        Task.FromResult(Failure is { } error
            ? Result.Failure<DiscordServer>(error)
            : !BotRemovedFrom.Contains(discordGuildId) && Servers.TryGetValue(discordGuildId, out var server)
                ? Result.Success(server.Server)
                : Result.Failure<DiscordServer>(DiscordErrors.BotNotInServer));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<DiscordServerMember>>> ListMembersAsync(string discordGuildId, CancellationToken cancellationToken) =>
        Task.FromResult(Failure is { } error
            ? Result.Failure<IReadOnlyList<DiscordServerMember>>(error)
            : !BotRemovedFrom.Contains(discordGuildId) && Servers.TryGetValue(discordGuildId, out var server)
                ? Result.Success<IReadOnlyList<DiscordServerMember>>([.. server.Members])
                : Result.Failure<IReadOnlyList<DiscordServerMember>>(DiscordErrors.BotNotInServer));
    #endregion Public Methods
}
