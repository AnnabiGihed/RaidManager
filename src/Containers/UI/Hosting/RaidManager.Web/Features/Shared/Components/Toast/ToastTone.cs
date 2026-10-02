namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Names the accent of a <see cref="Toast"/>.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Says at a glance whether something was done, needs attention or failed; the title says it in words.
/// </remarks>
public enum ToastTone
{
    /// <summary>Something was done, such as a saved change: teal.</summary>
    Success,

    /// <summary>Something changed meanwhile: blue.</summary>
    Info,

    /// <summary>Something went to someone else instead: amber.</summary>
    Warning,

    /// <summary>Something failed and nothing changed: red.</summary>
    Danger,
}
