namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Reads a player's characters and their profiles from the API.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Lets the My characters and profile view models be tested without HTTP; the website implements it with the shared website key (ADR-0011).
/// </remarks>
public interface ICharacterProfilesApiClient
{
    #region Methods
    /// <summary>Lists the characters the player owns.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The characters, by realm and name.</returns>
    Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Gets the profile of one of the player's characters.</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="characterId">The character.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The profile, or <see langword="null"/> when the character isn't the player's.</returns>
    Task<CharacterProfile?> GetProfileAsync(Guid userId, Guid characterId, CancellationToken cancellationToken);

    /// <summary>Removes all the player's characters, claims and loadouts; offered on dev and test only (#597).</summary>
    /// <param name="userId">The signed-in player.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The number of characters the player no longer has.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API refuses the call or cannot be reached.</exception>
    Task<int> RemoveAllAsync(Guid userId, CancellationToken cancellationToken);
    #endregion Methods
}
