namespace WarmaneRaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents the exact talent allocation and glyphs associated with a synchronized loadout.</summary>
/// <param name="SpecializationName">The human-readable specialization name.</param>
/// <param name="FirstTreePoints">Points invested in the first talent tree.</param>
/// <param name="SecondTreePoints">Points invested in the second talent tree.</param>
/// <param name="ThirdTreePoints">Points invested in the third talent tree.</param>
/// <param name="TalentCode">The deterministic serialized talent allocation used to reconstruct the full tree.</param>
/// <param name="MajorGlyphIds">The equipped major glyph spell or item identifiers.</param>
/// <param name="MinorGlyphIds">The equipped minor glyph spell or item identifiers.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps talent and glyph data versionable and independent from the currently active specialization.
/// </remarks>
public sealed record TalentConfiguration(
    string SpecializationName,
    int FirstTreePoints,
    int SecondTreePoints,
    int ThirdTreePoints,
    string TalentCode,
    IReadOnlyCollection<int> MajorGlyphIds,
    IReadOnlyCollection<int> MinorGlyphIds);
