namespace RaidManager.ViewModels.Features.Shared.Shell;

/// <summary>Describes one sidebar section with its heading and entries.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the layout render each non-empty section in order without deciding which ones a user sees.
/// </remarks>
/// <param name="Heading">The section heading, for example <c>PLAYER</c>.</param>
/// <param name="Entries">The entries listed in the sidebar, in order.</param>
public sealed record ShellNavigationGroup(string Heading, IReadOnlyList<ShellEntry> Entries);
