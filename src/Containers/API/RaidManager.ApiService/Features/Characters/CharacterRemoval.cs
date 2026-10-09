namespace RaidManager.ApiService.Features.Characters;

/// <summary>Tells how many characters a removal took from the player.</summary>
/// <param name="Removed">The number of characters the player no longer has.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: The answer of <c>DELETE /internal/users/{userId}/characters</c> (story #597).
/// </remarks>
public sealed record CharacterRemoval(int Removed);
