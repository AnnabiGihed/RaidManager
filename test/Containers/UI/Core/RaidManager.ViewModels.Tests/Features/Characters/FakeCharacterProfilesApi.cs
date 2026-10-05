using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Plays the profiles API for the character profile view models.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Returns the characters and profile a test sets, or throws the failure it sets, and records the calls.
/// </remarks>
internal sealed class FakeCharacterProfilesApi : ICharacterProfilesApiClient
{
    #region Properties
    /// <summary>Gets or sets the characters returned by the list call.</summary>
    public List<CharacterSummary> Characters { get; set; } = [];

    /// <summary>Gets or sets the profile returned, or <see langword="null"/> for a character that isn't the player's.</summary>
    public CharacterProfile? Profile { get; set; }

    /// <summary>Gets or sets the exception every call throws, if any.</summary>
    public Exception? Failure { get; set; }

    /// <summary>Gets the player and character of each profile call.</summary>
    public List<(Guid UserId, Guid CharacterId)> ProfileCalls { get; } = [];
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public Task<IReadOnlyList<CharacterSummary>> GetCharactersAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult<IReadOnlyList<CharacterSummary>>([.. Characters]);
    }

    /// <inheritdoc />
    public Task<CharacterProfile?> GetProfileAsync(Guid userId, Guid characterId, CancellationToken cancellationToken)
    {
        ProfileCalls.Add((userId, characterId));
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Profile);
    }
    #endregion Public Methods
}
