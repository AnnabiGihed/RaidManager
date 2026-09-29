using WarmaneRaidManager.Domain.Features.Characters.Enums;
using WarmaneRaidManager.Domain.Features.Shared.Enums;
using WarmaneRaidManager.Domain.Features.Characters.Events;
using WarmaneRaidManager.Domain.Features.Characters.ValueObjects;
using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Characters.Aggregates;

/// <summary>Represents a Warmane character and the raid-relevant state shared by all of its loadouts.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
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

    /// <summary>Gets a value indicating whether ownership was verified through a trusted proof flow.</summary>
    public bool IsOwnershipVerified { get; private set; }

    /// <summary>Gets the UTC timestamp of the latest successful Warmane Armory synchronization.</summary>
    public DateTimeOffset? LastArmorySynchronizedAtUtc { get; private set; }

    /// <summary>Gets the UTC timestamp of the latest successful WoW addon synchronization.</summary>
    public DateTimeOffset? LastAddonSynchronizedAtUtc { get; private set; }

    /// <summary>Gets the synchronized raid-capable loadouts.</summary>
    public IReadOnlyCollection<Loadout> Loadouts => _loadouts.AsReadOnly();

    /// <summary>Gets the current character-wide raid lockouts.</summary>
    public IReadOnlyCollection<RaidLockout> RaidLockouts => _raidLockouts.AsReadOnly();
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
    /// <summary>Claims the character for a user after trusted ownership verification.</summary>
    /// <param name="ownerId">The verified owner.</param>
    public void Claim(UserId ownerId)
    {
        if (IsOwnershipVerified && OwnerId != ownerId)
        {
            throw new DomainException("Character is already claimed by another verified user.");
        }

        OwnerId = ownerId;
        IsOwnershipVerified = true;
        RaiseDomainEvent(new CharacterClaimed(Id, ownerId));
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
            throw new DomainException("The requested character loadout does not exist.");
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

    /// <summary>Replaces the character's current raid-lockout snapshot with addon-synchronized values.</summary>
    /// <param name="raidLockouts">The complete current set of saved raid instances.</param>
    /// <param name="synchronizedAtUtc">The synchronization timestamp.</param>
    public void SynchronizeRaidLockouts(IEnumerable<RaidLockout> raidLockouts, DateTimeOffset synchronizedAtUtc)
    {
        EnsureTimestamp(synchronizedAtUtc);
        _raidLockouts.Clear();
        _raidLockouts.AddRange(raidLockouts.OrderBy(lockout => lockout.Instance).ThenBy(lockout => lockout.Difficulty));
        LastAddonSynchronizedAtUtc = synchronizedAtUtc;
        RaiseDomainEvent(new RaidLockoutsSynchronized(Id, synchronizedAtUtc));
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
            throw new DomainException("Character level must be between 1 and 80 for WotLK.");
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
            throw new DomainException("Synchronization timestamp cannot be in the future.");
        }
    }
    #endregion Invariants
}
