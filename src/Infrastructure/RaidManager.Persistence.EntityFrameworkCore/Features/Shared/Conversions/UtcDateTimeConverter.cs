using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Shared.Conversions;

/// <summary>Reads every stored <see cref="DateTime"/> back as UTC.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: SQL Server <c>datetime2</c> columns lose <see cref="DateTimeKind"/>, and Pivot's <c>AuditInfo</c> refuses a non-UTC
/// date when a loaded aggregate is modified. RaidManager stores only UTC values, so marking them UTC on read is lossless.
/// </remarks>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UtcDateTimeConverter"/> class.</summary>
    public UtcDateTimeConverter()
        : base(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
    #endregion Constructors
}
