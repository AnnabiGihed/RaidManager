using System.Net;
using System.Text;

namespace RaidManager.Web.Tests.Support;

/// <summary>Answers HTTP requests from a list of prepared answers, keyed by path, and records the requests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets website clients be tested on real HTTP messages without calling Discord or the API.
/// </remarks>
public sealed class RecordingHandler : HttpMessageHandler
{
    #region Fields
    /// <summary>Stores the prepared answers by path.</summary>
    private readonly Dictionary<string, Func<HttpResponseMessage>> _answers = new(StringComparer.Ordinal);
    #endregion Fields

    #region Properties
    /// <summary>Gets the requests received, with their bodies read, in order.</summary>
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    /// <summary>Gets or sets the exception every request throws, as an unreachable server would.</summary>
    public Exception? Failure { get; set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Answers requests to a path with a status and a JSON body.</summary>
    /// <param name="path">The request path.</param>
    /// <param name="status">The HTTP status.</param>
    /// <param name="json">The JSON body.</param>
    /// <returns>The same handler.</returns>
    public RecordingHandler Answer(string path, HttpStatusCode status, string json = "{}")
    {
        _answers[path] = () => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        return this;
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        if (Failure is not null)
        {
            throw Failure;
        }

        return _answers.TryGetValue(request.RequestUri!.AbsolutePath, out var answer)
            ? answer()
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
    #endregion Overrides
}
