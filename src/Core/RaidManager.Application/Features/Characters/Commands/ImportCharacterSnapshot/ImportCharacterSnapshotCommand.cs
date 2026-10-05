using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Requests that RaidManager imports one character snapshot a paired companion uploaded.</summary>
/// <param name="CompanionId">The companion that uploaded the snapshot.</param>
/// <param name="SchemaVersion">The version of the addon contract the snapshot follows; only 1 is accepted.</param>
/// <param name="Character">The character snapshot, in the shape of <c>docs/reference/addon-savedvariables.md</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries a snapshot from the companion's upload to the Character aggregate (story #17, ADR-0002); the character imports with a pending claim for the companion's player, and a repeated or older snapshot changes nothing.
/// </remarks>
public sealed record ImportCharacterSnapshotCommand(Guid CompanionId, int SchemaVersion, SnapshotCharacter? Character) : ICommand<SnapshotImportOutcome>;
