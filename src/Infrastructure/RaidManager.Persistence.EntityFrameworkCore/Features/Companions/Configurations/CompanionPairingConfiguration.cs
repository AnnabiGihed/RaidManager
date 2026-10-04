using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Companions.Configurations;

/// <summary>Maps the <see cref="CompanionPairing"/> aggregate to the CompanionPairings table.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Stores pairing requests with the hash of their device code only; the unique hash finds a polling companion's
/// request, and the code index finds the one a player confirms.
/// </remarks>
internal sealed class CompanionPairingConfiguration : IEntityTypeConfiguration<CompanionPairing>
{
    #region Constants
    /// <summary>Defines the column length for stored enum names.</summary>
    private const int EnumLength = 16;
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanionPairing> builder)
    {
        builder.ToTable("CompanionPairings");
        builder.HasKey(pairing => pairing.Id);
        builder.Property(pairing => pairing.Id).HasConversion(id => id.Value, value => new CompanionPairingId(value)).ValueGeneratedNever();
        builder.Property(pairing => pairing.DeviceCodeHash)
            .HasConversion(hash => hash.Value, value => CredentialHash.FromStored(value))
            .HasMaxLength(CredentialHash.Length)
            .IsUnicode(false);
        builder.Property(pairing => pairing.Code)
            .HasConversion(code => code.Value, value => PairingCode.Create(value))
            .HasMaxLength(PairingCode.Length)
            .IsUnicode(false);
        builder.Property(pairing => pairing.ComputerLabel).HasMaxLength(CompanionPairing.MaximumLabelLength);
        builder.Property(pairing => pairing.State).HasConversion<string>().HasMaxLength(EnumLength);
        builder.Property(pairing => pairing.ConfirmedByUserId).HasConversion(id => id!.Value, value => new UserId(value));
        builder.Property(pairing => pairing.CompanionId).HasConversion(id => id!.Value, value => new CompanionId(value));
        builder.Property(pairing => pairing.Version).IsConcurrencyToken();
        builder.HasIndex(pairing => pairing.DeviceCodeHash).IsUnique();
        builder.HasIndex(pairing => pairing.Code);
    }
    #endregion Public Methods
}
