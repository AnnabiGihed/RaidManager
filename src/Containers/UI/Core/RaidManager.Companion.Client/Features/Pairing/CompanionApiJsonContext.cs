using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Serializes the companion API's bodies without reflection.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Uses the web defaults (camel case) the API answers with; source generation keeps the companion
/// trimming-safe (avalonia-desktop §10).
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(StartPairingRequest))]
[JsonSerializable(typeof(StartedPairingResponse))]
[JsonSerializable(typeof(CollectTokenRequest))]
[JsonSerializable(typeof(CompanionTokenResponse))]
[JsonSerializable(typeof(ProblemResponse))]
internal sealed partial class CompanionApiJsonContext : JsonSerializerContext;
