namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a message about adding RaidManager to a Discord server.</summary>
/// <param name="Title">The notice title.</param>
/// <param name="Message">The line under the title.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the view models give pages their wording, so the pages only lay it out.
/// </remarks>
public sealed record LinkNotice(string Title, string Message);
