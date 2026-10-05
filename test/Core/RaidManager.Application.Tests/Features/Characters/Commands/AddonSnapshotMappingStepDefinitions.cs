using System.Globalization;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Application.Tests.Features.Characters.Commands;

/// <summary>Defines business-readable steps for reading an addon snapshot into the Character aggregate's values.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Verifies the one translation of the addon contract to the domain (#384): game tokens, inventory slots, saved
/// raids with their lockout ids and resets, talent groups with their glyphs, and unavailable sections.
/// </remarks>
[Binding]
[Scope(Feature = "Addon snapshot mapping")]
public sealed class AddonSnapshotMappingStepDefinitions
{
    #region Fields
    /// <summary>Stores the capture instant of the sample snapshot.</summary>
    private readonly DateTimeOffset _capturedAtUtc = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).AddHours(-1);

    /// <summary>Stores the mapped snapshot.</summary>
    private AddonSnapshot? _mapped;
    #endregion Fields

    #region Properties
    /// <summary>Gets the mapped snapshot, failing the scenario if none was mapped.</summary>
    private AddonSnapshot Mapped => _mapped ?? throw new InvalidOperationException("No snapshot was mapped in this scenario.");
    #endregion Properties

    #region When Steps
    /// <summary>Maps the complete sample snapshot.</summary>
    [When("the sample snapshot is mapped")]
    public void WhenTheSampleSnapshotIsMapped() => Map(Sample());

    /// <summary>Maps the sample snapshot from a client in a language.</summary>
    /// <param name="locale">The client locale.</param>
    [When("the sample snapshot from a {string} client is mapped")]
    public void WhenTheSampleSnapshotFromAClientIsMapped(string locale) => Map(Sample() with { Client = new SnapshotClient(locale, "12340") });

    /// <summary>Maps the sample snapshot with every section unavailable.</summary>
    [When("the sample snapshot with every section unavailable is mapped")]
    public void WhenTheSampleSnapshotWithEverySectionUnavailableIsMapped()
    {
        const string Unavailable = SnapshotSamples.Unavailable;
        var sample = Sample();
        Map(sample with
        {
            Identity = sample.Identity! with { Status = Unavailable },
            Guild = sample.Guild! with { Status = Unavailable },
            Professions = sample.Professions! with { Status = Unavailable },
            Equipped = null,
            Talents = sample.Talents! with { Status = Unavailable },
            Lockouts = sample.Lockouts! with { Status = Unavailable },
        });
    }

    /// <summary>Maps the sample snapshot of a character in no guild.</summary>
    [When("the sample snapshot in no guild is mapped")]
    public void WhenTheSampleSnapshotInNoGuildIsMapped()
    {
        var sample = Sample();
        Map(sample with { Guild = sample.Guild! with { InGuild = false, Name = null } });
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the mapped identity.</summary>
    /// <param name="level">The expected level.</param>
    /// <param name="race">The expected race.</param>
    /// <param name="wowClass">The expected class.</param>
    /// <param name="faction">The expected faction.</param>
    [Then("the identity is a level {int} {string} {string} of the {string}")]
    public void ThenTheIdentityIsALevelOfThe(int level, string race, string wowClass, string faction) =>
        Mapped.Identity.ShouldBe(new AddonIdentity(Enum.Parse<WowClass>(wowClass), Enum.Parse<WowRace>(race), Enum.Parse<Faction>(faction), level));

    /// <summary>Asserts the mapped guild.</summary>
    /// <param name="guild">The expected guild.</param>
    [Then("the guild is {string}")]
    public void ThenTheGuildIs(string guild) => Mapped.Guild.ShouldBe(new AddonGuild(guild));

    /// <summary>Asserts the mapped professions.</summary>
    /// <param name="professions">The expected professions, such as <c>Mining 450/450, Fishing 1/75</c>.</param>
    [Then("the professions are {string}")]
    public void ThenTheProfessionsAre(string professions) =>
        string.Join(", ", Mapped.Professions.ShouldNotBeNull().Items.Select(profession => $"{profession.Name} {profession.Rank}/{profession.MaxRank}"))
            .ShouldBe(professions);

    /// <summary>Asserts the mapped raid saves.</summary>
    /// <param name="table">Table with the columns <c>raid</c>, <c>difficulty</c>, <c>lockout</c>, <c>hours</c> and <c>extended</c>.</param>
    [Then("the raid saves are")]
    public void ThenTheRaidSavesAre(DataTable table)
    {
        var scan = Mapped.RaidSaves.ShouldNotBeNull();
        scan.IsComplete.ShouldBeTrue();
        scan.ObservedAtUtc.ShouldBe(_capturedAtUtc);
        scan.Lockouts.ShouldBe(table.Rows.Select(row => new RaidLockout(
            Enum.Parse<RaidInstance>(row["raid"]),
            Enum.Parse<RaidDifficulty>(row["difficulty"]),
            row["lockout"],
            _capturedAtUtc.AddHours(int.Parse(row["hours"], CultureInfo.InvariantCulture)),
            row["extended"] == "yes")));
    }

    /// <summary>Asserts whether the mapped raid-save scan is complete.</summary>
    /// <param name="completeness"><c>complete</c> or <c>incomplete</c>.</param>
    [Then("the raid-save scan is {string}")]
    public void ThenTheRaidSaveScanIs(string completeness) =>
        Mapped.RaidSaves.ShouldNotBeNull().IsComplete.ShouldBe(completeness == "complete");

    /// <summary>Asserts the item mapped into an equipment slot.</summary>
    /// <param name="itemId">The expected item.</param>
    /// <param name="slot">The equipment slot.</param>
    [Then("the gear holds item {int} in the {string} slot")]
    public void ThenTheGearHoldsItemInTheSlot(int itemId, string slot) =>
        Mapped.Gear.ShouldNotBeNull().Items.Single(item => item.Slot == Enum.Parse<EquipmentSlot>(slot)).ItemId.ShouldBe(itemId);

    /// <summary>Asserts that a slot is unread.</summary>
    /// <param name="slot">The equipment slot.</param>
    [Then("the {string} slot is unread")]
    public void ThenTheSlotIsUnread(string slot) => Mapped.Gear.ShouldNotBeNull().UnreadSlots.ShouldBe([Enum.Parse<EquipmentSlot>(slot)]);

    /// <summary>Asserts how many items the gear holds.</summary>
    /// <param name="count">The expected count.</param>
    [Then("the gear holds {int} items")]
    public void ThenTheGearHoldsItems(int count) => Mapped.Gear.ShouldNotBeNull().Items.Count.ShouldBe(count);

    /// <summary>Asserts a talent group's name and points.</summary>
    /// <param name="group">The talent group.</param>
    /// <param name="name">The expected name.</param>
    /// <param name="first">The expected points of the first tree.</param>
    /// <param name="second">The expected points of the second tree.</param>
    /// <param name="third">The expected points of the third tree.</param>
    [Then("talent group {int} is {string} with {int}, {int} and {int} points")]
    public void ThenTalentGroupIsWithAndPoints(int group, string name, int first, int second, int third)
    {
        var talents = Group(group);
        (talents.SpecializationName, talents.FirstTreePoints, talents.SecondTreePoints, talents.ThirdTreePoints).ShouldBe((name, first, second, third));
    }

    /// <summary>Asserts a talent group's glyphs.</summary>
    /// <param name="group">The talent group.</param>
    /// <param name="major">The expected major glyph.</param>
    /// <param name="minor">The expected minor glyph.</param>
    [Then("talent group {int} has the major glyph {int} and the minor glyph {int}")]
    public void ThenTalentGroupHasTheMajorGlyphAndTheMinorGlyph(int group, int major, int minor)
    {
        Group(group).MajorGlyphIds.ShouldBe([major]);
        Group(group).MinorGlyphIds.ShouldBe([minor]);
    }

    /// <summary>Asserts the active talent group.</summary>
    /// <param name="group">The expected active group.</param>
    [Then("talent group {int} is active")]
    public void ThenTalentGroupIsActive(int group) => Mapped.Talents.ShouldNotBeNull().ActiveGroup.ShouldBe(group);

    /// <summary>Asserts that no section was mapped.</summary>
    [Then("the mapped snapshot has no section")]
    public void ThenTheMappedSnapshotHasNoSection() =>
        Mapped.ShouldBe(new AddonSnapshot(_capturedAtUtc, null, null, null, null, null, null));

    /// <summary>Asserts that the guild was observed as none.</summary>
    [Then("the guild is observed without a name")]
    public void ThenTheGuildIsObservedWithoutAName() => Mapped.Guild.ShouldBe(new AddonGuild(null));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Gets the sample snapshot.</summary>
    /// <returns>The snapshot.</returns>
    private SnapshotCharacter Sample() => SnapshotSamples.Character("Arthasdk", _capturedAtUtc);

    /// <summary>Maps a snapshot.</summary>
    /// <param name="snapshot">The snapshot.</param>
    private void Map(SnapshotCharacter snapshot) => _mapped = AddonSnapshotMapper.ToAddonSnapshot(snapshot);

    /// <summary>Gets a mapped talent group's configuration.</summary>
    /// <param name="group">The talent group.</param>
    /// <returns>The configuration.</returns>
    private TalentConfiguration Group(int group) => Mapped.Talents.ShouldNotBeNull().Groups.Single(candidate => candidate.Group == group).Talents;
    #endregion Private Helpers
}
