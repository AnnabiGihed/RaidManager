using RaidManager.ViewModels.Features.Communities;

namespace RaidManager.Web.Tests.Support;

/// <summary>Stands in for the API's community endpoints in website tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets tests give a user communities, make the API fail, and see what the website asked to link.
/// </remarks>
public sealed class FakeCommunitiesApiClient : ICommunitiesApiClient
{
    #region Properties
    /// <summary>Gets the linked communities.</summary>
    public List<CommunitySummary> Communities { get; } = [];

    /// <summary>Gets or sets a value indicating whether every call fails as an unreachable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets the links the website asked for, with their realm.</summary>
    public List<(PendingCommunityLink Link, string Realm)> Links { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Creates a community administered by a user.</summary>
    /// <param name="administratorId">The Administrator.</param>
    /// <param name="name">The community name.</param>
    /// <param name="realm">The realm's name.</param>
    /// <returns>The community.</returns>
    public static CommunitySummary Community(Guid administratorId, string name = "Dark Templars", string realm = "Icecrown") =>
        new(Guid.NewGuid(), "123456789012345678", name, realm, administratorId, "Gihed Annabi");

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
        ArgumentNullException.ThrowIfNull(link);
        if (Fails)
        {
            throw new HttpRequestException("The API is unavailable.");
        }

        Links.Add((link, realm));
        if (Communities.Exists(community => community.DiscordGuildId == link.DiscordGuildId))
        {
            return Task.FromResult<Guid?>(null);
        }

        var community = new CommunitySummary(Guid.NewGuid(), link.DiscordGuildId, link.ServerName, realm, link.UserId, "Gihed Annabi");
        Communities.Add(community);
        return Task.FromResult<Guid?>(community.CommunityId);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Answers a call, or fails it when the API is set to fail.</summary>
    /// <typeparam name="T">The answer's type.</typeparam>
    /// <param name="value">The value to answer with.</param>
    /// <returns>A task with the value.</returns>
    private Task<T> Answer<T>(T value) =>
        Fails ? throw new HttpRequestException("The API is unavailable.") : Task.FromResult(value);
    #endregion Private Helpers
}
