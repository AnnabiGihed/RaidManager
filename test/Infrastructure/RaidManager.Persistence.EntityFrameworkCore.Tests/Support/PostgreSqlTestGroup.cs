using Xunit;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Support;

/// <summary>Shares one PostgreSQL container across every persistence test class.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Starting a database container takes seconds, so test classes join this collection instead of starting their own.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestGroup : ICollectionFixture<PostgreSqlFixture>
{
    #region Constants
    /// <summary>Defines the collection name test classes refer to.</summary>
    public const string Name = "PostgreSQL";
    #endregion Constants
}
