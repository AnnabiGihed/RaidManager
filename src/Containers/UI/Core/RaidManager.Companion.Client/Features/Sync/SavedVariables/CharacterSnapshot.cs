using System.Text.Json.Nodes;

namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Represents one complete character snapshot read from <c>RaidManager.lua</c>.</summary>
/// <param name="Character">The character's realm and name.</param>
/// <param name="CapturedAt">When the addon wrote it, in seconds since 1970 (UTC); with the key it identifies the snapshot.</param>
/// <param name="Body">The character's entry as JSON, in the addon contract's shape (schema 1).</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: What the companion queues and uploads unchanged to <c>POST /companion/snapshots</c> (#384, #550).
/// </remarks>
internal sealed record CharacterSnapshot(CharacterKey Character, long CapturedAt, JsonObject Body);
