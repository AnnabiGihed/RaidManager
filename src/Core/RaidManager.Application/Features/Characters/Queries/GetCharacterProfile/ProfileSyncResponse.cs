namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Says when each data source of a profile last succeeded.</summary>
/// <param name="AddonAtUtc">The latest successful addon sync, or <see langword="null"/> before the first.</param>
/// <param name="ArmoryAtUtc">The latest successful Warmane Armory sync, or <see langword="null"/> before the first.</param>
/// <param name="CompleteRaidSaveScanAtUtc">The latest complete raid-save scan, or <see langword="null"/> before the first.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds the profile's Data sources card, so a player sees how fresh each fact is.
/// </remarks>
public sealed record ProfileSyncResponse(DateTimeOffset? AddonAtUtc, DateTimeOffset? ArmoryAtUtc, DateTimeOffset? CompleteRaidSaveScanAtUtc);
