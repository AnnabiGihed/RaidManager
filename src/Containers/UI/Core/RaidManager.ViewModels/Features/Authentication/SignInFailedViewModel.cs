namespace RaidManager.ViewModels.Features.Authentication;

/// <summary>Explains why a Discord sign-in did not complete.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Turns the failure reason from the sign-in flow into a title and message a player can act on, with a retry.
/// </remarks>
public sealed class SignInFailedViewModel
{
    #region Constants
    /// <summary>Defines the reason used when the player declined Discord's consent screen.</summary>
    public const string DeniedReason = "denied";
    #endregion Constants

    #region Properties
    /// <summary>Gets the page title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the explanation shown under the title.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the player cancelled on Discord, as opposed to sign-in failing.</summary>
    public bool IsCancelled { get; private set; }
    #endregion Properties

    #region Public Methods
    /// <summary>Sets the title and message for a failure reason.</summary>
    /// <param name="reason">The failure reason from the sign-in flow; anything unknown is treated as a general failure.</param>
    public void Describe(string? reason)
    {
        IsCancelled = string.Equals(reason, DeniedReason, StringComparison.OrdinalIgnoreCase);
        if (IsCancelled)
        {
            Title = "Sign-in cancelled";
            Message = "Discord did not share your account with RaidManager, so you are not signed in. "
                + "Sign in again and choose Authorize to continue.";
            return;
        }

        Title = "Sign-in did not complete";
        Message = "Something went wrong while signing you in with Discord, and you are not signed in. "
            + "This is usually temporary; please try again.";
    }
    #endregion Public Methods
}
