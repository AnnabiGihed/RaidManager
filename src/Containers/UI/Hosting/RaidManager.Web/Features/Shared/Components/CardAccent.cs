namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the accent bar a card can show on its left edge.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets a card signal its tone, such as a warning or an error, without a separate card per tone.
/// </remarks>
public enum CardAccent
{
    /// <summary>No accent bar.</summary>
    None,

    /// <summary>The teal brand accent.</summary>
    Brand,

    /// <summary>Amber, for something to check.</summary>
    Warning,

    /// <summary>Red, for an error.</summary>
    Danger,
}
