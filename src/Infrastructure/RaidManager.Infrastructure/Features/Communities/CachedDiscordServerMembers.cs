using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Communities.Abstractions;

namespace RaidManager.Infrastructure.Features.Communities;

/// <summary>Reuses Discord's answer about a member for a short time before asking Discord again.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps Discord calls to about one per active member a minute while a removed role still stops counting within
/// that minute (ADR-0022). Only answers are cached; a failure is never reused, so the next check asks Discord again.
/// </remarks>
internal sealed class CachedDiscordServerMembers : IDiscordServerMembers
{
    #region Fields
    /// <summary>Stores the lookup that asks Discord.</summary>
    private readonly IDiscordServerMembers _inner;

    /// <summary>Stores the process-wide memory cache.</summary>
    private readonly IMemoryCache _cache;

    /// <summary>Stores how long an answer is reused.</summary>
    private readonly TimeSpan _duration;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CachedDiscordServerMembers"/> class.</summary>
    /// <param name="inner">The lookup that asks Discord.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="options">The Discord options, which give the cache duration.</param>
    public CachedDiscordServerMembers(IDiscordServerMembers inner, IMemoryCache cache, IOptions<DiscordOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _duration = options.Value.MemberCacheDuration;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<Result<DiscordMembership>> FindAsync(string discordGuildId, string discordUserId, CancellationToken cancellationToken)
    {
        var key = $"discord-member:{discordGuildId}:{discordUserId}";
        if (_cache.TryGetValue(key, out DiscordMembership? cached) && cached is not null)
        {
            return Result.Success(cached);
        }

        var answer = await _inner.FindAsync(discordGuildId, discordUserId, cancellationToken);
        if (answer.IsSuccess)
        {
            _cache.Set(key, answer.Value, _duration);
        }

        return answer;
    }
    #endregion Public Methods
}
