using System.Globalization;
using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Services;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Domain.Tests.Features.Characters.Aggregates;

/// <summary>Defines business-readable steps for applying addon snapshots to a character.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Verifies the Character aggregate's side of #384: only a newer snapshot applies, an unanswered section keeps
/// what was known, and each talent group becomes a loadout whose active group gets the worn gear (owner decisions on #384).
/// </remarks>
[Binding]
[Scope(Feature = "Addon snapshot sync")]
public sealed class AddonSnapshotSyncStepDefinitions
{
    #region Constants
    /// <summary>Defines the guild of the applied snapshot.</summary>
    private const string Guild = "Citadel Vanguard";
    #endregion Constants

    #region Static Instances
    /// <summary>Stores the gear the tests equip, head, neck and shoulders.</summary>
    private static readonly GearItem[] Gear =
    [
        new(EquipmentSlot.Head, 51312, null, "item:51312:3817:3624:0:0:0:0:0:80", null),
        new(EquipmentSlot.Neck, 50728, null, "item:50728:0:3628:0:0:0:0:0:80", null),
        new(EquipmentSlot.Shoulders, 51314, null, "item:51314:3808:3628:0:0:0:0:0:80", null),
    ];
    #endregion Static Instances

    #region Fields
    /// <summary>Stores the fixed UTC instant treated as now throughout the scenario.</summary>
    private readonly DateTimeOffset _nowUtc = DateTimeOffset.UtcNow;

    /// <summary>Stores the character under test.</summary>
    private Character _character = null!;

    /// <summary>Stores whether the latest snapshot was applied.</summary>
    private bool? _applied;

    /// <summary>Stores the domain error raised by the latest snapshot, if any.</summary>
    private DomainException? _failure;

    /// <summary>Stores the role read in a role scenario.</summary>
    private CharacterRole? _role;
    #endregion Fields

    #region Properties
    /// <summary>Gets the Frost talents of the tests, 0/53/18.</summary>
    private static TalentConfiguration Frost => new("Frost", 0, 53, 18, "0-3050030303-33333", [58631], [58640]);

    /// <summary>Gets the Blood talents of the tests, 51/10/10.</summary>
    private static TalentConfiguration Blood => new("Blood", 51, 10, 10, "3333-30-3", [58616], [58623]);
    #endregion Properties

