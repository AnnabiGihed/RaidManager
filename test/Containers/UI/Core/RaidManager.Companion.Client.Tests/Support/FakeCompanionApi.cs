using RaidManager.Companion.Client.Features.Pairing;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Answers the companion's API calls from the test's script, and records them.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the pairing flow run against RaidManager's answers without a network; every answer completes at once,
/// so a step that advances the clock runs the flow to its next wait.
/// </remarks>
internal sealed class FakeCompanionApi : ICompanionApi
{
    #region Fields
    /// <summary>Stores the answers to the next polls, in order; an empty queue answers pending.</summary>
    private readonly Queue<TokenPoll> _polls = new();
    #endregion Fields

    #region Public Properties
    /// <summary>Gets or sets the started pairing; <see langword="null"/> makes the start fail as unreachable.</summary>
    public StartedPairing? Started { get; set; }

    /// <summary>Gets or sets the answer to a token check.</summary>
    public TokenCheckStatus Check { get; set; } = TokenCheckStatus.Valid;

    /// <summary>Gets the computer labels pairings were started with.</summary>
    public List<string> StartedLabels { get; } = [];

    /// <summary>Gets the number of polls.</summary>
    public int PollCount { get; private set; }

    /// <summary>Gets the tokens checked.</summary>
    public List<string> CheckedTokens { get; } = [];
    #endregion Public Properties

    #region Public Methods
    /// <summary>Queues the answer to a later poll.</summary>
    /// <param name="poll">The answer.</param>
    public void AnswerPoll(TokenPoll poll) => _polls.Enqueue(poll);

    /// <inheritdoc />
    public Task<StartedPairing> StartPairingAsync(string computerLabel, CancellationToken cancellationToken)
    {
        StartedLabels.Add(computerLabel);
        return Started is null
            ? Task.FromException<StartedPairing>(new HttpRequestException("RaidManager can't be reached."))
            : Task.FromResult(Started);
    }

    /// <inheritdoc />
    public Task<TokenPoll> CollectTokenAsync(string deviceCode, CancellationToken cancellationToken)
    {
        PollCount++;
        return Task.FromResult(_polls.Count > 0 ? _polls.Dequeue() : new TokenPoll(TokenPollStatus.Pending));
    }

    /// <inheritdoc />
    public Task<TokenCheckStatus> CheckTokenAsync(string deviceToken, CancellationToken cancellationToken)
    {
        CheckedTokens.Add(deviceToken);
        return Task.FromResult(Check);
    }
    #endregion Public Methods
}
