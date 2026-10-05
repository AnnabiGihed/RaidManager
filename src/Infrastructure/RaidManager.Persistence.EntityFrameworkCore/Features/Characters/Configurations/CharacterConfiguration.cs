using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Configurations;

/// <summary>Maps the <see cref="Character"/> aggregate, its claims, loadouts, raid saves and professions to PostgreSQL tables.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Persists the whole character boundary as one aggregate: child collections are owned, so they load and save with it.
/// </remarks>
internal sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    #region Constants
    /// <summary>Defines the column length for stored enum names.</summary>
    private const int EnumLength = 32;
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("Characters");
        builder.HasKey(character => character.Id);
        builder.Property(character => character.Id).HasConversion(id => id.Value, value => new CharacterId(value)).ValueGeneratedNever();
        builder.Property(character => character.OwnerId).HasConversion(id => id!.Value, value => new UserId(value));
        builder.Property(character => character.Realm).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(character => character.Name).HasConversion(name => name.Value, value => CharacterName.Create(value)).HasMaxLength(12);
        builder.Property(character => character.Class).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(character => character.Race).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(character => character.Faction).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(character => character.GuildName).HasMaxLength(64);
        builder.Property(character => character.Visibility).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(character => character.Version).IsConcurrencyToken();
        builder.HasIndex(character => new { character.Realm, character.Name }).IsUnique();

        ConfigureClaims(builder);
        ConfigureLoadouts(builder);
        ConfigureRaidLockouts(builder);
        ConfigureProfessions(builder);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Maps the ownership claims made on a character.</summary>
    /// <param name="builder">The character builder.</param>
    private static void ConfigureClaims(EntityTypeBuilder<Character> builder)
    {
        builder.OwnsMany(character => character.Claims, claims =>
        {
            claims.ToTable("CharacterClaims");
            claims.WithOwner().HasForeignKey("CharacterId");
            claims.HasKey(claim => claim.Id);
            claims.Property(claim => claim.Id).HasConversion(id => id.Value, value => new CharacterClaimId(value)).ValueGeneratedNever();
            claims.Property(claim => claim.RequestedByUserId).HasConversion(id => id.Value, value => new UserId(value));
            claims.Property(claim => claim.State).HasConversion<string>().HasMaxLength(EnumLength);
            claims.HasIndex(claim => claim.RequestedByUserId);
        });
        builder.Navigation(character => character.Claims).HasField("_claims");
    }

    /// <summary>Maps the raid-capable loadouts, each with its gear, talents and combat statistics.</summary>
    /// <param name="builder">The character builder.</param>
    private static void ConfigureLoadouts(EntityTypeBuilder<Character> builder)
    {
        builder.OwnsMany(character => character.Loadouts, loadouts =>
        {
            loadouts.ToTable("Loadouts");
            loadouts.WithOwner().HasForeignKey("CharacterId");
            loadouts.HasKey(loadout => loadout.Id);
            loadouts.Property(loadout => loadout.Id).HasConversion(id => id.Value, value => new LoadoutId(value)).ValueGeneratedNever();
            loadouts.Property(loadout => loadout.Name).HasMaxLength(64);
            loadouts.Property(loadout => loadout.Role).HasConversion<string>().HasMaxLength(EnumLength);
            loadouts.Property(loadout => loadout.Source).HasConversion<string>().HasMaxLength(EnumLength);
            loadouts.Property(loadout => loadout.GearScore).HasConversion(score => score.Value, value => new GearScore(value));

            loadouts.OwnsOne(loadout => loadout.TalentConfiguration, talents =>
            {
                talents.Property(talent => talent.SpecializationName).HasMaxLength(64);
                talents.Property(talent => talent.TalentCode).HasMaxLength(128);
                talents.Property(talent => talent.MajorGlyphIds).HasConversion(GlyphIdsConverter(), GlyphIdsComparer()).HasMaxLength(64);
                talents.Property(talent => talent.MinorGlyphIds).HasConversion(GlyphIdsConverter(), GlyphIdsComparer()).HasMaxLength(64);
            });

            loadouts.OwnsOne(loadout => loadout.Stats, stats =>
            {
                foreach (var property in typeof(CombatStats).GetProperties().Where(property => property.PropertyType == typeof(decimal)))
                {
                    stats.Property(property.Name).HasPrecision(9, 4);
                }
            });

            loadouts.OwnsMany(loadout => loadout.GearItems, gear =>
            {
                gear.ToTable("LoadoutGearItems");
                gear.WithOwner().HasForeignKey("LoadoutId");
                gear.HasKey("LoadoutId", nameof(GearItem.Slot));
                gear.Property(item => item.Slot).HasConversion<string>().HasMaxLength(EnumLength);
                gear.Property(item => item.ItemLink).HasMaxLength(512);
            });
            loadouts.Navigation(loadout => loadout.GearItems).HasField("_gearItems");
        });
        builder.Navigation(character => character.Loadouts).HasField("_loadouts");
    }

    /// <summary>Maps the character-wide raid saves from the latest complete scan.</summary>
    /// <param name="builder">The character builder.</param>
    private static void ConfigureRaidLockouts(EntityTypeBuilder<Character> builder)
    {
        builder.OwnsMany(character => character.RaidLockouts, lockouts =>
        {
            lockouts.ToTable("CharacterRaidLockouts");
            lockouts.WithOwner().HasForeignKey("CharacterId");

            // A scan may list the same instance and difficulty twice, so a generated key identifies each row.
            lockouts.Property<int>("Id").ValueGeneratedOnAdd();
            lockouts.HasKey("Id");
            lockouts.Property(lockout => lockout.Instance).HasConversion<string>().HasMaxLength(EnumLength);
            lockouts.Property(lockout => lockout.Difficulty).HasConversion<string>().HasMaxLength(EnumLength);
            lockouts.Property(lockout => lockout.LockoutId).HasMaxLength(64);
        });
        builder.Navigation(character => character.RaidLockouts).HasField("_raidLockouts");
    }

    /// <summary>Maps the professions from the latest complete skill-list read, kept in the game's order.</summary>
    /// <param name="builder">The character builder.</param>
    private static void ConfigureProfessions(EntityTypeBuilder<Character> builder)
    {
        builder.OwnsMany(character => character.Professions, professions =>
        {
            professions.ToTable("CharacterProfessions");
            professions.WithOwner().HasForeignKey("CharacterId");

            // The generated key also keeps the read's order, which the profile shows.
            professions.Property<int>("Id").ValueGeneratedOnAdd();
            professions.HasKey("Id");
            professions.Property(profession => profession.Name).HasMaxLength(Profession.MaximumNameLength);
        });
        builder.Navigation(character => character.Professions).HasField("_professions");
    }

    /// <summary>Stores a glyph list as comma-separated identifiers.</summary>
    /// <returns>The value converter.</returns>
    private static Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<IReadOnlyCollection<int>, string> GlyphIdsConverter() =>
        new(
            ids => string.Join(',', ids),
            text => text.Length == 0
                ? Array.Empty<int>()
                : text.Split(',', StringSplitOptions.None).Select(id => int.Parse(id, System.Globalization.CultureInfo.InvariantCulture)).ToArray());

    /// <summary>Compares glyph lists by content so that changes are detected.</summary>
    /// <returns>The value comparer.</returns>
    private static ValueComparer<IReadOnlyCollection<int>> GlyphIdsComparer() =>
        new(
            (left, right) => left!.SequenceEqual(right!),
            ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
            ids => ids.ToArray());
    #endregion Private Helpers
}
