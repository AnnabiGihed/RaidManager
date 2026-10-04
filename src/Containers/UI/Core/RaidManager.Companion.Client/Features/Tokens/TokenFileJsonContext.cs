using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidManager.Companion.Client.Features.Tokens;

/// <summary>Serializes the token file without reflection.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the token file's format in one place and the companion trimming-safe.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(StoredTokenFile))]
internal sealed partial class TokenFileJsonContext : JsonSerializerContext;
