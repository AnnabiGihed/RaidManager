namespace RaidManager.Domain.Features.Shared.Identifiers;

/// <summary>Represents the strongly typed identifier for user records.</summary>
/// <param name="Value">The underlying non-empty GUID value.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Prevents identifiers from different aggregates and entities from being interchanged accidentally.
/// </remarks>
public sealed record UserId(Guid Value) : StronglyTypedGuidId<UserId>(Value);
