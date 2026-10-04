using RaidManager.ViewModels.Features.Companions;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Features.Companions;

/// <summary>Maps the companion pages' notice kinds to the design system's tones.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the wording in the view models and the styling in the components; both companion pages use it.
/// </remarks>
internal static class CompanionTones
{
    #region Public Methods
    /// <summary>Maps a kind to a notification tone.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The tone.</returns>
    public static ToastTone ToastOf(CompanionNoticeKind kind) => kind switch
    {
        CompanionNoticeKind.Success => ToastTone.Success,
        CompanionNoticeKind.Warning => ToastTone.Warning,
        CompanionNoticeKind.Error => ToastTone.Danger,
        _ => ToastTone.Info,
    };

    /// <summary>Maps a kind to a notice tone; a success reads as information.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The tone.</returns>
    public static NoticeTone NoticeOf(CompanionNoticeKind kind) => kind switch
    {
        CompanionNoticeKind.Warning => NoticeTone.Warning,
        CompanionNoticeKind.Error => NoticeTone.Danger,
        _ => NoticeTone.Info,
    };
    #endregion Public Methods
}
