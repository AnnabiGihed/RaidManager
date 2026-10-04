namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Lists what a poll for the device token can answer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Names the answers of <c>POST /companion/pairings/token</c> the pairing flow acts on (ADR-0030, step 4).
/// </remarks>
public enum TokenPollStatus
{
    /// <summary>The player hasn't confirmed the code yet.</summary>
    Pending,

    /// <summary>The API asked the companion to poll less often.</summary>
    SlowDown,

    /// <summary>The code expired.</summary>
    Expired,

    /// <summary>The pairing can't be used anymore, for example because its token was already collected.</summary>
    Invalid,

    /// <summary>The player confirmed the code and the token was collected.</summary>
    Collected,

    /// <summary>RaidManager didn't answer, or answered with a server error.</summary>
    Unavailable,
}
