using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Identity.Configurations;

/// <summary>Maps the <see cref="User"/> aggregate to the Users table.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Stores one row per Discord account; the unique Discord id index is the database's last guard against duplicate users.
/// </remarks>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasConversion(id => id.Value, value => new UserId(value)).ValueGeneratedNever();
        builder.Property(user => user.DiscordUserId).HasConversion(id => id.Value, value => DiscordUserId.Create(value)).HasMaxLength(20).IsUnicode(false);
        builder.Property(user => user.DisplayName).HasMaxLength(100);
        builder.Property(user => user.AvatarUrl).HasMaxLength(512);
        builder.Property(user => user.TimeZoneId).HasMaxLength(64);
        builder.Property(user => user.Version).IsConcurrencyToken();
        builder.HasIndex(user => user.DiscordUserId).IsUnique();
    }
    #endregion Public Methods
}
