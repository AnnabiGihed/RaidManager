using Xunit;

namespace RaidManager.Persistence.EntityFrameworkCore.Tests.Support;

/// <summary>Shares one SQL Server container across every persistence test class.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Starting SQL Server takes seconds, so test classes join this collection instead of starting their own container.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerFixture>
{
    #region Constants
    /// <summary>Defines the collection name test classes refer to.</summary>
    public const string Name = "SQL Server";
    #endregion Constants
}
