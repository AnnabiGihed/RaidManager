namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents a profession or secondary skill the addon read from the character's skill list.</summary>
/// <param name="Name">The profession name as the game client shows it, for example <c>Blacksmithing</c>.</param>
/// <param name="Rank">The current skill.</param>
/// <param name="MaxRank">The maximum skill of the character's current training.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries the synchronized profession facts a profile shows (story #19), as the snapshot's
/// <c>professions</c> section defines them (#38).
/// </remarks>
public sealed record Profession(string Name, int Rank, int MaxRank)
{
    #region Constants
    /// <summary>Defines the longest profession name stored.</summary>
    public const int MaximumNameLength = 64;
    #endregion Constants
}
