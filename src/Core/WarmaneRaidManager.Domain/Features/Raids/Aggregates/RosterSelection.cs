using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Aggregates;

/// <summary>Represents the single character loadout selected for one user in a raid roster.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Guarantees that a user offering multiple character options still occupies at most one final raid-roster place.
/// </remarks>
public sealed class RosterSelection : Entity<RosterSelectionId>
{
    #region Constants
    /// <summary>Defines the maximum number of five-player groups in a twenty-five-player WotLK raid.</summary>
    private const int MaximumRaidGroup = 5;

    /// <summary>Defines the maximum position inside one five-player group.</summary>
    private const int MaximumPositionInGroup = 5;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private RosterSelection()
        : base(new RosterSelectionId(Guid.NewGuid()))
    {
        UserId = new UserId(Guid.NewGuid());
        CharacterId = new CharacterId(Guid.NewGuid());
        LoadoutId = new LoadoutId(Guid.NewGuid());
    }

    /// <summary>Initializes a new instance of the <see cref="RosterSelection"/> class.</summary>
    /// <param name="id">The roster-selection identifier.</param>
    /// <param name="userId">The selected participant.</param>
    /// <param name="characterId">The selected character.</param>
    /// <param name="loadoutId">The selected loadout.</param>
    /// <param name="groupNumber">The raid group number.</param>
    /// <param name="position">The position inside the group.</param>
    private RosterSelection(RosterSelectionId id, UserId userId, CharacterId characterId, LoadoutId loadoutId, int groupNumber, int position)
        : base(id)
    {
        UserId = userId;
        CharacterId = characterId;
        LoadoutId = loadoutId;
        GroupNumber = groupNumber;
        Position = position;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the selected participant.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets the selected character.</summary>
    public CharacterId CharacterId { get; private set; }

    /// <summary>Gets the selected loadout.</summary>
    public LoadoutId LoadoutId { get; private set; }

    /// <summary>Gets the one-based raid group number.</summary>
    public int GroupNumber { get; private set; }

    /// <summary>Gets the one-based position within the group.</summary>
    public int Position { get; private set; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a roster selection.</summary>
    /// <param name="userId">The selected participant.</param>
    /// <param name="characterId">The selected character.</param>
    /// <param name="loadoutId">The selected loadout.</param>
    /// <param name="groupNumber">The one-based raid group number.</param>
    /// <param name="position">The one-based position within the group.</param>
    /// <returns>The roster selection.</returns>
    public static RosterSelection Create(UserId userId, CharacterId characterId, LoadoutId loadoutId, int groupNumber, int position)
    {
        EnsurePosition(groupNumber, position);
        return new RosterSelection(new RosterSelectionId(Guid.NewGuid()), userId, characterId, loadoutId, groupNumber, position);
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Moves the roster selection to another group position.</summary>
    /// <param name="groupNumber">The replacement one-based raid group number.</param>
    /// <param name="position">The replacement one-based position.</param>
    public void Move(int groupNumber, int position)
    {
        EnsurePosition(groupNumber, position);
        GroupNumber = groupNumber;
        Position = position;
    }
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures the requested roster position is valid for a twenty-five-player raid layout.</summary>
    /// <param name="groupNumber">The one-based raid group number.</param>
    /// <param name="position">The one-based position within the group.</param>
    /// <exception cref="DomainException">Thrown when the position falls outside the supported layout.</exception>
    private static void EnsurePosition(int groupNumber, int position)
    {
        if (groupNumber < 1 || groupNumber > MaximumRaidGroup || position < 1 || position > MaximumPositionInGroup)
        {
            throw new DomainException("Roster group and position must both be between 1 and 5.");
        }
    }
    #endregion Invariants
}
