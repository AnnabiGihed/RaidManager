using Xunit;

namespace RaidManager.ApiService.Tests.Support;

/// <summary>Shares one API host, and its SQL Server container, across the API test classes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Each SQL Server container takes about a gigabyte; one per test class exhausted Docker's memory once the
/// community tests were added, so the classes share one, as the persistence tests do. Tests use their own random ids,
/// so they don't depend on each other's data.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFactory>
{
    #region Constants
    /// <summary>Defines the collection name test classes refer to.</summary>
    public const string Name = "API";
    #endregion Constants
}
