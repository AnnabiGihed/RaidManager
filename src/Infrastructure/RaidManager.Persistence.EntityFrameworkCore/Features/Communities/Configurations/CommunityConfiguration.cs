using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Communities.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Communities.Configurations;

/// <summary>Maps the <see cref="Community"/> aggregate and its Discord role mappings to SQL Server tables.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Stores what RaidManager owns about a linked server; the unique server id is the database's guard against
/// linking one Discord server twice. Members are not stored, because their roles are read from Discord (ADR-0022).
/// </remarks>
internal sealed class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    #region Constants
    /// <summary>Defines the column length for stored enum names.</summary>
    private const int EnumLength = 32;

    /// <summary>Defines the column length of a Discord snowflake.</summary>
    private const int SnowflakeLength = 20;
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("Communities");
        builder.HasKey(community => community.Id);
        builder.Property(community => community.Id).HasConversion(id => id.Value, value => new CommunityId(value)).ValueGeneratedNever();
        builder.Property(community => community.DiscordGuildId).HasMaxLength(SnowflakeLength).IsUnicode(false);
        builder.Property(community => community.Name).HasMaxLength(Community.MaximumNameLength);
        builder.Property(community => community.Realm).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(community => community.AdministratorId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(community => community.Version).IsConcurrencyToken();
        builder.HasIndex(community => community.DiscordGuildId).IsUnique();

        builder.OwnsMany(community => community.RoleMappings, mappings =>
        {
            mappings.ToTable("CommunityRoleMappings");
            mappings.WithOwner().HasForeignKey("CommunityId");

            // One Discord role can give several RaidManager roles, so a row is a community, a Discord role and a role.
            mappings.HasKey("CommunityId", nameof(DiscordRoleMapping.DiscordRoleId), nameof(DiscordRoleMapping.Role));
            mappings.Property(mapping => mapping.DiscordRoleId).HasMaxLength(SnowflakeLength).IsUnicode(false);
            mappings.Property(mapping => mapping.Role).HasConversion<string>().HasMaxLength(EnumLength);
        });
        builder.Navigation(community => community.RoleMappings).HasField("_roleMappings");
    }
    #endregion Public Methods
}
