using System.Net;
using System.Text;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Answers HTTP requests from the test's script, and records them.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the API client's tests use the API's real status codes and problem bodies without a network.
/// </remarks>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    #region Fields
    /// <summary>Stores how the next request is answered.</summary>
    private Func<HttpRequestMessage, HttpResponseMessage> _answer = _ => new HttpResponseMessage(HttpStatusCode.OK);
    #endregion Fields

    #region Public Properties
    /// <summary>Gets the requests received, with their bodies read.</summary>
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];
    #endregion Public Properties

    #region Public Methods
    /// <summary>Answers every request with a status and a JSON body.</summary>
    /// <param name="status">The status.</param>
    /// <param name="json">The JSON body, or <see langword="null"/> for none.</param>
    public void Answer(HttpStatusCode status, string? json = null) => _answer = _ => new HttpResponseMessage(status)
    {
        Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json"),
    };

    /// <summary>Answers every request with a problem whose title is an error code.</summary>
    /// <param name="status">The status.</param>
    /// <param name="code">The error code.</param>
    public void AnswerProblem(HttpStatusCode status, string code) =>
        Answer(status, $$"""{"title":"{{code}}","status":{{(int)status}}}""");

    /// <summary>Fails every request as a network failure would.</summary>
    /// <param name="exception">The failure.</param>
    public void Fail(Exception exception) => _answer = _ => throw exception;
    #endregion Public Methods

    #region Protected Methods
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return _answer(request);
    }
    #endregion Protected Methods
}
