namespace RaidManager.Domain.Features.Shared.Identifiers;

/// <summary>Represents the strongly typed identifier of a paired desktop companion.</summary>
/// <param name="Value">The underlying non-empty GUID value.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Prevents identifiers from different aggregates and entities from being interchanged accidentally.
/// </remarks>
public sealed record CompanionId(Guid Value) : StronglyTypedGuidId<CompanionId>(Value);
