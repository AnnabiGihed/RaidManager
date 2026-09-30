namespace RaidManager.ApiService.Tests.Support;

/// <summary>Hosts the real API as a deployed environment runs it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Proves that developer-only endpoints, such as the API reference page, are not mapped outside Development.
/// </remarks>
public sealed class ProductionApiFactory : ApiFactory
{
    #region Properties
    /// <inheritdoc />
    protected override string EnvironmentName => "Production";
    #endregion Properties
}
