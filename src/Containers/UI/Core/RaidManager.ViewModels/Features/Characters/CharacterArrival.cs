namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Names the characters a sync brought for the player's review since the last check.</summary>
/// <param name="Names">The new characters' names, oldest claim first.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: Gives the wording of the notifications of <c>character-sync</c> boards 1 and 4 (story #595).
/// </remarks>
public sealed record CharacterArrival(IReadOnlyList<string> Names)
{
    #region Constants
    /// <summary>Defines how many names a notification lists before counting the rest.</summary>
    private const int NamesShown = 3;
    #endregion Constants

    #region Properties
    /// <summary>Gets the title shown on another page, such as "2 new characters to review".</summary>
    public string ToReviewTitle => Names.Count == 1 ? "1 new character to review" : $"{Names.Count} new characters to review";

    /// <summary>Gets the line shown on another page, such as "Your companion found Uthertank and Valeerarog.".</summary>
    public string ToReviewMessage => $"Your companion found {NameList}.";

    /// <summary>Gets the title shown on the review page, such as "2 new characters arrived".</summary>
    public string ArrivedTitle => Names.Count == 1 ? "1 new character arrived" : $"{Names.Count} new characters arrived";

    /// <summary>Gets the line shown on the review page, such as "Uthertank and Valeerarog were added to the list.".</summary>
    public string ArrivedMessage => Names.Count == 1 ? $"{NameList} was added to the list." : $"{NameList} were added to the list.";

    /// <summary>Gets the names as a sentence: "A", "A and B", "A, B and C", or "A, B, C and 2 more".</summary>
    private string NameList => Names.Count switch
    {
        1 => Names[0],
        <= NamesShown => $"{string.Join(", ", Names.Take(Names.Count - 1))} and {Names[^1]}",
        _ => $"{string.Join(", ", Names.Take(NamesShown))} and {Names.Count - NamesShown} more",
    };
    #endregion Properties
}
