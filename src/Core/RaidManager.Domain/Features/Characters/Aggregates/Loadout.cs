using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Aggregates;

/// <summary>Represents one raid-capable gear, talent and stat configuration owned by a character.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Separates main-spec and off-spec readiness so raid leaders are not limited to the equipment currently worn on the Armory.
/// </remarks>
public sealed class Loadout : Entity<LoadoutId>
{
    #region Fields
    /// <summary>Stores the exact equipment captured for this loadout.</summary>
    private readonly List<GearItem> _gearItems = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Loadout()
        : base(new LoadoutId(Guid.NewGuid()))
    {
        Name = string.Empty;
        TalentConfiguration = new TalentConfiguration(string.Empty, 0, 0, 0, string.Empty, [], []);
        Stats = new CombatStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>Initializes a new instance of the <see cref="Loadout"/> class.</summary>
    /// <param name="id">The loadout identifier.</param>
    /// <param name="name">The player-visible equipment-set name.</param>
    /// <param name="role">The raid role supplied by the loadout.</param>
    /// <param name="isPrimary">Whether the loadout is the character's preferred primary raid loadout.</param>
    private Loadout(LoadoutId id, string name, CharacterRole role, bool isPrimary)
        : base(id)
    {
        Name = name;
        Role = role;
        IsPrimary = isPrimary;
        TalentConfiguration = new TalentConfiguration(string.Empty, 0, 0, 0, string.Empty, [], []);
        Stats = new CombatStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the player-visible loadout name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the raid role supplied by the loadout.</summary>
    public CharacterRole Role { get; private set; }

    /// <summary>Gets a value indicating whether this is the character's preferred primary raid loadout.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets the latest calculated GearScore.</summary>
    public GearScore GearScore { get; private set; }

    /// <summary>Gets the talent and glyph configuration associated with the loadout.</summary>
    public TalentConfiguration TalentConfiguration { get; private set; }

    /// <summary>Gets the latest combat statistics observed while this loadout was equipped.</summary>
    public CombatStats Stats { get; private set; }

    /// <summary>Gets the source that most recently synchronized this loadout.</summary>
    public CharacterDataSource Source { get; private set; }

    /// <summary>Gets the UTC instant when this loadout was last synchronized.</summary>
    public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

    /// <summary>Gets the immutable view of items belonging to the loadout.</summary>
    public IReadOnlyCollection<GearItem> GearItems => _gearItems.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a new character loadout.</summary>
    /// <param name="name">The player-visible equipment-set name.</param>
    /// <param name="role">The raid role supplied by the loadout.</param>
    /// <param name="isPrimary">Whether this is the preferred primary raid loadout.</param>
    /// <returns>The new loadout.</returns>
    public static Loadout Create(string name, CharacterRole role, bool isPrimary)
    {
        EnsureName(name);
        return new Loadout(new LoadoutId(Guid.NewGuid()), name.Trim(), role, isPrimary);
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Refreshes the complete synchronized state for the loadout.</summary>
    /// <param name="name">The player-visible equipment-set name.</param>
    /// <param name="role">The raid role supplied by the loadout.</param>
    /// <param name="isPrimary">Whether the loadout is currently the preferred primary loadout.</param>
    /// <param name="gearScore">The calculated GearScore.</param>
    /// <param name="talentConfiguration">The exact talent and glyph configuration.</param>
    /// <param name="stats">The game-calculated stat snapshot.</param>
    /// <param name="gearItems">The exact item set.</param>
    /// <param name="source">The source of the synchronized data.</param>
    /// <param name="synchronizedAtUtc">The synchronization timestamp.</param>
    public void Refresh(
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
        EnsureName(name);
        EnsureSynchronizationTimestamp(synchronizedAtUtc);

        Name = name.Trim();
        Role = role;
        IsPrimary = isPrimary;
        GearScore = gearScore;
        TalentConfiguration = talentConfiguration;
        Stats = stats;
        Source = source;
        LastSynchronizedAtUtc = synchronizedAtUtc;

        _gearItems.Clear();
        _gearItems.AddRange(gearItems.OrderBy(item => item.Slot));
    }
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures a loadout name is present.</summary>
    /// <param name="name">The loadout name.</param>
    /// <exception cref="DomainException">Thrown when the name is missing.</exception>
    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new UnknownDomainException("Loadout name is required.");
        }
    }

    /// <summary>Ensures a synchronization timestamp is not in the future beyond normal clock drift.</summary>
    /// <param name="synchronizedAtUtc">The synchronization timestamp.</param>
    /// <exception cref="DomainException">Thrown when the timestamp is implausibly in the future.</exception>
    private static void EnsureSynchronizationTimestamp(DateTimeOffset synchronizedAtUtc)
    {
        var maximumAcceptedTimestamp = DateTimeOffset.UtcNow.AddMinutes(5);
        if (synchronizedAtUtc > maximumAcceptedTimestamp)
        {
            throw new UnknownDomainException("Loadout synchronization timestamp cannot be in the future.");
        }
    }
    #endregion Invariants
}
