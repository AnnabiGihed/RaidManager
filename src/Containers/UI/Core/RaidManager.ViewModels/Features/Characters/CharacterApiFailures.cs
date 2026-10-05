namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Tells an API failure from the page being closed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets both character profile view models show an error for a failed call but stay quiet when the player leaves the page.
/// </remarks>
internal static class CharacterApiFailures
{
    #region Public Methods
    /// <summary>Determines whether an exception means the API failed, as opposed to the page being closed.</summary>
    /// <param name="exception">The exception.</param>
    /// <param name="cancellationToken">The page's token.</param>
    /// <returns><see langword="true"/> for an HTTP failure or a timeout.</returns>
    public static bool IsApiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    #endregion Public Methods
}
