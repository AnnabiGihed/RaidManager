using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Characters.Events;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Aggregates;

/// <summary>Represents a Warmane character and the raid-relevant state shared by all of its loadouts.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides the authoritative character boundary for ownership, raid lockouts and multiple synchronized raid loadouts.
/// </remarks>
public sealed class Character : AggregateRoot<CharacterId>
{
    #region Fields
    /// <summary>Stores the raid-capable loadouts synchronized for the character.</summary>
    private readonly List<Loadout> _loadouts = [];

    /// <summary>Stores the current character-wide raid lockouts.</summary>
    private readonly List<RaidLockout> _raidLockouts = [];

    /// <summary>Stores every ownership claim made on the character, one per requesting user.</summary>
    private readonly List<CharacterClaim> _claims = [];

    /// <summary>Stores the professions from the latest complete skill-list read.</summary>
    private readonly List<Profession> _professions = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Character()
        : base(new CharacterId(Guid.NewGuid()))
    {
        Name = CharacterName.Create("Unknown");
    }

    /// <summary>Initializes a new instance of the <see cref="Character"/> class.</summary>
    /// <param name="id">The character identifier.</param>
    /// <param name="realm">The Warmane realm.</param>
    /// <param name="name">The normalized character name.</param>
    /// <param name="wowClass">The WotLK class.</param>
    /// <param name="race">The WotLK race.</param>
    /// <param name="faction">The faction.</param>
    /// <param name="level">The current character level.</param>
    private Character(CharacterId id, WarmaneRealm realm, CharacterName name, WowClass wowClass, WowRace race, Faction faction, int level)
        : base(id)
    {
        Realm = realm;
        Name = name;
        Class = wowClass;
        Race = race;
        Faction = faction;
        Level = level;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the verified owner identifier when character ownership has been proven.</summary>
    public UserId? OwnerId { get; private set; }

    /// <summary>Gets the Warmane realm containing the character.</summary>
    public WarmaneRealm Realm { get; private set; }

    /// <summary>Gets the normalized character name.</summary>
    public CharacterName Name { get; private set; }

    /// <summary>Gets the WotLK character class.</summary>
    public WowClass Class { get; private set; }

    /// <summary>Gets the WotLK race.</summary>
    public WowRace Race { get; private set; }

    /// <summary>Gets the character faction.</summary>
    public Faction Faction { get; private set; }

    /// <summary>Gets the current character level.</summary>
    public int Level { get; private set; }

    /// <summary>Gets the optional current guild name.</summary>
    public string? GuildName { get; private set; }

    /// <summary>Gets a value indicating whether a user owns the character through an approved claim.</summary>
    public bool IsOwnershipVerified { get; private set; }

    /// <summary>Gets the UTC timestamp of the latest successful Warmane Armory synchronization.</summary>
    public DateTimeOffset? LastArmorySynchronizedAtUtc { get; private set; }

    /// <summary>Gets the UTC timestamp of the latest successful WoW addon synchronization.</summary>
    public DateTimeOffset? LastAddonSynchronizedAtUtc { get; private set; }

    /// <summary>Gets the UTC observation instant of the latest accepted complete saved-instance scan, the only evidence for raid saves.</summary>
    public DateTimeOffset? LastCompleteRaidSaveScanAtUtc { get; private set; }

    /// <summary>Gets the UTC instant of the latest saved-instance scan that could not read every save.</summary>
    public DateTimeOffset? LastIncompleteRaidSaveScanAtUtc { get; private set; }

    /// <summary>Gets the synchronized raid-capable loadouts.</summary>
    public IReadOnlyCollection<Loadout> Loadouts => _loadouts.AsReadOnly();

    /// <summary>Gets the current character-wide raid lockouts.</summary>
    public IReadOnlyCollection<RaidLockout> RaidLockouts => _raidLockouts.AsReadOnly();

    /// <summary>Gets the ownership claims made on the character.</summary>
    public IReadOnlyCollection<CharacterClaim> Claims => _claims.AsReadOnly();

    /// <summary>Gets the professions from the latest complete skill-list read, in the game's order.</summary>
    public IReadOnlyCollection<Profession> Professions => _professions.AsReadOnly();

    /// <summary>Gets the UTC observation instant of the latest complete skill-list read.</summary>
    public DateTimeOffset? LastProfessionsSynchronizedAtUtc { get; private set; }

    /// <summary>Gets who in the community may see the profile: the community until the owner chooses otherwise.</summary>
    public CharacterVisibility Visibility { get; } = CharacterVisibility.Community;
    #endregion Properties

    #region Factory Methods
    /// <summary>Imports a character discovered from Warmane or the addon.</summary>
    /// <param name="realm">The Warmane realm.</param>
    /// <param name="name">The normalized character name.</param>
    /// <param name="wowClass">The WotLK class.</param>
    /// <param name="race">The WotLK race.</param>
    /// <param name="faction">The faction.</param>
    /// <param name="level">The current level.</param>
    /// <returns>The imported character aggregate.</returns>
    public static Character Import(WarmaneRealm realm, CharacterName name, WowClass wowClass, WowRace race, Faction faction, int level)
    {
        EnsureLevel(level);
        var character = new Character(new CharacterId(Guid.NewGuid()), realm, name, wowClass, race, faction, level);
        character.RaiseDomainEvent(new CharacterImported(character.Id, realm.ToString(), name.Value));
        return character;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Records that a user's synchronized data discovered the character, opening or refreshing their claim.</summary>
    /// <param name="userId">The user whose paired companion uploaded the character.</param>
    /// <param name="requestedAtUtc">The UTC instant of the discovery.</param>
    /// <returns>
    /// The user's claim identifier. A new claim is pending, or a conflict when another user already owns the character.
    /// An existing claim is returned unchanged: a repeated upload never approves, reopens or transfers ownership.
    /// </returns>
    public Result<CharacterClaimId> RequestClaim(UserId userId, DateTimeOffset requestedAtUtc)
    {
        var existingClaim = FindClaim(userId);
        if (existingClaim is not null)
        {
            return existingClaim.Id;
        }

        var state = IsOwnedByAnotherUser(userId) ? CharacterClaimState.Conflict : CharacterClaimState.Pending;
        var claim = CharacterClaim.Create(userId, state, requestedAtUtc);
        _claims.Add(claim);
        return claim.Id;
    }

    /// <summary>Approves the user's pending claim, making the user the character's owner.</summary>
    /// <param name="userId">The user approving their own claim.</param>
    /// <param name="decidedAtUtc">The UTC instant of the approval.</param>
    /// <returns>
    /// Success, or a failure: <see cref="CharacterErrors.ClaimNotFound"/> (<see cref="ResultExceptionType.NotFound"/>),
    /// <see cref="CharacterErrors.ClaimNotPending"/> or <see cref="CharacterErrors.OwnedByAnotherUser"/>
    /// (<see cref="ResultExceptionType.Conflict"/>). In the last case the claim moves to conflict review.
    /// </returns>
    public Result ApproveClaim(UserId userId, DateTimeOffset decidedAtUtc)
    {
        var pendingClaim = FindPendingClaim(userId);
        if (pendingClaim.IsFailure)
        {
            return pendingClaim;
        }

        var claim = pendingClaim.Value;
        if (IsOwnedByAnotherUser(userId))
        {
            claim.Decide(CharacterClaimState.Conflict, decidedAtUtc);
            return Result.Failure(CharacterErrors.OwnedByAnotherUser, ResultExceptionType.Conflict);
        }

        claim.Decide(CharacterClaimState.Approved, decidedAtUtc);
        OwnerId = userId;
        IsOwnershipVerified = true;
        RaiseDomainEvent(new CharacterClaimed(Id, userId));
        return Result.Success();
    }

    /// <summary>Rejects the user's pending claim so the character never appears among their signup choices.</summary>
    /// <param name="userId">The user rejecting their own claim.</param>
    /// <param name="decidedAtUtc">The UTC instant of the rejection.</param>
    /// <returns>
    /// Success, or <see cref="CharacterErrors.ClaimNotFound"/> (<see cref="ResultExceptionType.NotFound"/>) or
    /// <see cref="CharacterErrors.ClaimNotPending"/> (<see cref="ResultExceptionType.Conflict"/>).
    /// </returns>
    public Result RejectClaim(UserId userId, DateTimeOffset decidedAtUtc)
    {
        var pendingClaim = FindPendingClaim(userId);
        if (pendingClaim.IsFailure)
        {
            return pendingClaim;
        }

        pendingClaim.Value.Decide(CharacterClaimState.Rejected, decidedAtUtc);
        return Result.Success();
    }

    /// <summary>Refreshes public identity data retrieved from the Warmane Armory.</summary>
    /// <param name="wowClass">The current class.</param>
    /// <param name="race">The current race.</param>
    /// <param name="faction">The current faction.</param>
    /// <param name="level">The current level.</param>
    /// <param name="guildName">The optional current guild name.</param>
    /// <param name="synchronizedAtUtc">The UTC synchronization timestamp.</param>
    public void RefreshArmoryIdentity(WowClass wowClass, WowRace race, Faction faction, int level, string? guildName, DateTimeOffset synchronizedAtUtc)
    {
        EnsureLevel(level);
        EnsureTimestamp(synchronizedAtUtc);
        Class = wowClass;
        Race = race;
        Faction = faction;
        Level = level;
        GuildName = string.IsNullOrWhiteSpace(guildName) ? null : guildName.Trim();
        LastArmorySynchronizedAtUtc = synchronizedAtUtc;
    }

    /// <summary>Creates or refreshes a loadout from synchronized game data.</summary>
    /// <param name="loadoutId">The existing loadout identifier, or <see langword="null"/> to create one.</param>
    /// <param name="name">The equipment-set or loadout name.</param>
    /// <param name="role">The raid role.</param>
    /// <param name="isPrimary">Whether the loadout is the preferred primary loadout.</param>
    /// <param name="gearScore">The calculated GearScore.</param>
    /// <param name="talentConfiguration">The exact talents and glyphs.</param>
    /// <param name="stats">The observed combat statistics.</param>
    /// <param name="gearItems">The exact equipment set.</param>
    /// <param name="source">The source of synchronized data.</param>
    /// <param name="synchronizedAtUtc">The synchronization timestamp.</param>
    /// <returns>The synchronized loadout identifier.</returns>
    public LoadoutId SynchronizeLoadout(
        LoadoutId? loadoutId,
        string name,
        CharacterRole role,
        bool isPrimary,
        GearScore gearScore,
        TalentConfiguration talentConfiguration,
        CombatStats stats,
        IEnumerable<GearItem> gearItems,
        CharacterDataSource source,
        DateTimeOffset synchronizedAtUtc)
    {
        EnsureTimestamp(synchronizedAtUtc);
        var loadout = loadoutId is null ? null : _loadouts.SingleOrDefault(candidate => candidate.Id == loadoutId);
        if (loadoutId is not null && loadout is null)
        {
            throw new UnknownDomainException("The requested character loadout does not exist.");
        }

        if (loadout is null)
        {
            loadout = Loadout.Create(name, role, isPrimary);
            _loadouts.Add(loadout);
        }

        if (isPrimary)
        {
            foreach (var existingLoadout in _loadouts.Where(candidate => candidate.Id != loadout.Id && candidate.IsPrimary))
            {
                existingLoadout.Refresh(
                    existingLoadout.Name,
                    existingLoadout.Role,
                    false,
                    existingLoadout.GearScore,
                    existingLoadout.TalentConfiguration,
                    existingLoadout.Stats,
                    existingLoadout.GearItems,
                    existingLoadout.Source,
                    existingLoadout.LastSynchronizedAtUtc);
            }
        }

        loadout.Refresh(name, role, isPrimary, gearScore, talentConfiguration, stats, gearItems, source, synchronizedAtUtc);
        LastAddonSynchronizedAtUtc = source == CharacterDataSource.WowAddon ? synchronizedAtUtc : LastAddonSynchronizedAtUtc;
        LastArmorySynchronizedAtUtc = source == CharacterDataSource.WarmaneArmory ? synchronizedAtUtc : LastArmorySynchronizedAtUtc;
        RaiseDomainEvent(new LoadoutSynchronized(Id, loadout.Id, gearScore.Value, synchronizedAtUtc));
        return loadout.Id;
    }

    /// <summary>Replaces the character's raid saves with a complete addon saved-instance scan.</summary>
    /// <param name="raidLockouts">Every save the scan found; an empty set is a verified absence of saves.</param>
    /// <param name="observedAtUtc">The UTC instant the game data was observed.</param>
    /// <returns>
    /// Success, or <see cref="CharacterErrors.RaidSaveScanOutdated"/> (<see cref="ResultExceptionType.Conflict"/>) when a
    /// newer complete scan was already accepted; the newer raid saves are then kept.
    /// </returns>
    public Result RecordCompleteRaidSaveScan(IEnumerable<RaidLockout> raidLockouts, DateTimeOffset observedAtUtc)
    {
        EnsureTimestamp(observedAtUtc);
        if (observedAtUtc < LastCompleteRaidSaveScanAtUtc)
        {
            return Result.Failure(CharacterErrors.RaidSaveScanOutdated, ResultExceptionType.Conflict);
        }

        _raidLockouts.Clear();
        _raidLockouts.AddRange(raidLockouts.OrderBy(lockout => lockout.Instance).ThenBy(lockout => lockout.Difficulty));
        LastCompleteRaidSaveScanAtUtc = observedAtUtc;
        LastAddonSynchronizedAtUtc = Latest(LastAddonSynchronizedAtUtc, observedAtUtc);
        RaiseDomainEvent(new RaidLockoutsSynchronized(Id, observedAtUtc));
        return Result.Success();
    }

    /// <summary>Records an addon saved-instance scan that could not read every save.</summary>
    /// <param name="observedAtUtc">The UTC instant the scan was attempted.</param>
    /// <remarks>The last complete raid saves are kept: a partial list is never evidence that a save is gone.</remarks>
    public void RecordIncompleteRaidSaveScan(DateTimeOffset observedAtUtc)
    {
        EnsureTimestamp(observedAtUtc);
        LastIncompleteRaidSaveScanAtUtc = Latest(LastIncompleteRaidSaveScanAtUtc, observedAtUtc);
    }

    /// <summary>Replaces the character's professions with a complete read of the addon's skill list.</summary>
    /// <param name="professions">Every profession and secondary skill read; an empty set means the character has none.</param>
    /// <param name="observedAtUtc">The UTC instant the game data was observed.</param>
    /// <returns>
    /// <see langword="true"/> when the read was recorded; <see langword="false"/> when a newer read was already
    /// recorded, whose professions are then kept.
    /// </returns>
    /// <exception cref="DomainException">Thrown when a profession has no name or a skill outside its range.</exception>
    public bool SynchronizeProfessions(IEnumerable<Profession> professions, DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(professions);
        EnsureTimestamp(observedAtUtc);
        var read = professions.ToList();
        read.ForEach(EnsureProfession);
        if (observedAtUtc < LastProfessionsSynchronizedAtUtc)
        {
            return false;
        }

        _professions.Clear();
        _professions.AddRange(read);
        LastProfessionsSynchronizedAtUtc = observedAtUtc;
        LastAddonSynchronizedAtUtc = Latest(LastAddonSynchronizedAtUtc, observedAtUtc);
        return true;
    }

    /// <summary>Determines whether the character is currently saved to the requested raid and difficulty.</summary>
    /// <param name="instance">The raid instance.</param>
    /// <param name="difficulty">The raid difficulty.</param>
    /// <param name="nowUtc">The UTC instant used when evaluating expiration.</param>
    /// <returns><see langword="true"/> when an unexpired matching lockout exists; otherwise <see langword="false"/>.</returns>
    public bool IsSavedTo(RaidInstance instance, RaidDifficulty difficulty, DateTimeOffset nowUtc) =>
        _raidLockouts.Any(lockout => lockout.Instance == instance && lockout.Difficulty == difficulty && lockout.ResetsAtUtc > nowUtc);
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures the character level is supported by the initial WotLK model.</summary>
    /// <param name="level">The level to validate.</param>
    /// <exception cref="DomainException">Thrown when the level is outside the supported range.</exception>
    private static void EnsureLevel(int level)
    {
        const int MaximumWotlkLevel = 80;
        if (level < 1 || level > MaximumWotlkLevel)
        {
            throw new UnknownDomainException("Character level must be between 1 and 80 for WotLK.");
        }
    }

    /// <summary>Ensures synchronized timestamps are not implausibly in the future.</summary>
    /// <param name="synchronizedAtUtc">The UTC synchronization timestamp.</param>
    /// <exception cref="DomainException">Thrown when the timestamp is in the future beyond tolerated clock drift.</exception>
    private static void EnsureTimestamp(DateTimeOffset synchronizedAtUtc)
    {
        var maximumAcceptedTimestamp = DateTimeOffset.UtcNow.AddMinutes(5);
        if (synchronizedAtUtc > maximumAcceptedTimestamp)
        {
            throw new UnknownDomainException("Synchronization timestamp cannot be in the future.");
        }
    }

    /// <summary>Ensures a profession read from the game has a name and a skill within its training.</summary>
    /// <param name="profession">The profession to validate.</param>
    /// <exception cref="DomainException">Thrown when the name is blank or too long, or the skill is out of range.</exception>
    private static void EnsureProfession(Profession profession)
    {
        if (string.IsNullOrWhiteSpace(profession.Name) || profession.Name.Length > Profession.MaximumNameLength)
        {
            throw new UnknownDomainException($"A profession needs a name of at most {Profession.MaximumNameLength} characters.");
        }

        if (profession.Rank < 0 || profession.MaxRank < profession.Rank)
        {
            throw new UnknownDomainException("A profession's skill must be between 0 and its maximum skill.");
        }
    }
    #endregion Invariants

    #region Private Helpers
    /// <summary>Returns the later of a recorded instant and a new observation.</summary>
    /// <param name="recordedAtUtc">The recorded instant, if any.</param>
    /// <param name="observedAtUtc">The new observation.</param>
    /// <returns>The later instant.</returns>
    private static DateTimeOffset Latest(DateTimeOffset? recordedAtUtc, DateTimeOffset observedAtUtc) =>
        recordedAtUtc > observedAtUtc ? recordedAtUtc.Value : observedAtUtc;

    /// <summary>Finds the claim made by a user.</summary>
    /// <param name="userId">The requesting user.</param>
    /// <returns>The user's claim, or <see langword="null"/> when the user never claimed the character.</returns>
    private CharacterClaim? FindClaim(UserId userId) => _claims.SingleOrDefault(claim => claim.RequestedByUserId == userId);

    /// <summary>Finds the user's claim and requires it to be pending.</summary>
    /// <param name="userId">The requesting user.</param>
    /// <returns>The pending claim, or a not-found or not-pending failure.</returns>
    private Result<CharacterClaim> FindPendingClaim(UserId userId)
    {
        var claim = FindClaim(userId);
        if (claim is null)
        {
            return Result.Failure<CharacterClaim>(CharacterErrors.ClaimNotFound, ResultExceptionType.NotFound);
        }

        return claim.State == CharacterClaimState.Pending
            ? claim
            : Result.Failure<CharacterClaim>(CharacterErrors.ClaimNotPending, ResultExceptionType.Conflict);
    }

    /// <summary>Determines whether a different user already owns the character.</summary>
    /// <param name="userId">The requesting user.</param>
    /// <returns><see langword="true"/> when another user owns the character.</returns>
    private bool IsOwnedByAnotherUser(UserId userId) => IsOwnershipVerified && OwnerId != userId;
    #endregion Private Helpers
}
