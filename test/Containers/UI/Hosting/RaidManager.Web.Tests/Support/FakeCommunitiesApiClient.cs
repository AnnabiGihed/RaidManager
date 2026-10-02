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
    #region Fields
    /// <summary>Stores the Officer preset's id in the sample roles card.</summary>
    public static readonly Guid OfficerId = Guid.Parse("0a6f3c1e-1111-4c55-9a8e-0d3c1b2a4f5e");

    /// <summary>Stores the Raid leader preset's id in the sample roles card.</summary>
    public static readonly Guid RaidLeaderId = Guid.Parse("0a6f3c1e-2222-4c55-9a8e-0d3c1b2a4f5e");
    #endregion Fields

    #region Properties
    /// <summary>Gets the linked communities.</summary>
    public List<CommunitySummary> Communities { get; } = [];

    /// <summary>Gets or sets a value indicating whether every call fails as an unreachable API would.</summary>
    public bool Fails { get; set; }

    /// <summary>Gets the links the website asked for, with their realm.</summary>
    public List<(PendingCommunityLink Link, string Realm)> Links { get; } = [];

    /// <summary>Gets the roles cards by community.</summary>
    public Dictionary<Guid, CommunityRoleSettings> RoleSettings { get; } = [];

    /// <summary>Gets or sets how the API answers every roles call; <see cref="CommunityApiStatus.Succeeded"/> by default.</summary>
    public CommunityApiStatus RoleStatus { get; set; } = CommunityApiStatus.Succeeded;

    /// <summary>Gets the role changes asked for: the Discord role, the community role's id, and whether it was added or removed.</summary>
    public List<(string DiscordRoleId, Guid RoleId, bool Added)> RoleChanges { get; } = [];

    /// <summary>Gets the Discord servers each sign-in asked about.</summary>
    public List<IReadOnlyList<string>> ServerLookups { get; } = [];

    /// <summary>Gets the members pages by community; a community without one gets a sample page.</summary>
    public Dictionary<Guid, CommunityMembers> MemberLists { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <summary>Creates a community administered by a user.</summary>
    /// <param name="administratorId">The Administrator.</param>
    /// <param name="name">The community name.</param>
    /// <param name="realm">The realm's name.</param>
    /// <returns>The community.</returns>
    public static CommunitySummary Community(Guid administratorId, string name = "Dark Templars", string realm = "Icecrown") =>
        new(Guid.NewGuid(), "123456789012345678", name, realm, administratorId, "Gihed Annabi");

    /// <summary>Creates a members page with an officer who has a picture and a member without roles or picture.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="checkedAtUtc">When Discord was asked.</param>
    /// <returns>The members page.</returns>
    public static CommunityMembers MemberList(Guid communityId, DateTimeOffset checkedAtUtc) => new(
        communityId,
        "Dark Templars",
        checkedAtUtc,
        [
            new CommunityMember("1", "Malarya", "https://cdn.discordapp.com/avatars/1/a.png", [new DiscordRoleOption("12", "Officier"), new DiscordRoleOption("13", "Veteran")], ["Officer", "Raid leader"]),
            new CommunityMember("2", "OrlkDemon", null, [], ["Member"]),
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
            new CommunityRoleRow("Administrator", null, "Administrator", ["ManageRaids", "BuildRosters", "RunRaidNight", "ReviewConflicts", "ManageCommunityRoles"], [], 1),
            new CommunityRoleRow("Role", OfficerId, "Officer", ["ManageRaids", "BuildRosters", "RunRaidNight", "ReviewConflicts"], [new MappedDiscordRole("12", "Officier", false)], 1),
            new CommunityRoleRow("Role", RaidLeaderId, "Raid leader", ["ManageRaids", "BuildRosters", "RunRaidNight"], [new MappedDiscordRole("99", null, true)], 0),
            new CommunityRoleRow("Member", null, "Member", [], [], 2),
        ]);

    /// <inheritdoc />
    public Task<IReadOnlyList<CommunitySummary>> GetUserCommunitiesAsync(Guid userId, IReadOnlyCollection<Guid> memberOf, CancellationToken cancellationToken) =>
        Answer<IReadOnlyList<CommunitySummary>>(
        [
            .. Communities.Where(community => community.AdministratorId == userId),
            .. Communities.Where(community => community.AdministratorId != userId && memberOf.Contains(community.CommunityId)),
        ]);

    /// <inheritdoc />
    public Task<IReadOnlyList<CommunitySummary>> FindByDiscordServersAsync(IReadOnlyCollection<string> discordGuildIds, CancellationToken cancellationToken)
    {
        ServerLookups.Add([.. discordGuildIds]);
        return Answer<IReadOnlyList<CommunitySummary>>([.. Communities.Where(community => discordGuildIds.Contains(community.DiscordGuildId))]);
    }

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

    /// <inheritdoc />
    public Task<CommunityMembersAnswer> GetMembersAsync(Guid userId, Guid communityId, CancellationToken cancellationToken) =>
        Fails ? throw new HttpRequestException("The API is unavailable.") : Task.FromResult(RoleStatus == CommunityApiStatus.Succeeded
            ? new CommunityMembersAnswer(RoleStatus, MemberLists.TryGetValue(communityId, out var list) ? list : MemberList(communityId, DateTimeOffset.UtcNow))
            : new CommunityMembersAnswer(RoleStatus, null));

    /// <inheritdoc />
    public Task<CommunityRoleSettingsAnswer> GetRoleSettingsAsync(Guid userId, Guid communityId, CancellationToken cancellationToken) =>
        Fails ? throw new HttpRequestException("The API is unavailable.") : Task.FromResult(RoleStatus == CommunityApiStatus.Succeeded
            ? new CommunityRoleSettingsAnswer(RoleStatus, RoleSettings.TryGetValue(communityId, out var card) ? card : Card(communityId, canEdit: true))
            : new CommunityRoleSettingsAnswer(RoleStatus, null));

    /// <inheritdoc />
    public Task<CommunityApiStatus> MapRoleAsync(Guid userId, Guid communityId, string discordRoleId, Guid roleId, CancellationToken cancellationToken)
    {
        RoleChanges.Add((discordRoleId, roleId, true));
        return Task.FromResult(RoleStatus);
    }

    /// <inheritdoc />
    public Task<CommunityApiStatus> UnmapRoleAsync(Guid userId, Guid communityId, string discordRoleId, Guid roleId, CancellationToken cancellationToken)
    {
        RoleChanges.Add((discordRoleId, roleId, false));
        return Task.FromResult(RoleStatus);
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
