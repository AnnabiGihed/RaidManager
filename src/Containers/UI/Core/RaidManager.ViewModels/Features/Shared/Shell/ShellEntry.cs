namespace RaidManager.ViewModels.Features.Shared.Shell;

/// <summary>Describes one website page the app shell knows: its section, title and route.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds the sidebar and the breadcrumb. A page reached only from a flow, such as the character review page
/// after sign-in, is listed with <paramref name="InSidebar"/> false so the breadcrumb still names it.
/// </remarks>
/// <param name="Section">The navigation section.</param>
/// <param name="Title">The title shown in the sidebar and the breadcrumb.</param>
/// <param name="Route">The page's route, starting with <c>/</c>.</param>
/// <param name="InSidebar">Whether the sidebar lists the page.</param>
/// <param name="MatchesSubpaths">Whether the sidebar entry stays highlighted on the pages under its route.</param>
public sealed record ShellEntry(ShellSection Section, string Title, string Route, bool InSidebar = true, bool MatchesSubpaths = false);
