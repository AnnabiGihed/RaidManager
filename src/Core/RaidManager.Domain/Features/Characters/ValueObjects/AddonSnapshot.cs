namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents what the WoW addon observed about one character in one capture.</summary>
/// <param name="CapturedAtUtc">The UTC instant the addon wrote the character's snapshot, which identifies it.</param>
/// <param name="Identity">Who the character is, or <see langword="null"/> when the game didn't answer.</param>
/// <param name="Guild">The character's guild, or <see langword="null"/> when the game didn't answer.</param>
/// <param name="Professions">The professions, or <see langword="null"/> when the skill list couldn't be read.</param>
/// <param name="Talents">The talent groups, or <see langword="null"/> when the game hadn't loaded them.</param>
/// <param name="Gear">The gear worn at the capture, or <see langword="null"/> when it couldn't be read.</param>
/// <param name="RaidSaves">The saved-instance scan, or <see langword="null"/> when the game hadn't answered.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one character snapshot of the addon contract (schema 1) into the Character aggregate, each section either observed or absent, so an unavailable section never erases a known fact (#384).
/// </remarks>
public sealed record AddonSnapshot(
    DateTimeOffset CapturedAtUtc,
    AddonIdentity? Identity,
    AddonGuild? Guild,
    AddonProfessions? Professions,
    AddonTalents? Talents,
    AddonGear? Gear,
    AddonRaidSaves? RaidSaves);
