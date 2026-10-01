namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the design system's button styles.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The three button styles of ADR-0019: the teal primary action, the raised secondary action, and danger.
/// </remarks>
public enum ActionButtonAppearance
{
    /// <summary>The main action: teal with dark text.</summary>
    Primary,

    /// <summary>A secondary action: raised surface, outlined, light text.</summary>
    Secondary,

    /// <summary>A destructive action: red with dark text.</summary>
    Danger,
}
