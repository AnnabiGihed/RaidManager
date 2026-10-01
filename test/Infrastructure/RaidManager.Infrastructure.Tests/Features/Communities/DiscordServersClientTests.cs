using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Pivot.Framework.Domain.Shared;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Infrastructure.Features.Communities;
using RaidManager.Infrastructure.Tests.Support;

namespace RaidManager.Infrastructure.Tests.Features.Communities;

/// <summary>Verifies how the Discord server client reads a server's name, roles and members.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The answers have the shape Discord sent for the owner's server on 2026-10-01: @everyone with the server's id,
/// integration roles marked managed, bots marked user.bot, and members paged with after.
/// </remarks>
public sealed class DiscordServersClientTests : IDisposable
{
    #region Constants
    /// <summary>Defines the server the tests read.</summary>
    private const string GuildId = "123456789012345678";

    /// <summary>Defines the path of the server.</summary>
    private const string GuildPath = $"/api/v10/guilds/{GuildId}";

    /// <summary>Defines the roles answer: @everyone, a managed integration role and roles people are given.</summary>
    private const string RolesAnswer =
        $$"""[{"id":"{{GuildId}}","name":"@everyone","position":0,"managed":false},{"id":"11","name":"Member","position":5,"managed":false},{"id":"12","name":"Dyno","position":9,"managed":true},{"id":"13","name":"Officier","position":8,"managed":false},{"id":"14","name":"Guild Master","position":10,"managed":false}]""";
    #endregion Constants

    #region Fields
    /// <summary>Stores the stub standing in for Discord.</summary>
    private readonly PathStubDiscordHandler _discord = new();
    #endregion Fields

    #region Tests
    /// <summary>Reads the name and keeps only the roles people are given, highest first.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task ServerHasItsNameAndMappableRoles()
    {
        _discord.Answer(GuildPath, HttpStatusCode.OK, """{"id":"123456789012345678","name":"Dark Templars","roles":[]}""")
            .Answer($"{GuildPath}/roles", HttpStatusCode.OK, RolesAnswer);

        var server = await Client().GetAsync(GuildId, CancellationToken.None);

        server.IsSuccess.ShouldBeTrue();
        server.Value.Name.ShouldBe("Dark Templars");
        server.Value.Roles.Select(role => role.Name).ShouldBe(["Guild Master", "Officier", "Member"]);
        server.Value.Roles[0].ShouldBe(new DiscordServerRole("14", "Guild Master", 10));
    }

    /// <summary>Lists people only, with the name the server shows, across pages.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task MembersAreThePeopleAcrossPages()
    {
        var firstPage = string.Join(",", Enumerable.Range(1, 1000).Select(index => Member(index.ToString(CultureInfo.InvariantCulture), null, null, $"user{index}", false)));
        _discord.Answer($"{GuildPath}/members?limit=1000&after=0", HttpStatusCode.OK, $"[{firstPage}]")
            .Answer($"{GuildPath}/members?limit=1000&after=1000", HttpStatusCode.OK, $"[{Member("1001", "Nick", "Global", "user1001", false)},{Member("1002", null, "Global", "user1002", false)},{Member("1003", null, null, "bot", true)}]");

        var members = await Client().ListMembersAsync(GuildId, CancellationToken.None);

        members.Value.Count.ShouldBe(1002);
        members.Value[1000].ShouldBe(new DiscordServerMember("1001", "Nick", ["5"]), new MemberComparer());
        members.Value[1001].DisplayName.ShouldBe("Global");
        members.Value[0].DisplayName.ShouldBe("user1");
        members.Value.ShouldNotContain(member => member.UserId == "1003");
        _discord.Requests.Count.ShouldBe(2);
    }

    /// <summary>Reports a server the bot was removed from.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task RemovedBotIsReported()
    {
        _discord.Answer(GuildPath, HttpStatusCode.NotFound, """{"message":"Unknown Guild","code":10004}""")
            .Answer($"{GuildPath}/members?limit=1000&after=0", HttpStatusCode.NotFound, """{"message":"Unknown Guild","code":10004}""");

        (await Client().GetAsync(GuildId, CancellationToken.None)).Error.ShouldBe(DiscordErrors.BotNotInServer);
        (await Client().ListMembersAsync(GuildId, CancellationToken.None)).Error.ShouldBe(DiscordErrors.BotNotInServer);
    }

    /// <summary>Fails closed on any other answer, an unreadable one, or an unreachable Discord.</summary>
    /// <param name="variant">What goes wrong.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("roles refused")]
    [InlineData("server forbidden")]
    [InlineData("unreadable")]
    [InlineData("null answer")]
    [InlineData("unreachable")]
    public async Task AnyOtherAnswerFailsClosed(string variant)
    {
        _discord.Answer(GuildPath, HttpStatusCode.OK, """{"name":"Dark Templars"}""");
        _ = variant switch
        {
            "roles refused" => _discord.Answer($"{GuildPath}/roles", HttpStatusCode.InternalServerError, "{}"),
            "server forbidden" => _discord.Answer(GuildPath, HttpStatusCode.Forbidden, """{"message":"Missing Access","code":50001}"""),
            "unreadable" => _discord.Answer($"{GuildPath}/roles", HttpStatusCode.OK, "not json"),
            "null answer" => _discord.Answer($"{GuildPath}/roles", HttpStatusCode.OK, "null"),
            _ => _discord,
        };
        _discord.Failure = variant == "unreachable" ? new HttpRequestException("No route to Discord.") : null;

        (await Client().GetAsync(GuildId, CancellationToken.None)).Error.ShouldBe(DiscordErrors.Unavailable);
    }
    #endregion Tests

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _discord.Dispose();
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Writes a guild member object as Discord sends it.</summary>
    /// <param name="id">The user snowflake.</param>
    /// <param name="nick">The server nickname.</param>
    /// <param name="globalName">The global name.</param>
    /// <param name="username">The username.</param>
    /// <param name="bot">Whether the account is a bot.</param>
    /// <returns>The JSON.</returns>
    private static string Member(string id, string? nick, string? globalName, string username, bool bot)
    {
        var json = new StringBuilder();
        json.Append(CultureInfo.InvariantCulture, $$"""{"nick":{{Quoted(nick)}},"roles":["5"],"user":{"id":"{{id}}","username":"{{username}}","global_name":{{Quoted(globalName)}}""");
        json.Append(bot ? ""","bot":true}}""" : "}}");
        return json.ToString();
    }

    /// <summary>Writes a JSON string or null.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The JSON.</returns>
    private static string Quoted(string? value) => value is null ? "null" : $"\"{value}\"";

    /// <summary>Creates the client over the stub.</summary>
    /// <returns>The client.</returns>
    private DiscordServersClient Client() => new(
        new HttpClient(_discord, disposeHandler: false) { BaseAddress = new Uri("https://discord.com/api/v10/") },
        NullLogger<DiscordServersClient>.Instance);
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Compares members by value, including their role lists.</summary>
    private sealed class MemberComparer : IEqualityComparer<DiscordServerMember>
    {
        /// <inheritdoc />
        public bool Equals(DiscordServerMember? x, DiscordServerMember? y) =>
            x is not null && y is not null && x.UserId == y.UserId && x.DisplayName == y.DisplayName && x.RoleIds.SequenceEqual(y.RoleIds);

        /// <inheritdoc />
        public int GetHashCode(DiscordServerMember obj) => obj.UserId.GetHashCode(StringComparison.Ordinal);
    }
    #endregion Nested Types
}
