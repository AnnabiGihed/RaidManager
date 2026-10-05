using FluentValidation;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.ValueObjects;

namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Validates the shape of an uploaded snapshot before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Rejects a snapshot that breaks the addon contract (schema 1) with a validation problem the companion can show,
/// so the handler maps only well-formed sections (#384). What a valid section means for the character stays in the
/// aggregate.
/// </remarks>
public sealed class ImportCharacterSnapshotCommandValidator : AbstractValidator<ImportCharacterSnapshotCommand>
{
    #region Constants
    /// <summary>Defines the only schema version of the addon contract.</summary>
    public const int SupportedSchemaVersion = 1;

    /// <summary>Defines the clock drift tolerated on a capture time, as the Character aggregate does.</summary>
    private const int ToleratedDriftMinutes = 5;

    /// <summary>Defines the number of inventory slots.</summary>
    private const int InventorySlots = 19;

    /// <summary>Defines the number of talent trees of every class.</summary>
    private const int TalentTrees = 3;

    /// <summary>Defines the longest talent tree, in talents.</summary>
    private const int MaximumTalentsPerTree = 40;

    /// <summary>Defines the longest name of a talent tree.</summary>
    private const int MaximumTreeNameLength = 64;

    /// <summary>Defines the longest item string stored with an item.</summary>
    private const int MaximumItemStringLength = 512;

    /// <summary>Defines the longest guild name.</summary>
    private const int MaximumGuildNameLength = 64;

    /// <summary>Defines the message of a section without a valid status or, when observed, its observation time.</summary>
    private const string SectionMessage = "A section is observed with its observation time, or unavailable.";

    /// <summary>Defines the highest WotLK level.</summary>
    private const int MaximumLevel = 80;

    /// <summary>Defines the longest time until a raid save resets, extended saves included: 60 days.</summary>
    private const int MaximumResetSeconds = 60 * 24 * 60 * 60;
    #endregion Constants

