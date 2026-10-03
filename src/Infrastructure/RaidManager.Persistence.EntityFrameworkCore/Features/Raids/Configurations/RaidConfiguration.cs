using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Raids.Configurations;

/// <summary>Maps the <see cref="Raid"/> aggregate, its targets, signups and roster selections to PostgreSQL tables.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Persists the whole raid boundary as one aggregate: child collections are owned, so they load and save with it.
/// </remarks>
internal sealed class RaidConfiguration : IEntityTypeConfiguration<Raid>
{
    #region Constants
    /// <summary>Defines the column length for stored enum names.</summary>
    private const int EnumLength = 32;

    /// <summary>Defines the shadow foreign key that ties each child row to its raid.</summary>
    private const string RaidIdColumn = "RaidId";

    /// <summary>Defines the shadow key generated for child rows that have no identity of their own.</summary>
    private const string GeneratedIdColumn = "Id";
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Raid> builder)
    {
        builder.ToTable("Raids");
        builder.HasKey(raid => raid.Id);
        builder.Property(raid => raid.Id).HasConversion(id => id.Value, value => new RaidId(value)).ValueGeneratedNever();
        builder.Property(raid => raid.CommunityId).HasConversion(id => id.Value, value => new CommunityId(value));
        builder.Property(raid => raid.CreatedByUserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(raid => raid.Title).HasMaxLength(Raid.MaximumTitleLength);
        builder.Property(raid => raid.Status).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(raid => raid.Version).IsConcurrencyToken();
        builder.HasIndex(raid => new { raid.CommunityId, raid.StartsAtUtc });

        ConfigureRequirements(builder);
        ConfigureTargets(builder);
        ConfigureSignups(builder);
        ConfigureRosterSelections(builder);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Maps the automatic signup requirements as columns of the raid row.</summary>
    /// <param name="builder">The raid builder.</param>
    private static void ConfigureRequirements(EntityTypeBuilder<Raid> builder)
    {
        builder.ComplexProperty(raid => raid.Requirements, requirements =>
        {
            requirements.Property(requirement => requirement.MinimumGearScore)
                .HasConversion(score => score.HasValue ? score.Value.Value : (int?)null, value => value.HasValue ? new GearScore(value.Value) : null);

            // Stored as ticks: a data age is often counted in days, which SQL Server's time type couldn't hold.
            requirements.Property(requirement => requirement.MaximumCharacterDataAge).HasConversion<long>();
        });
    }

    /// <summary>Maps the required instances and difficulties.</summary>
    /// <param name="builder">The raid builder.</param>
    private static void ConfigureTargets(EntityTypeBuilder<Raid> builder)
    {
        builder.OwnsMany(raid => raid.Targets, targets =>
        {
            targets.ToTable("RaidTargets");
            targets.WithOwner().HasForeignKey(RaidIdColumn);

            // The generated key grows with each insert, and owned rows load ordered by key, so the organizer's order survives.
            targets.Property<int>(GeneratedIdColumn).ValueGeneratedOnAdd();
            targets.HasKey(GeneratedIdColumn);
            targets.Property(target => target.Instance).HasConversion<string>().HasMaxLength(EnumLength);
            targets.Property(target => target.Difficulty).HasConversion<string>().HasMaxLength(EnumLength);
        });
        builder.Navigation(raid => raid.Targets).HasField("_targets");
    }

    /// <summary>Maps the participant signups and the character loadouts each one offers.</summary>
    /// <param name="builder">The raid builder.</param>
    private static void ConfigureSignups(EntityTypeBuilder<Raid> builder)
    {
        builder.OwnsMany(raid => raid.Signups, signups =>
        {
            signups.ToTable("RaidSignups");
            signups.WithOwner().HasForeignKey(RaidIdColumn);
            signups.HasKey(signup => signup.Id);
            signups.Property(signup => signup.Id).HasConversion(id => id.Value, value => new RaidSignupId(value)).ValueGeneratedNever();
            signups.Property(signup => signup.UserId).HasConversion(id => id.Value, value => new UserId(value));
            signups.Property(signup => signup.Availability).HasConversion<string>().HasMaxLength(EnumLength);
            signups.HasIndex(RaidIdColumn, nameof(RaidSignup.UserId)).IsUnique();

            signups.OwnsMany(signup => signup.Options, options =>
            {
                options.ToTable("RaidSignupOptions");
                options.WithOwner().HasForeignKey("RaidSignupId");

                // A refreshed signup replaces its options with equal new instances, so they cannot be keyed by value.
                options.Property<int>(GeneratedIdColumn).ValueGeneratedOnAdd();
                options.HasKey(GeneratedIdColumn);
                options.Property(option => option.CharacterId).HasConversion(id => id.Value, value => new CharacterId(value));
                options.Property(option => option.LoadoutId).HasConversion(id => id.Value, value => new LoadoutId(value));
            });
            signups.Navigation(signup => signup.Options).HasField("_options");
        });
        builder.Navigation(raid => raid.Signups).HasField("_signups");
    }

    /// <summary>Maps the final roster: one selected character loadout and group position per user.</summary>
    /// <param name="builder">The raid builder.</param>
    private static void ConfigureRosterSelections(EntityTypeBuilder<Raid> builder)
    {
        builder.OwnsMany(raid => raid.RosterSelections, selections =>
        {
            selections.ToTable("RosterSelections");
            selections.WithOwner().HasForeignKey(RaidIdColumn);
            selections.HasKey(selection => selection.Id);
            selections.Property(selection => selection.Id).HasConversion(id => id.Value, value => new RosterSelectionId(value)).ValueGeneratedNever();
            selections.Property(selection => selection.UserId).HasConversion(id => id.Value, value => new UserId(value));
            selections.Property(selection => selection.CharacterId).HasConversion(id => id.Value, value => new CharacterId(value));
            selections.Property(selection => selection.LoadoutId).HasConversion(id => id.Value, value => new LoadoutId(value));
            selections.HasIndex(RaidIdColumn, nameof(RosterSelection.UserId)).IsUnique();
        });
        builder.Navigation(raid => raid.RosterSelections).HasField("_rosterSelections");
    }
    #endregion Private Helpers
}
