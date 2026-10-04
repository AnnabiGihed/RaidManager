namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Describes a notification or notice on the companion pages.</summary>
/// <param name="Kind">How it is styled.</param>
/// <param name="Title">The short title.</param>
/// <param name="Detail">The sentence under the title.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Carries the wording of the companion mockup's notices and notifications (companion pairing boards 2, 4 and 9 to 17).
/// </remarks>
public sealed record CompanionNotice(CompanionNoticeKind Kind, string Title, string Detail);
