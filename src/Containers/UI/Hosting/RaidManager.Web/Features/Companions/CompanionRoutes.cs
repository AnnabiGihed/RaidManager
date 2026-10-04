namespace RaidManager.Web.Features.Companions;

/// <summary>Names the website's companion routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the sidebar, the companion's link to the confirm page and the tests on the same paths.
/// </remarks>
public static class CompanionRoutes
{
    #region Constants
    /// <summary>Defines the paired companions list, the Companion &amp; sync page of the sidebar.</summary>
    public const string List = "/companion";

    /// <summary>Defines the page where the player confirms a companion's code; the companion adds <c>?code=</c>.</summary>
    public const string Pair = "/companion/pair";
    #endregion Constants

    #region Public Methods
    /// <summary>Builds the confirm page URL for a code, as the companion opens it.</summary>
    /// <param name="pairingCode">The code.</param>
    /// <returns>The relative URL.</returns>
    public static string PairFor(string pairingCode) => $"{Pair}?code={Uri.EscapeDataString(pairingCode)}";

    /// <summary>Builds the list URL that greets a newly paired computer.</summary>
    /// <param name="computerLabel">The paired computer's label.</param>
    /// <returns>The relative URL.</returns>
    public static string ListAfterPairing(string computerLabel) => $"{List}?paired={Uri.EscapeDataString(computerLabel)}";
    #endregion Public Methods
}
