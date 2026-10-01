namespace RaidManager.Web.Components;

/// <summary>Routes requests to pages inside the main layout.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: The routing root that App renders in InteractiveServer mode; every page runs inside <see cref="Layout.MainLayout"/>,
/// and a page marked <c>[Authorize]</c> renders only for a signed-in player.
/// </remarks>
public partial class Routes;
