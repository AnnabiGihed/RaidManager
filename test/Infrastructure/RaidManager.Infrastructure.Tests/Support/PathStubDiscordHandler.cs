using System.Net;
using System.Text;

namespace RaidManager.Infrastructure.Tests.Support;

/// <summary>Stands in for Discord's REST API, answering each path and query with its own prepared response.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets clients that make several calls, such as reading a server and paging its members, be tested on real
/// HTTP messages without calling Discord.
/// </remarks>
internal sealed class PathStubDiscordHandler : HttpMessageHandler
{
    #region Fields
    /// <summary>Stores the prepared answers by path and query.</summary>
    private readonly Dictionary<string, (HttpStatusCode Status, string Json)> _answers = new(StringComparer.Ordinal);
    #endregion Fields

    #region Properties
    /// <summary>Gets the paths and queries requested, in order.</summary>
    public List<string> Requests { get; } = [];

    /// <summary>Gets or sets the exception every request throws, as an unreachable Discord would.</summary>
    public Exception? Failure { get; set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Answers a path and query with a status and a JSON body.</summary>
    /// <param name="pathAndQuery">The path and query, such as <c>/api/v10/guilds/1</c>.</param>
    /// <param name="status">The HTTP status.</param>
    /// <param name="json">The JSON body.</param>
    /// <returns>The same handler.</returns>
    public PathStubDiscordHandler Answer(string pathAndQuery, HttpStatusCode status, string json)
    {
        _answers[pathAndQuery] = (status, json);
        return this;
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var pathAndQuery = request.RequestUri!.PathAndQuery;
        Requests.Add(pathAndQuery);
        if (Failure is not null)
        {
            throw Failure;
        }

        var (status, json) = _answers.TryGetValue(pathAndQuery, out var answer) ? answer : (HttpStatusCode.NotFound, "{}");
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
    #endregion Overrides
}
