namespace WarmaneRaidManager.Domain.Features.Shared.Identifiers;

/// <summary>Represents the strongly typed identifier for community records.</summary>
/// <param name="Value">The underlying non-empty GUID value.</param>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Prevents identifiers from different aggregates and entities from being interchanged accidentally.
/// </remarks>
public sealed record CommunityId(Guid Value) : StronglyTypedGuidId<CommunityId>(Value);
