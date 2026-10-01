using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.ViewModels.Tests.Features.Communities;

/// <summary>Stands in for the API's community endpoints in view model tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets tests give users communities, make calls fail, and see what was linked.
/// </remarks>
internal sealed class FakeCommunitiesApi : ICommunitiesApiClient
{
    #region Properties
    /// <summary>Gets the linked communities.</summary>
    public List<CommunitySummary> Communities { get; } = [];

    /// <summary>Gets or sets the exception every call throws, if any.</summary>
    public Exception? Failure { get; set; }

    /// <summary>Gets or sets a value indicating whether linking answers 409 even for a new server.</summary>
    public bool ConflictOnLink { get; set; }

    /// <summary>Gets the links asked for, with their realm.</summary>
    public List<(PendingCommunityLink Link, string Realm)> Links { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Creates a community.</summary>
    /// <param name="administratorId">The Administrator.</param>
    /// <param name="guildId">The Discord server snowflake.</param>
    /// <returns>The community.</returns>
    public static CommunitySummary Community(Guid administratorId, string guildId = "987") =>
        new(Guid.NewGuid(), guildId, "Dark Templars", "Icecrown", administratorId, "Gihed Annabi");

    /// <inheritdoc />
    public Task<IReadOnlyList<CommunitySummary>> GetUserCommunitiesAsync(Guid userId, CancellationToken cancellationToken) =>
        Answer<IReadOnlyList<CommunitySummary>>([.. Communities.Where(community => community.AdministratorId == userId)]);

    /// <inheritdoc />
    public Task<CommunitySummary?> GetAsync(Guid communityId, CancellationToken cancellationToken) =>
        Answer(Communities.Find(community => community.CommunityId == communityId));

    /// <inheritdoc />
    public Task<CommunitySummary?> FindByDiscordServerAsync(string discordGuildId, CancellationToken cancellationToken) =>
        Answer(Communities.Find(community => community.DiscordGuildId == discordGuildId));

    /// <inheritdoc />
    public Task<Guid?> LinkAsync(PendingCommunityLink link, string realm, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Links.Add((link, realm));
        if (ConflictOnLink || Communities.Exists(community => community.DiscordGuildId == link.DiscordGuildId))
        {
            return Task.FromResult<Guid?>(null);
        }

        var community = new CommunitySummary(Guid.NewGuid(), link.DiscordGuildId, link.ServerName, realm, link.UserId, "Gihed Annabi");
        Communities.Add(community);
        return Task.FromResult<Guid?>(community.CommunityId);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Answers a call, or throws the set failure.</summary>
    /// <typeparam name="T">The answer's type.</typeparam>
    /// <param name="value">The value to answer with.</param>
    /// <returns>A task with the value.</returns>
    private Task<T> Answer<T>(T value) => Failure is not null ? throw Failure : Task.FromResult(value);
    #endregion Private Helpers
}
