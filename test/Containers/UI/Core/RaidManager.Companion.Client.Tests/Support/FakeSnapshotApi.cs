using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.Upload;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Answers snapshot uploads from the test's script, and records them.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Lets the upload rules of #550 be tested without HTTP: every upload is accepted unless the test says
/// otherwise, for all characters or for one.
/// </remarks>
internal sealed class FakeSnapshotApi : ISnapshotApi
{
    #region Fields
    /// <summary>Stores the answers for single characters, by name.</summary>
    private readonly Dictionary<string, SnapshotUploadResult> _answersByCharacter = new(StringComparer.Ordinal);
    #endregion Fields

    #region Public Properties
    /// <summary>Gets or sets the answer to every upload of a character without its own answer.</summary>
    public SnapshotUploadResult Answer { get; set; } = new(SnapshotUploadOutcome.Accepted);

    /// <summary>Gets or sets the device token RaidManager refuses with 401, if any.</summary>
    public string? RefusedToken { get; set; }

    /// <summary>Gets the uploads received, as the character's name and the device token used.</summary>
    public List<(string Name, string DeviceToken)> Uploads { get; } = [];
    #endregion Public Properties

    #region Public Methods
    /// <summary>Answers every upload of one character the same way.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="result">The answer.</param>
    public void AnswerFor(string name, SnapshotUploadResult result) => _answersByCharacter[name] = result;

    /// <inheritdoc />
    public Task<SnapshotUploadResult> UploadAsync(string deviceToken, QueuedSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Uploads.Add((snapshot.Name, deviceToken));
        return Task.FromResult(deviceToken == RefusedToken
            ? new SnapshotUploadResult(SnapshotUploadOutcome.Unauthorized)
            : _answersByCharacter.GetValueOrDefault(snapshot.Name, Answer));
    }
    #endregion Public Methods
}
