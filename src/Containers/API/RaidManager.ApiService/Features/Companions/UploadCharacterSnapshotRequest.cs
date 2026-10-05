using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Represents one character snapshot a paired companion uploads.</summary>
/// <param name="SchemaVersion">The version of the addon contract, <c>schemaVersion</c> in the file: 1.</param>
/// <param name="AddonVersion">The addon version that wrote the snapshot, <c>addonVersion</c> in the file.</param>
/// <param name="Character">One entry of the file's <c>characters</c> table, as the addon wrote it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets the companion upload each character of <c>RaidManager.lua</c> as JSON, in the shape of the addon contract
/// (<c>docs/reference/addon-savedvariables.md</c>), without interpreting it (#384).
/// </remarks>
public sealed record UploadCharacterSnapshotRequest(int SchemaVersion, string? AddonVersion, SnapshotCharacter? Character);
