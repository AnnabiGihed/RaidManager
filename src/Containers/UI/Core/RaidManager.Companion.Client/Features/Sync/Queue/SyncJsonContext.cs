using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Serializes the sync's files without reflection.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Source generation keeps the companion trimming-safe (avalonia-desktop §10); camel case like the API.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SnapshotQueueFile))]
internal sealed partial class SyncJsonContext : JsonSerializerContext;
