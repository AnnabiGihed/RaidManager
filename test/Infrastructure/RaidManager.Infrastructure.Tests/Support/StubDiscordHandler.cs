using System.Net;
using System.Text;

namespace RaidManager.Infrastructure.Tests.Support;

/// <summary>Stands in for Discord's REST API, answering every request with one prepared response.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the Discord client be tested on real HTTP messages without calling Discord, and records what it sent.
/// </remarks>
internal sealed class StubDiscordHandler : HttpMessageHandler
{
    #region Fields
    /// <summary>Stores the requests received, in order.</summary>
    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>Stores how the stub answers.</summary>
    private Func<CancellationToken, Task<HttpResponseMessage>> _answer = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    #endregion Fields

    #region Properties
    /// <summary>Gets the requests received, in order.</summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;
    #endregion Properties

    #region Public Methods
    /// <summary>Answers every request with a status and a JSON body.</summary>
    /// <param name="status">The HTTP status.</param>
    /// <param name="json">The JSON body.</param>
    /// <returns>The same stub.</returns>
    public StubDiscordHandler Answering(HttpStatusCode status, string json)
    {
        _answer = _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        return this;
    }

    /// <summary>Fails every request with an exception, as a network failure or a timeout would.</summary>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The same stub.</returns>
    public StubDiscordHandler Failing(Exception exception)
    {
        _answer = _ => Task.FromException<HttpResponseMessage>(exception);
        return this;
    }

    /// <summary>Waits until the caller cancels, as a slow Discord would.</summary>
    /// <returns>The same stub.</returns>
    public StubDiscordHandler Hanging()
    {
        _answer = async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        return this;
    }
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);
        return _answer(cancellationToken);
    }
    #endregion Overrides
}
