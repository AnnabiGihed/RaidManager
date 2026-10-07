using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Uploads character snapshots to <c>POST /companion/snapshots</c> over HTTPS.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The companion side of #384's endpoint: one character per request, in the addon contract's shape, with the
/// device token sent per request (avalonia-desktop §10). Answers are read from the status and the problem title, never
/// from the message (#550).
/// </remarks>
internal sealed class SnapshotApi : ISnapshotApi
{
    #region Constants
    /// <summary>Defines the upload route.</summary>
    private const string SnapshotsRoute = "companion/snapshots";
    #endregion Constants

    #region Fields
    /// <summary>Stores the HTTP client, addressed to the environment's public API host.</summary>
    private readonly HttpClient _httpClient;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SnapshotApi"/> class.</summary>
    /// <param name="httpClient">The HTTP client, addressed to the API host.</param>
    public SnapshotApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<SnapshotUploadResult> UploadAsync(string deviceToken, QueuedSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, SnapshotsRoute) { Content = Body(snapshot) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deviceToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ResultAsync(response, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return new SnapshotUploadResult(SnapshotUploadOutcome.Unavailable);
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds the request body: the schema and addon versions with the character's entry unchanged.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns>The JSON content.</returns>
    private static ByteArrayContent Body(QueuedSnapshot snapshot)
    {
        var body = new JsonObject
        {
            ["schemaVersion"] = SavedVariablesReader.SupportedSchemaVersion,
            ["addonVersion"] = snapshot.AddonVersion,
            ["character"] = snapshot.Character.DeepClone(),
        };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            body.WriteTo(writer);
        }

        var content = new ByteArrayContent(stream.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <summary>Reads RaidManager's answer.</summary>
    /// <param name="response">The answer.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The upload's result.</returns>
    private static async Task<SnapshotUploadResult> ResultAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        response.StatusCode switch
        {
            HttpStatusCode.OK or HttpStatusCode.Accepted => new SnapshotUploadResult(SnapshotUploadOutcome.Accepted),
            HttpStatusCode.BadRequest => new SnapshotUploadResult(SnapshotUploadOutcome.Refused, await ErrorCodeAsync(response, cancellationToken)),
            HttpStatusCode.Unauthorized => new SnapshotUploadResult(SnapshotUploadOutcome.Unauthorized),
            HttpStatusCode.TooManyRequests => new SnapshotUploadResult(SnapshotUploadOutcome.SlowDown, RetryAfter: RetryAfter(response)),
            _ => new SnapshotUploadResult(SnapshotUploadOutcome.Unavailable),
        };

    /// <summary>Reads how long a 429 asks to wait.</summary>
    /// <param name="response">The answer.</param>
    /// <returns>The wait, or <see langword="null"/> when the answer doesn't say.</returns>
    private static TimeSpan? RetryAfter(HttpResponseMessage response) => response.Headers.RetryAfter?.Delta;

    /// <summary>Reads the error code from a problem answer.</summary>
    /// <param name="response">The answer.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The error code, or <see langword="null"/> when the answer has none.</returns>
    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync(CompanionApiJsonContext.Default.ProblemResponse, cancellationToken);
            return problem?.Title;
        }
        catch (JsonException)
        {
            return null;
        }
    }
    #endregion Private Helpers
}
