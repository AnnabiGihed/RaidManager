namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Describes one step of a <see cref="StepList"/>.</summary>
/// <param name="Title">The step's title.</param>
/// <param name="Detail">The line under the title.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds a step list its content, so the list itself holds no wording.
/// </remarks>
public sealed record StepListItem(string Title, string Detail);
