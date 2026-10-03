namespace RaidManager.ApiService.Tests.Support;

/// <summary>Hosts the real API as the deployed dev environment runs it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: The deployed environments are named <c>Dev</c>, <c>Test</c> and <c>Production</c> (owner decision on #388);
/// <c>Dev</c> is the name closest to Development, so it proves a deployed name never counts as Development.
/// </remarks>
public sealed class DevApiFactory : ApiFactory
{
    #region Properties
    /// <inheritdoc />
    protected override string EnvironmentName => "Dev";
    #endregion Properties
}
