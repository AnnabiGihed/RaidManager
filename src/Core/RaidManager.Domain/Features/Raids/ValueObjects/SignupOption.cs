using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.ValueObjects;

/// <summary>Represents one verified character loadout a user is willing to bring to a raid.</summary>
/// <param name="CharacterId">The offered character.</param>
/// <param name="LoadoutId">The offered raid-capable loadout.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows one signup to offer several characters or specs while still representing one person attending the raid.
/// </remarks>
public sealed record SignupOption(CharacterId CharacterId, LoadoutId LoadoutId);
