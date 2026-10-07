using System.Text.Json;
using System.Text.Json.Serialization;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.Settings;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Serializes the sync's files without reflection.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Source generation keeps the companion trimming-safe (avalonia-desktop §10); camel case like the API.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SnapshotQueueFile))]
[JsonSerializable(typeof(SyncSettings))]
internal sealed partial class SyncJsonContext : JsonSerializerContext;
