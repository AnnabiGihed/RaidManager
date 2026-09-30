using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaidManager.Persistence.EntityFrameworkCore;

/// <summary>Creates a <see cref="RaidManagerDbContext"/> for the EF Core command-line tools.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Lets <c>dotnet ef migrations</c> build the model without starting a host. The connection string is used only
/// when a command such as <c>database update</c> actually connects; it can be overridden with <c>--connection</c>.
/// </remarks>
internal sealed class RaidManagerDbContextFactory : IDesignTimeDbContextFactory<RaidManagerDbContext>
{
    #region Constants
    /// <summary>Defines the local development database used when no connection is given.</summary>
    private const string LocalConnectionString = "Server=localhost;Database=RaidManager;Integrated Security=true;TrustServerCertificate=true";
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public RaidManagerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RaidManagerDbContext>().UseSqlServer(LocalConnectionString).Options;
        return new RaidManagerDbContext(options);
    }
    #endregion Public Methods
}
