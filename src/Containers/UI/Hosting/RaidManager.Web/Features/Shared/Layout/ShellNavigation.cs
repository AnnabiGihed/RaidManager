using RaidManager.ViewModels.Features.Shared.Shell;
using RaidManager.Web.Features.Characters;
using RaidManager.Web.Features.Communities;

namespace RaidManager.Web.Features.Shared.Layout;

/// <summary>Lists the pages the app shell knows and registers the shell's view model.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The one place a page joins the navigation: each page's pull request adds its entry here, and the sidebar
/// shows only pages that exist (owner decision, 2026-10-01).
/// </remarks>
public static class ShellNavigation
{
    #region Fields
    /// <summary>Stores the pages in sidebar order.</summary>
    private static readonly ShellEntry[] Entries =
    [
        new(ShellSection.Player, "Overview", "/"),
        new(ShellSection.Player, "Review new characters", CharacterRoutes.Review, InSidebar: false),
        new(ShellSection.Player, "Add RaidManager", CommunityRoutes.ChooseRealm, InSidebar: false),
        new(ShellSection.Player, "Add RaidManager", CommunityRoutes.AlreadyLinked, InSidebar: false),
        new(ShellSection.Player, "Community settings", CommunityRoutes.Settings, InSidebar: false),
        new(ShellSection.Player, "Members", CommunityRoutes.Members, InSidebar: false),
    ];
    #endregion Fields

    #region Public Methods
    /// <summary>Registers the shell's view model with the known pages.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddShellNavigation(this IServiceCollection services) =>
        services.AddScoped(_ => new ShellViewModel(Entries));
    #endregion Public Methods
}
