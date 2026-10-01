using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Authentication.Components;

/// <summary>Ends the session through the antiforgery-protected sign-out form.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Sign-out must be a posted form (cookies change only in plain HTTP requests); this keeps that form in one place for any page or layout that offers Sign out.
/// </remarks>
public sealed partial class SignOutButton
{
    #region Properties
    /// <summary>Gets or sets the button label.</summary>
    [Parameter]
    public string Text { get; set; } = "Sign out";
    #endregion Properties
}
