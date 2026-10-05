namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Represents one character snapshot of the addon contract, schema 1.</summary>
/// <param name="Realm">The realm name, such as <c>Icecrown</c>.</param>
/// <param name="Name">The character name as the game returns it.</param>
/// <param name="CapturedAt">When the addon wrote the snapshot, in seconds since 1970 (UTC); it identifies the snapshot.</param>
/// <param name="Client">The game client's locale and build.</param>
/// <param name="Identity">Who the character is.</param>
/// <param name="Guild">The character's guild.</param>
/// <param name="Professions">The professions and secondary skills.</param>
/// <param name="Equipped">The gear the character wears.</param>
/// <param name="Talents">The talent groups.</param>
/// <param name="Lockouts">The saved instances.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the addon's character snapshot so the companion uploads what the addon wrote without interpreting it (#384). Equipment sets are accepted and ignored (owner decision on #384).
/// </remarks>
public sealed record SnapshotCharacter(
    string? Realm,
    string? Name,
    long CapturedAt,
    SnapshotClient? Client,
    SnapshotIdentity? Identity,
    SnapshotGuild? Guild,
    SnapshotProfessions? Professions,
    SnapshotEquipped? Equipped,
    SnapshotTalents? Talents,
    SnapshotLockouts? Lockouts);
