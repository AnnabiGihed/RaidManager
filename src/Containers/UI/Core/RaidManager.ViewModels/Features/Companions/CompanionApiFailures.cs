namespace RaidManager.ViewModels.Features.Companions;

/// <summary>Tells an API failure apart from the page being closed.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the companion view models from showing an error for a call the player cancelled by leaving.
/// </remarks>
internal static class CompanionApiFailures
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
