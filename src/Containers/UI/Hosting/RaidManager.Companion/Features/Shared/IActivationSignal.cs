namespace RaidManager.Companion.Features.Shared;

/// <summary>Tells the running companion that the player started it again.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the application show its window when a second start is refused, without depending on the
/// Windows-only signal behind it.
/// </remarks>
internal interface IActivationSignal
{
    #region Public Methods
    /// <summary>Calls an action, on a background thread, each time another start asks to be shown.</summary>
    /// <param name="onActivated">The action.</param>
    void Listen(Action onActivated);
    #endregion Public Methods
}