    #region Given Steps
    /// <summary>Imports a Death Knight of a level, in no guild.</summary>
    /// <param name="level">The level.</param>
    [Given("a level {int} character in no guild")]
    public void GivenALevelCharacterInNoGuild(int level) =>
        _character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create("Arthasdk"), WowClass.DeathKnight, WowRace.Human, Faction.Alliance, level);

    /// <summary>Imports a level 70 Death Knight and applies a complete snapshot: guild, one profession, one raid save, two talent groups with group 1 active wearing three items.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [Given("a character whose snapshot captured {int} hours ago was applied")]
    public void GivenACharacterWhoseSnapshotCapturedHoursAgoWasApplied(int hours)
    {
        GivenALevelCharacterInNoGuild(70);
        var capturedAtUtc = HoursAgo(hours);
        var snapshot = new AddonSnapshot(
            capturedAtUtc,
            new AddonIdentity(WowClass.DeathKnight, WowRace.Human, Faction.Alliance, 70),
            new AddonGuild(Guild),
            new AddonProfessions([new Profession("Blacksmithing", 450, 450)], capturedAtUtc),
            Talents(1),
            new AddonGear(Gear, []),
            new AddonRaidSaves(
                true,
                [new RaidLockout(RaidInstance.IcecrownCitadel, RaidDifficulty.TwentyFivePlayerHeroic, "43127", _nowUtc.AddDays(2), false)],
                capturedAtUtc));
        _character.SynchronizeAddonSnapshot(snapshot).ShouldBeTrue();
    }
    #endregion Given Steps

    #region When Steps
    /// <summary>Applies a snapshot with only an identity and a guild.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <param name="level">The level.</param>
    /// <param name="guild">The guild.</param>
    [When("a snapshot captured {int} hours ago shows level {int} in the guild {string}")]
    public void WhenASnapshotCapturedHoursAgoShowsLevelInTheGuild(int hours, int level, string guild) =>
        Apply(Empty(hours) with
        {
            Identity = new AddonIdentity(WowClass.DeathKnight, WowRace.Human, Faction.Alliance, level),
            Guild = new AddonGuild(guild),
        });

    /// <summary>Applies a snapshot that observed the character in no guild.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago shows the character in no guild")]
    public void WhenASnapshotCapturedHoursAgoShowsTheCharacterInNoGuild(int hours) => Apply(Empty(hours) with { Guild = new AddonGuild(null) });

    /// <summary>Applies a snapshot in which the game answered no section.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago has no answered section")]
    public void WhenASnapshotCapturedHoursAgoHasNoAnsweredSection(int hours) => Apply(Empty(hours));

    /// <summary>Applies a snapshot with an incomplete saved-instance scan.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago has an incomplete raid-save scan")]
    public void WhenASnapshotCapturedHoursAgoHasAnIncompleteRaidSaveScan(int hours) =>
        Apply(Empty(hours) with { RaidSaves = new AddonRaidSaves(false, [], HoursAgo(hours)) });

    /// <summary>Applies a snapshot with a complete saved-instance scan that found no save.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago has a complete raid-save scan without saves")]
    public void WhenASnapshotCapturedHoursAgoHasACompleteRaidSaveScanWithoutSaves(int hours) =>
        Apply(Empty(hours) with { RaidSaves = new AddonRaidSaves(true, [], HoursAgo(hours)) });

    /// <summary>Applies a snapshot whose skill list has no profession.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago has a skill list without professions")]
    public void WhenASnapshotCapturedHoursAgoHasASkillListWithoutProfessions(int hours) =>
        Apply(Empty(hours) with { Professions = new AddonProfessions([], HoursAgo(hours)) });

    /// <summary>Applies a snapshot with both talent groups, one active and wearing some of the test gear.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <param name="group">The active talent group.</param>
    /// <param name="items">How many of the test items are worn.</param>
    [When("a snapshot captured {int} hours ago shows talent group {int} active wearing {int} items")]
    public void WhenASnapshotCapturedHoursAgoShowsTalentGroupActiveWearingItems(int hours, int group, int items) =>
        Apply(Empty(hours) with { Talents = Talents(group), Gear = new AddonGear([.. Gear.Take(items)], []) });

    /// <summary>Applies a snapshot whose head slot holds an item the game didn't describe.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <param name="group">The active talent group.</param>
    /// <param name="slot">The unread slot, such as <c>head</c>.</param>
    [When("a snapshot captured {int} hours ago shows talent group {int} active with the {word} slot unread")]
    public void WhenASnapshotCapturedHoursAgoShowsTalentGroupActiveWithTheSlotUnread(int hours, int group, string slot)
    {
        var unread = Slot(slot);
        Apply(Empty(hours) with { Talents = Talents(group), Gear = new AddonGear([.. Gear.Where(item => item.Slot != unread)], [unread]) });
    }

    /// <summary>Applies a snapshot with talent groups but no readable gear.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <param name="group">The active talent group.</param>
    [When("a snapshot captured {int} hours ago shows talent group {int} active without readable gear")]
    public void WhenASnapshotCapturedHoursAgoShowsTalentGroupActiveWithoutReadableGear(int hours, int group) =>
        Apply(Empty(hours) with { Talents = Talents(group) });

    /// <summary>Applies a snapshot with a talent group the game doesn't have.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    [When("a snapshot captured {int} hours ago shows a third talent group")]
    public void WhenASnapshotCapturedHoursAgoShowsAThirdTalentGroup(int hours) =>
        Apply(Empty(hours) with { Talents = new AddonTalents(3, [new AddonTalentGroup(3, Frost)]) });

    /// <summary>Reads the role of a class with some points in each tree.</summary>
    /// <param name="wowClass">The class.</param>
    /// <param name="first">Points in the first tree.</param>
    /// <param name="second">Points in the second tree.</param>
    /// <param name="third">Points in the third tree.</param>
    [When("the role of a {string} with {int}, {int} and {int} points is read")]
    public void WhenTheRoleOfAWithAndPointsIsRead(string wowClass, int first, int second, int third) =>
        _role = TalentRoles.RoleOf(Enum.Parse<WowClass>(wowClass), new TalentConfiguration("Tree", first, second, third, string.Empty, [], []));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts that the snapshot was applied.</summary>
    [Then("the snapshot is applied")]
    public void ThenTheSnapshotIsApplied() => _applied.ShouldBe(true);

    /// <summary>Asserts that the snapshot changed nothing.</summary>
    [Then("the snapshot is ignored")]
    public void ThenTheSnapshotIsIgnored() => _applied.ShouldBe(false);

    /// <summary>Asserts that the snapshot raised a domain error.</summary>
    [Then("the snapshot fails with a domain error")]
    public void ThenTheSnapshotFailsWithADomainError() => _failure.ShouldNotBeNull();

    /// <summary>Asserts the character's level.</summary>
    /// <param name="level">The expected level.</param>
    [Then("the character is level {int}")]
    public void ThenTheCharacterIsLevel(int level) => _character.Level.ShouldBe(level);

    /// <summary>Asserts the character's guild.</summary>
    /// <param name="guild">The expected guild.</param>
    [Then("the character's guild is {string}")]
    public void ThenTheCharactersGuildIs(string guild) => _character.GuildName.ShouldBe(guild);

    /// <summary>Asserts that the character is in no guild.</summary>
    [Then("the character is in no guild")]
    public void ThenTheCharacterIsInNoGuild() => _character.GuildName.ShouldBeNull();

    /// <summary>Asserts how many professions the character has.</summary>
    /// <param name="count">The expected count.</param>
    [Then("the character has {int} professions")]
    public void ThenTheCharacterHasProfessions(int count) => _character.Professions.Count.ShouldBe(count);

    /// <summary>Asserts how many raids the character is saved to.</summary>
    /// <param name="count">The expected count.</param>
    [Then("the character is saved to {int} raids")]
    public void ThenTheCharacterIsSavedToRaids(int count) => _character.RaidLockouts.Count.ShouldBe(count);

    /// <summary>Asserts the character's loadouts.</summary>
    /// <param name="table">Table with the columns <c>group</c>, <c>name</c>, <c>role</c>, <c>primary</c> and <c>items</c>.</param>
    [Then("the character has these loadouts")]
    public void ThenTheCharacterHasTheseLoadouts(DataTable table)
    {
        var expected = table.Rows.Select(row => (
            (int?)int.Parse(row["group"], CultureInfo.InvariantCulture),
            row["name"],
            Enum.Parse<CharacterRole>(row["role"]),
            row["primary"] == "yes",
            int.Parse(row["items"], CultureInfo.InvariantCulture)));
        _character.Loadouts
            .OrderBy(loadout => loadout.TalentGroup)
            .Select(loadout => (loadout.TalentGroup, loadout.Name, loadout.Role, loadout.IsPrimary, loadout.GearItems.Count))
            .ShouldBe(expected);
    }

    /// <summary>Asserts the item a talent group's loadout holds in a slot.</summary>
    /// <param name="slot">The slot, such as <c>head</c>.</param>
    /// <param name="group">The talent group.</param>
    /// <param name="itemId">The expected item.</param>
    [Then("the {word} of talent group {int} holds item {int}")]
    public void ThenTheOfTalentGroupHoldsItem(string slot, int group, int itemId) =>
        LoadoutOf(group).GearItems.Single(item => item.Slot == Slot(slot)).ItemId.ShouldBe(itemId);

    /// <summary>Asserts how many items a talent group's loadout holds.</summary>
    /// <param name="group">The talent group.</param>
    /// <param name="count">The expected count.</param>
    [Then("talent group {int} wears {int} items")]
    public void ThenTalentGroupWearsItems(int group, int count) => LoadoutOf(group).GearItems.Count.ShouldBe(count);

    /// <summary>Asserts that no loadout carries a GearScore or stats.</summary>
    [Then("no loadout has a GearScore or stats")]
    public void ThenNoLoadoutHasAGearScoreOrStats() =>
        _character.Loadouts.ShouldAllBe(loadout => loadout.GearScore == null && loadout.Stats == null && loadout.GearItems.All(item => item.ItemLevel == null));

    /// <summary>Asserts the role read.</summary>
    /// <param name="role">The expected role.</param>
    [Then("the role is {string}")]
    public void ThenTheRoleIs(string role) => _role.ShouldBe(Enum.Parse<CharacterRole>(role));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Gets both talent groups of the tests, Frost first and Blood second.</summary>
    /// <param name="activeGroup">The active group.</param>
    /// <returns>The talents.</returns>
    private static AddonTalents Talents(int activeGroup) => new(activeGroup, [new AddonTalentGroup(1, Frost), new AddonTalentGroup(2, Blood)]);

    /// <summary>Gets the equipment slot a step names.</summary>
    /// <param name="slot">The slot in lowercase words.</param>
    /// <returns>The slot.</returns>
    private static EquipmentSlot Slot(string slot) => Enum.Parse<EquipmentSlot>(slot, ignoreCase: true);

    /// <summary>Gets an instant some hours before now.</summary>
    /// <param name="hours">The hours; negative for the future.</param>
    /// <returns>The instant.</returns>
    private DateTimeOffset HoursAgo(int hours) => _nowUtc.AddHours(-hours);

    /// <summary>Gets a snapshot in which the game answered no section.</summary>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <returns>The snapshot.</returns>
    private AddonSnapshot Empty(int hours) => new(HoursAgo(hours), null, null, null, null, null, null);

    /// <summary>Applies a snapshot, keeping whether it applied or the domain error it raised.</summary>
    /// <param name="snapshot">The snapshot.</param>
    private void Apply(AddonSnapshot snapshot)
    {
        try
        {
            _applied = _character.SynchronizeAddonSnapshot(snapshot);
        }
        catch (DomainException exception)
        {
            _failure = exception;
        }
    }

    /// <summary>Gets the loadout of a talent group.</summary>
    /// <param name="group">The talent group.</param>
    /// <returns>The loadout.</returns>
    private Loadout LoadoutOf(int group) => _character.Loadouts.Single(loadout => loadout.TalentGroup == group);
    #endregion Private Helpers
}
