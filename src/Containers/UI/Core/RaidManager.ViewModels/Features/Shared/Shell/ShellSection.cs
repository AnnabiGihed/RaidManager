namespace RaidManager.ViewModels.Features.Shared.Shell;

/// <summary>Names the navigation sections of the app shell.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Groups the sidebar entries as ADR-0019 draws them: every player's pages, then the officers' pages.
/// </remarks>
public enum ShellSection
{
    /// <summary>Pages every signed-in player uses.</summary>
    Player,

    /// <summary>Pages only community officers use.</summary>
    Officer,
}
