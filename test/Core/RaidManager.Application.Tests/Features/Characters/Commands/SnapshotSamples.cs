using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

namespace RaidManager.Application.Tests.Features.Characters.Commands;

/// <summary>Builds character snapshots in the shape of the addon contract for the import tests.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Mirrors the <c>one-character</c> and <c>raid-save</c> fixtures of <c>test/Fixtures/Addon/SavedVariables</c>
/// with every section observed, so each test changes only what it is about (#384).
/// </remarks>
internal static class SnapshotSamples
{
    #region Constants
    /// <summary>Defines the status of an observed section.</summary>
    public const string Observed = "observed";

    /// <summary>Defines the status of an unavailable section.</summary>
    public const string Unavailable = "unavailable";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds a snapshot of a level 80 Death Knight with every section observed.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="capturedAtUtc">The capture instant.</param>
    /// <returns>The snapshot.</returns>
    public static SnapshotCharacter Character(string name, DateTimeOffset capturedAtUtc)
    {
        var at = capturedAtUtc.ToUnixTimeSeconds();
        return new SnapshotCharacter(
            "Icecrown",
            name,
            at,
            new SnapshotClient("enUS", "12340"),
            new SnapshotIdentity(Observed, at, 80, "DEATHKNIGHT", "Scourge", "Horde"),
            new SnapshotGuild(Observed, at, true, "Dark Templars"),
            new SnapshotProfessions(Observed, at, [new SnapshotProfession("Blacksmithing", 450, 450), new SnapshotProfession("Fishing", 1, 75)]),
            new SnapshotEquipped(
                Observed,
                at,
                [
                    new SnapshotGearSlot(1, null, null, "item:51312:3817:3628:3519:0:0:0:1218372352:80", 51312),
                    new SnapshotGearSlot(2, null, Unavailable, null, null),
                    new SnapshotGearSlot(4, true, null, null, null),
                    new SnapshotGearSlot(16, null, null, "item:50737:3368:3628:3628:0:0:0:0:80", 50737),
                ]),
            new SnapshotTalents(
                Observed,
                at,
                1,
                [
                    new SnapshotTalentGroup(
                        1,
                        [new SnapshotTalentTab("Blood", 0, "0000"), new SnapshotTalentTab("Frost", 17, "3050"), new SnapshotTalentTab("Unholy", 54, "3333")],
                        [new SnapshotGlyph(1, 1, true, null, 63335), new SnapshotGlyph(2, 2, true, null, 60200), new SnapshotGlyph(3, 2, true, true, null), new SnapshotGlyph(4, 1, false, null, null)]),
                    new SnapshotTalentGroup(
                        2,
                        [new SnapshotTalentTab("Blood", 51, "3333"), new SnapshotTalentTab("Frost", 10, "3000"), new SnapshotTalentTab("Unholy", 10, "3000")],
                        []),
                ]),
            new SnapshotLockouts(
                Observed,
                at,
                true,
                [
                    new SnapshotLockout("Icecrown Citadel", 31415926, 0, 345600, 2, 25, true, true, false),
                    new SnapshotLockout("The Ruby Sanctum", 4026531841, 1, 950400, 3, 10, true, true, true),
                    new SnapshotLockout("Trial of the Crusader", 27182818, 0, 0, 2, 25, true, false, false),
                    new SnapshotLockout("Gruul's Lair", 14142135, 0, 345600, 1, 25, true, true, false),
                    new SnapshotLockout("The Forge of Souls", 16180339, 0, 43200, 2, 5, false, true, false),
                    new SnapshotLockout("Naxxramas", 11111111, 0, 345600, 1, 40, true, true, false),
                ]));
    }
    #endregion Public Methods
}
