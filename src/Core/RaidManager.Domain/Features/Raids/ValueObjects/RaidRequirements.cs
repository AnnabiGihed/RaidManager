using RaidManager.Domain.Features.Characters.ValueObjects;

namespace RaidManager.Domain.Features.Raids.ValueObjects;

/// <summary>Defines automatic character-data requirements applied when evaluating raid signup options.</summary>
/// <param name="MinimumGearScore">The optional minimum GearScore required for a loadout.</param>
/// <param name="RequiresUnsavedCharacter">Whether characters already saved to the raid must be rejected.</param>
/// <param name="MaximumCharacterDataAge">The maximum accepted age of synchronized addon data.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps raid eligibility rules explicit and reusable across web and Discord signup surfaces.
/// </remarks>
public sealed record RaidRequirements(GearScore? MinimumGearScore, bool RequiresUnsavedCharacter, TimeSpan MaximumCharacterDataAge);
