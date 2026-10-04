namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Describes the answer to one poll for the device token.</summary>
/// <param name="Status">What the API answered.</param>
/// <param name="Companion">The paired companion, when <paramref name="Status"/> is <see cref="TokenPollStatus.Collected"/>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the pairing flow decide its next step from one value per poll.
/// </remarks>
public sealed record TokenPoll(TokenPollStatus Status, PairedCompanion? Companion = null)
{
    #region Factory Methods
    /// <summary>Creates the answer of a confirmed pairing.</summary>
    /// <param name="companion">The paired companion.</param>
    /// <returns>A collected poll.</returns>
    public static TokenPoll Collected(PairedCompanion companion) => new(TokenPollStatus.Collected, companion);
    #endregion Factory Methods
}
