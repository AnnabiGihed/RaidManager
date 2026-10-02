namespace RaidManager.Domain.Features.Shared.Identifiers;

/// <summary>Represents the strongly typed identifier for a community's roles.</summary>
/// <param name="Value">The underlying non-empty GUID value.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Prevents identifiers from different aggregates and entities from being interchanged accidentally.
/// </remarks>
public sealed record CommunityRoleId(Guid Value) : StronglyTypedGuidId<CommunityRoleId>(Value);
