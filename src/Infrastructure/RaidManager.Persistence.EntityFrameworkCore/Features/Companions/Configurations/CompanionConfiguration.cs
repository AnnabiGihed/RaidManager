using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Companions.Configurations;

/// <summary>Maps the <see cref="Companion"/> aggregate to the Companions table.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Stores paired companions with the hash of their device token only; the unique hash is the lookup every
/// companion request makes (ADR-0030).
/// </remarks>
internal sealed class CompanionConfiguration : IEntityTypeConfiguration<Companion>
{
    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Companion> builder)
    {
        builder.ToTable("Companions");
        builder.HasKey(companion => companion.Id);
        builder.Property(companion => companion.Id).HasConversion(id => id.Value, value => new CompanionId(value)).ValueGeneratedNever();
        builder.Property(companion => companion.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(companion => companion.Label).HasMaxLength(CompanionPairing.MaximumLabelLength);
        builder.Property(companion => companion.TokenHash)
            .HasConversion(hash => hash.Value, value => CredentialHash.FromStored(value))
            .HasMaxLength(CredentialHash.Length)
            .IsUnicode(false);
        builder.Property(companion => companion.Version).IsConcurrencyToken();
        builder.HasIndex(companion => companion.TokenHash).IsUnique();
        builder.HasIndex(companion => companion.UserId);
    }
    #endregion Public Methods
}
