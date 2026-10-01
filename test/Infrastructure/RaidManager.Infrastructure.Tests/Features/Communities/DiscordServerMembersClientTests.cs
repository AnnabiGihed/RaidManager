using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Infrastructure.Features.Communities;
using RaidManager.Infrastructure.Tests.Support;

namespace RaidManager.Infrastructure.Tests.Features.Communities;

/// <summary>Verifies how the Discord client turns Discord's answers into memberships or failures.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Only Discord's "unknown member, user or server" answers mean "not a member"; every other unexpected answer
/// fails closed (ADR-0022).
/// </remarks>
public sealed class DiscordServerMembersClientTests : IDisposable
{
    #region Constants
    /// <summary>Defines the Discord server the tests ask about.</summary>
    private const string GuildId = "123456789012345678";

    /// <summary>Defines the Discord user the tests ask about.</summary>
    private const string UserId = "876543210987654321";
    #endregion Constants

    #region Fields
    /// <summary>Stores the stub standing in for Discord.</summary>
    private readonly StubDiscordHandler _discord = new();
    #endregion Fields

    #region Tests
    /// <summary>Reads a member's roles from Discord's guild member object.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task AMemberComesBackWithTheirRoles()
    {
        _discord.Answering(HttpStatusCode.OK, """{"user":{"id":"876543210987654321"},"roles":["111","222"]}""");

        var result = await FindAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsMember.ShouldBeTrue();
        result.Value.RoleIds.ShouldBe(["111", "222"]);
        _discord.Requests.ShouldHaveSingleItem().RequestUri!.AbsolutePath.ShouldBe($"/api/v10/guilds/{GuildId}/members/{UserId}");
    }

    /// <summary>Treats Discord's "unknown" answers as not a member.</summary>
    /// <param name="code">Discord's JSON error code.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(10004)]
    [InlineData(10007)]
    [InlineData(10013)]
    public async Task UnknownMemberUserOrServerMeansNotAMember(int code)
    {
        _discord.Answering(HttpStatusCode.NotFound, $$"""{"message":"Unknown","code":{{code}}}""");

        var result = await FindAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(DiscordMembership.NotMember);
    }

    /// <summary>Fails closed on any other refusal or error status.</summary>
    /// <param name="status">The HTTP status Discord returns.</param>
    /// <param name="json">The body Discord returns.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData(HttpStatusCode.NotFound, """{"message":"404: Not Found","code":0}""")]
    [InlineData(HttpStatusCode.Unauthorized, """{"message":"401: Unauthorized","code":0}""")]
    [InlineData(HttpStatusCode.Forbidden, """{"message":"Missing Access","code":50001}""")]
    [InlineData(HttpStatusCode.TooManyRequests, """{"message":"You are being rate limited.","retry_after":1.5}""")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}")]
    public async Task AnyOtherAnswerFailsClosed(HttpStatusCode status, string json)
    {
        _discord.Answering(status, json);

        var result = await FindAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DiscordErrors.Unavailable);
    }

    /// <summary>Fails closed when Discord can't be reached, times out or answers unreadably.</summary>
    /// <param name="failure">The kind of failure.</param>
    /// <returns>A task that completes when the test has run.</returns>
    [Theory]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("unreadable")]
    public async Task AFailedCallFailsClosed(string failure)
    {
        _ = failure switch
        {
            "network" => _discord.Failing(new HttpRequestException("No route to Discord.")),
            "timeout" => _discord.Failing(new TaskCanceledException("The request timed out.")),
            _ => _discord.Answering(HttpStatusCode.OK, "not json"),
        };

        var result = await FindAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DiscordErrors.Unavailable);
    }

    /// <summary>Lets the caller's cancellation through instead of reporting Discord as unavailable.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task TheCallersCancellationIsNotAnOutage()
    {
        _discord.Hanging();
        using var cancellation = new CancellationTokenSource();
        var lookup = FindAsync(cancellation.Token);

        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(lookup);
    }
    #endregion Tests

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _discord.Dispose();
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Asks the client about the test user, through the stub.</summary>
    /// <param name="cancellationToken">The caller's token.</param>
    /// <returns>The client's answer.</returns>
    private async Task<Pivot.Framework.Domain.Shared.Result<DiscordMembership>> FindAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient(_discord, disposeHandler: false) { BaseAddress = new Uri("https://discord.com/api/v10/") };
        var client = new DiscordServerMembersClient(httpClient, NullLogger<DiscordServerMembersClient>.Instance);
        return await client.FindAsync(GuildId, UserId, cancellationToken);
    }
    #endregion Private Helpers
}
