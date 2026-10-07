using System.Text.Json.Nodes;

namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Represents the globals a SavedVariables file assigns.</summary>
/// <param name="Globals">The assigned values by global name, as JSON.</param>
/// <param name="Truncated">Whether the file ended before its last value was complete.</param>
/// <param name="CutPath">The keys from the global down to the innermost entry the end of the file cut; empty when complete.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The result of <see cref="LuaTableParser"/>, so the snapshot reader can keep the characters written before a
/// cut and name the one that was cut (#550).
/// </remarks>
internal sealed record LuaDocument(IReadOnlyDictionary<string, JsonNode?> Globals, bool Truncated, IReadOnlyList<string> CutPath);
