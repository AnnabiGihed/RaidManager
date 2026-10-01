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

    /// <summary>Gets the roles cards by community.</summary>
    public Dictionary<Guid, CommunityRoleSettings> RoleSettings { get; } = [];

    /// <summary>Gets or sets how the API answers every roles call; <see cref="CommunityApiStatus.Succeeded"/> by default.</summary>
    public CommunityApiStatus RoleStatus { get; set; } = CommunityApiStatus.Succeeded;

    /// <summary>Gets the role changes asked for: the Discord role, the RaidManager role, and whether it was added or removed.</summary>
    public List<(string DiscordRoleId, string Role, bool Added)> RoleChanges { get; } = [];

    /// <summary>Gets the members pages by community; a community without one gets a sample page.</summary>
    public Dictionary<Guid, CommunityMembers> MemberLists { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Creates a community.</summary>
    /// <param name="administratorId">The Administrator.</param>
    /// <param name="guildId">The Discord server snowflake.</param>
    /// <returns>The community.</returns>
    public static CommunitySummary Community(Guid administratorId, string guildId = "987") =>
        new(Guid.NewGuid(), guildId, "Dark Templars", "Icecrown", administratorId, "Gihed Annabi");

    /// <summary>Creates a members page with an officer who has a picture and a member without roles or picture.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="checkedAtUtc">When Discord was asked.</param>
    /// <returns>The members page.</returns>
    public static CommunityMembers MemberList(Guid communityId, DateTimeOffset checkedAtUtc) => new(
        communityId,
        "Dark Templars",
        checkedAtUtc,
        [
            new CommunityMember("1", "Malarya", "https://cdn.discordapp.com/avatars/1/a.png", [new DiscordRoleOption("12", "Officier"), new DiscordRoleOption("13", "Veteran")], "Officer"),
            new CommunityMember("2", "OrlkDemon", null, [], "Member"),
        ]);

    /// <summary>Creates a roles card with Guild Master and Officier mappable, Officier mapped to Officer.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="canEdit">Whether the user is the Administrator.</param>
    /// <returns>The roles card.</returns>
    public static CommunityRoleSettings Card(Guid communityId, bool canEdit) => new(
        communityId,
        "Dark Templars",
        canEdit,
        [new DiscordRoleOption("11", "Guild Master"), new DiscordRoleOption("12", "Officier"), new DiscordRoleOption("13", "Veteran")],
        [
            new CommunityRoleRow("Administrator", [], 1),
            new CommunityRoleRow("Officer", [new MappedDiscordRole("12", "Officier", false)], 1),
            new CommunityRoleRow("RaidLeader", [new MappedDiscordRole("99", null, true)], 0),
            new CommunityRoleRow("Member", [], 2),
        ]);

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

    /// <inheritdoc />
    public Task<CommunityMembersAnswer> GetMembersAsync(Guid userId, Guid communityId, CancellationToken cancellationToken) =>
        Failure is not null ? throw Failure : Task.FromResult(RoleStatus == CommunityApiStatus.Succeeded
            ? new CommunityMembersAnswer(RoleStatus, MemberLists.TryGetValue(communityId, out var list) ? list : MemberList(communityId, DateTimeOffset.UtcNow))
            : new CommunityMembersAnswer(RoleStatus, null));

    /// <inheritdoc />
    public Task<CommunityRoleSettingsAnswer> GetRoleSettingsAsync(Guid userId, Guid communityId, CancellationToken cancellationToken) =>
        Failure is not null ? throw Failure : Task.FromResult(RoleStatus == CommunityApiStatus.Succeeded
            ? new CommunityRoleSettingsAnswer(RoleStatus, RoleSettings[communityId])
            : new CommunityRoleSettingsAnswer(RoleStatus, null));

    /// <inheritdoc />
    public Task<CommunityApiStatus> MapRoleAsync(Guid userId, Guid communityId, string discordRoleId, string role, CancellationToken cancellationToken)
    {
        RoleChanges.Add((discordRoleId, role, true));
        return Task.FromResult(RoleStatus);
    }

    /// <inheritdoc />
    public Task<CommunityApiStatus> UnmapRoleAsync(Guid userId, Guid communityId, string discordRoleId, string role, CancellationToken cancellationToken)
    {
        RoleChanges.Add((discordRoleId, role, false));
        return Task.FromResult(RoleStatus);
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
