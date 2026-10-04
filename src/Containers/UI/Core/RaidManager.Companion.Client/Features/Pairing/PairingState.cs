namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>Lists the states of the companion's window, by their board in the companion pairing mockup.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives the window one value to switch on; each member names the board it shows.
/// </remarks>
public enum PairingState
{
    /// <summary>Board 19: the companion is asking RaidManager for a code.</summary>
    GettingCode,

    /// <summary>Board 5: the code is shown and the companion waits for the player to confirm it.</summary>
    Waiting,

    /// <summary>Board 6: this computer is paired.</summary>
    Paired,

    /// <summary>Board 7: the code expired before it was confirmed.</summary>
    Expired,

    /// <summary>Board 8: RaidManager refused the stored token; the companion must pair again.</summary>
    Revoked,

    /// <summary>Board 20: RaidManager didn't answer the request for a code.</summary>
    CodeRequestFailed,
}