    #region Fields
    /// <summary>Stores the clock that bounds every time the addon wrote.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ImportCharacterSnapshotCommandValidator"/> class.</summary>
    /// <param name="timeProvider">The clock that bounds a capture time.</param>
    public ImportCharacterSnapshotCommandValidator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        RuleFor(command => command.CompanionId).NotEmpty();
        RuleFor(command => command.SchemaVersion).Equal(SupportedSchemaVersion)
            .WithMessage($"Only schema version {SupportedSchemaVersion} of the addon contract is accepted.");
        RuleFor(command => command.Character).NotNull().DependentRules(() =>
        {
            RuleFor(command => command.Character!.Realm).Must(realm => AddonSnapshotMapper.TryParseRealm(realm, out _))
                .WithMessage("The realm isn't a Warmane realm RaidManager knows.");
            RuleFor(command => command.Character!.Name).Must(IsCharacterName)
                .WithMessage("A character name has 2 to 12 letters.");
            RuleFor(command => command.Character!.CapturedAt).Must(IsPastTime)
                .WithMessage("The capture time is after 1970 and not in the future.");
            RuleFor(command => command.Character!.Identity).Must(identity => identity is null || IsStatus(identity.Status))
                .WithMessage(SectionMessage).SetValidator(new IdentityValidator()!);
            RuleFor(command => command.Character!.Guild).Must(guild => guild is null || IsEnvelope(guild.Status, guild.ObservedAt))
                .WithMessage(SectionMessage).SetValidator(new GuildValidator()!);
            RuleFor(command => command.Character!.Professions).Must(professions => professions is null || IsEnvelope(professions.Status, professions.ObservedAt))
                .WithMessage(SectionMessage).SetValidator(new ProfessionsValidator()!);
            RuleFor(command => command.Character!.Equipped).Must(equipped => equipped is null || IsEnvelope(equipped.Status, equipped.ObservedAt))
                .WithMessage(SectionMessage).SetValidator(new EquippedValidator()!);
            RuleFor(command => command.Character!.Talents).Must(talents => talents is null || IsEnvelope(talents.Status, talents.ObservedAt))
                .WithMessage(SectionMessage).SetValidator(new TalentsValidator()!);
            RuleFor(command => command.Character!.Lockouts).Must(lockouts => lockouts is null || IsEnvelope(lockouts.Status, lockouts.ObservedAt))
                .WithMessage(SectionMessage).SetValidator(new LockoutsValidator()!);
        });
    }
    #endregion Constructors

    #region Private Helpers
    /// <summary>Gets whether a name is a character name.</summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether the Character aggregate accepts it.</returns>
    private static bool IsCharacterName(string? name)
    {
        try
        {
            CharacterName.Create(name!);
            return true;
        }
        catch (Pivot.Framework.Domain.Exceptions.DomainException)
        {
            return false;
        }
    }

    /// <summary>Gets whether a section is absent or has one of the contract's statuses.</summary>
    /// <param name="status">The section's status.</param>
    /// <returns>Whether the status is valid.</returns>
    private static bool IsStatus(string? status) =>
        AddonSnapshotMapper.IsObserved(status) || string.Equals(status, AddonSnapshotMapper.Unavailable, StringComparison.Ordinal);

    /// <summary>Gets whether a present section's status is valid and an observed section has an observation time; a missing section counts as unavailable.</summary>
    /// <param name="status">The status.</param>
    /// <param name="observedAt">The observation time.</param>
    /// <returns>Whether the envelope is valid.</returns>
    private bool IsEnvelope(string? status, long? observedAt) =>
        IsStatus(status) && (!AddonSnapshotMapper.IsObserved(status) || (observedAt is { } seconds && IsPastTime(seconds)));

    /// <summary>Gets whether a time the addon wrote is after 1970 and not in the future beyond the tolerated drift.</summary>
    /// <param name="seconds">Seconds since 1970 (UTC).</param>
    /// <returns>Whether the time is plausible.</returns>
    private bool IsPastTime(long seconds) =>
        seconds > 0 && seconds <= _timeProvider.GetUtcNow().AddMinutes(ToleratedDriftMinutes).ToUnixTimeSeconds();
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Validates an observed identity section.</summary>
    private sealed class IdentityValidator : AbstractValidator<SnapshotIdentity>
    {
        /// <summary>Initializes a new instance of the <see cref="IdentityValidator"/> class.</summary>
        public IdentityValidator() =>
            When(identity => AddonSnapshotMapper.IsObserved(identity.Status), () =>
            {
                RuleFor(identity => identity.Level).NotNull().InclusiveBetween(1, MaximumLevel);
                RuleFor(identity => identity.Class).Must(AddonSnapshotMapper.IsKnownClass).WithMessage("The class isn't a WotLK class.");
                RuleFor(identity => identity.Race).Must(AddonSnapshotMapper.IsKnownRace).WithMessage("The race isn't a WotLK race.");
                RuleFor(identity => identity.Faction).Must(AddonSnapshotMapper.IsKnownFaction).WithMessage("The faction is Alliance or Horde.");
            });
    }

    /// <summary>Validates an observed guild section.</summary>
    private sealed class GuildValidator : AbstractValidator<SnapshotGuild>
    {
        /// <summary>Initializes a new instance of the <see cref="GuildValidator"/> class.</summary>
        public GuildValidator() =>
            When(guild => AddonSnapshotMapper.IsObserved(guild.Status), () =>
            {
                RuleFor(guild => guild.InGuild).NotNull();
                RuleFor(guild => guild.Name).NotEmpty().MaximumLength(MaximumGuildNameLength).When(guild => guild.InGuild == true);
            });
    }

    /// <summary>Validates an observed professions section.</summary>
    private sealed class ProfessionsValidator : AbstractValidator<SnapshotProfessions>
    {
        /// <summary>Initializes a new instance of the <see cref="ProfessionsValidator"/> class.</summary>
        public ProfessionsValidator() =>
            When(professions => AddonSnapshotMapper.IsObserved(professions.Status), () =>
            {
                RuleFor(professions => professions.Items).NotNull();
                RuleForEach(professions => professions.Items).ChildRules(profession =>
                {
                    profession.RuleFor(item => item.Name).NotEmpty().MaximumLength(Profession.MaximumNameLength);
                    profession.RuleFor(item => item.Rank).GreaterThanOrEqualTo(0);
                    profession.RuleFor(item => item.MaxRank).GreaterThanOrEqualTo(item => item.Rank);
                });
            });
    }

    /// <summary>Validates an observed equipped section.</summary>
    private sealed class EquippedValidator : AbstractValidator<SnapshotEquipped>
    {
        /// <summary>Initializes a new instance of the <see cref="EquippedValidator"/> class.</summary>
        public EquippedValidator() =>
            When(equipped => AddonSnapshotMapper.IsObserved(equipped.Status), () =>
            {
                RuleFor(equipped => equipped.Slots).NotNull()
                    .Must(slots => slots!.Select(slot => slot.Slot).Distinct().Count() == slots!.Count)
                    .WithMessage("Each inventory slot is listed once.");
                RuleForEach(equipped => equipped.Slots).ChildRules(slot =>
                {
                    slot.RuleFor(item => item.Slot).InclusiveBetween(1, InventorySlots);
                    slot.RuleFor(item => item.Status).Must(IsStatus).When(item => item.Status is not null);
                    slot.When(item => item.Empty != true && item.Status is null, () =>
                    {
                        slot.RuleFor(item => item.ItemId).NotNull().GreaterThan(0);
                        slot.RuleFor(item => item.ItemString).NotEmpty().MaximumLength(MaximumItemStringLength);
                    });
                });
            });
    }

    /// <summary>Validates an observed talents section.</summary>
    private sealed class TalentsValidator : AbstractValidator<SnapshotTalents>
    {
        /// <summary>Initializes a new instance of the <see cref="TalentsValidator"/> class.</summary>
        public TalentsValidator() =>
            When(talents => AddonSnapshotMapper.IsObserved(talents.Status), () =>
            {
                RuleFor(talents => talents.ActiveGroup).NotNull().InclusiveBetween(1, Loadout.MaximumTalentGroups);
                RuleFor(talents => talents.Groups).NotNull()
                    .Must(groups => groups!.Select(group => group.Group).Distinct().Count() == groups!.Count)
                    .WithMessage("Each talent group is listed once.");
                RuleForEach(talents => talents.Groups).ChildRules(group =>
                {
                    group.RuleFor(item => item.Group).InclusiveBetween(1, Loadout.MaximumTalentGroups);
                    group.RuleFor(item => item.Tabs).NotNull().Must(tabs => tabs!.Count == TalentTrees)
                        .WithMessage($"A talent group has {TalentTrees} talent trees.");
                    group.RuleForEach(item => item.Tabs).ChildRules(tab =>
                    {
                        tab.RuleFor(tree => tree.Name).NotEmpty().MaximumLength(MaximumTreeNameLength);
                        tab.RuleFor(tree => tree.PointsSpent).GreaterThanOrEqualTo(0);
                        tab.RuleFor(tree => tree.Ranks).NotNull().MaximumLength(MaximumTalentsPerTree)
                            .Must(ranks => ranks!.All(char.IsAsciiDigit)).WithMessage("A tree's ranks are digits.");
                    });
                });
            });
    }

    /// <summary>Validates an observed lockouts section.</summary>
    private sealed class LockoutsValidator : AbstractValidator<SnapshotLockouts>
    {
        /// <summary>Initializes a new instance of the <see cref="LockoutsValidator"/> class.</summary>
        public LockoutsValidator() =>
            When(lockouts => AddonSnapshotMapper.IsObserved(lockouts.Status), () =>
            {
                RuleFor(lockouts => lockouts.Complete).NotNull();
                RuleFor(lockouts => lockouts.Items).NotNull();
                RuleForEach(lockouts => lockouts.Items).ChildRules(lockout =>
                {
                    lockout.RuleFor(item => item.ResetSeconds).InclusiveBetween(0, MaximumResetSeconds);
                    lockout.RuleFor(item => item.LockoutId).InclusiveBetween(0, uint.MaxValue);
                    lockout.RuleFor(item => item.IdMostSig).InclusiveBetween(0, uint.MaxValue);
                });
            });
    }
    #endregion Nested Types
}
