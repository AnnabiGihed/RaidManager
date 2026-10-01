namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes the notification the review page shows after a decision.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps the wording in the view model; the page only maps the kind to a notification style.
/// </remarks>
/// <param name="Kind">How the notification is styled.</param>
/// <param name="Title">The short title.</param>
/// <param name="Detail">The sentence under the title.</param>
public sealed record ReviewNotice(ReviewNoticeKind Kind, string Title, string Detail);
