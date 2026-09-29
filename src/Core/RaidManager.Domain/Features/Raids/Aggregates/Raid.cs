using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.Events;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Aggregates;

/// <summary>Represents a scheduled raid, its signup pool and the final selected roster.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Owns raid lifecycle, signup and roster invariants independently from web and Discord presentation channels.
/// </remarks>
public sealed class Raid : AggregateRoot<RaidId>
{
    #region Fields
    /// <summary>Stores participant signups for the raid.</summary>
    private readonly List<RaidSignup> _signups = [];

    /// <summary>Stores one final selected character loadout per rostered user.</summary>
    private readonly List<RosterSelection> _rosterSelections = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private Raid()
        : base(new RaidId(Guid.NewGuid()))
    {
        CommunityId = new CommunityId(Guid.NewGuid());
        CreatedByUserId = new UserId(Guid.NewGuid());
        Requirements = new RaidRequirements(null, false, TimeSpan.FromDays(3));
    }

    /// <summary>Initializes a new instance of the <see cref="Raid"/> class.</summary>
    /// <param name="id">The raid identifier.</param>
    /// <param name="communityId">The organizing community.</param>
    /// <param name="createdByUserId">The creating raid leader.</param>
    /// <param name="instance">The raid instance.</param>
    /// <param name="difficulty">The raid size and difficulty.</param>
    /// <param name="startsAtUtc">The scheduled start instant.</param>
    /// <param name="signupDeadlineUtc">The signup deadline.</param>
    /// <param name="requirements">The automatic signup requirements.</param>
    /// <param name="description">The optional organizer description.</param>
    private Raid(
        RaidId id,
        CommunityId communityId,
        UserId createdByUserId,
        RaidInstance instance,
        RaidDifficulty difficulty,
        DateTimeOffset startsAtUtc,
        DateTimeOffset signupDeadlineUtc,
        RaidRequirements requirements,
        string? description)
        : base(id)
    {
        CommunityId = communityId;
        CreatedByUserId = createdByUserId;
        Instance = instance;
        Difficulty = difficulty;
        StartsAtUtc = startsAtUtc;
        SignupDeadlineUtc = signupDeadlineUtc;
        Requirements = requirements;
        Description = description;
        Status = RaidStatus.Draft;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the organizing raiding community.</summary>
    public CommunityId CommunityId { get; private set; }

    /// <summary>Gets the user who created the raid.</summary>
    public UserId CreatedByUserId { get; private set; }

    /// <summary>Gets the raid instance.</summary>
    public RaidInstance Instance { get; private set; }

    /// <summary>Gets the raid size and difficulty.</summary>
    public RaidDifficulty Difficulty { get; private set; }

    /// <summary>Gets the scheduled UTC start instant.</summary>
    public DateTimeOffset StartsAtUtc { get; private set; }

    /// <summary>Gets the UTC signup deadline.</summary>
    public DateTimeOffset SignupDeadlineUtc { get; private set; }

    /// <summary>Gets the raid eligibility requirements.</summary>
    public RaidRequirements Requirements { get; private set; }

    /// <summary>Gets the optional raid-leader description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the current raid lifecycle status.</summary>
    public RaidStatus Status { get; private set; }

    /// <summary>Gets participant signups.</summary>
    public IReadOnlyCollection<RaidSignup> Signups => _signups.AsReadOnly();

    /// <summary>Gets final roster selections.</summary>
    public IReadOnlyCollection<RosterSelection> RosterSelections => _rosterSelections.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a draft raid event.</summary>
    /// <param name="communityId">The organizing community.</param>
    /// <param name="createdByUserId">The creating raid leader.</param>
    /// <param name="instance">The raid instance.</param>
    /// <param name="difficulty">The raid size and difficulty.</param>
    /// <param name="startsAtUtc">The scheduled start instant.</param>
    /// <param name="signupDeadlineUtc">The signup deadline.</param>
    /// <param name="requirements">The automatic signup requirements.</param>
    /// <param name="description">The optional organizer description.</param>
    /// <returns>The draft raid.</returns>
    public static Raid Create(
        CommunityId communityId,
        UserId createdByUserId,
        RaidInstance instance,
        RaidDifficulty difficulty,
        DateTimeOffset startsAtUtc,
        DateTimeOffset signupDeadlineUtc,
        RaidRequirements requirements,
        string? description)
    {
        EnsureSchedule(startsAtUtc, signupDeadlineUtc);
        var raid = new Raid(
            new RaidId(Guid.NewGuid()),
            communityId,
            createdByUserId,
            instance,
            difficulty,
            startsAtUtc,
            signupDeadlineUtc,
            requirements,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim());
        raid.RaiseDomainEvent(new RaidCreated(raid.Id, communityId, instance.ToString(), startsAtUtc));
        return raid;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Opens a draft raid for player signups.</summary>
    public void OpenForSignups()
    {
        if (Status != RaidStatus.Draft)
        {
            throw new UnknownDomainException("Only a draft raid can be opened for signups.");
        }

        Status = RaidStatus.OpenForSignups;
        RaiseDomainEvent(new RaidOpenedForSignups(Id));
    }

    /// <summary>Creates or refreshes a participant signup.</summary>
    /// <param name="userId">The participant user.</param>
    /// <param name="availability">Whether the participant plans to attend.</param>
    /// <param name="lateArrivalUtc">The expected arrival instant; required for a late signup, forbidden otherwise.</param>
    /// <param name="options">The verified character loadouts offered by the participant.</param>
    /// <param name="comment">The optional participant comment.</param>
    /// <param name="nowUtc">The UTC instant at which the signup is submitted.</param>
    /// <returns>The participant signup identifier.</returns>
    public RaidSignupId SubmitSignup(
        UserId userId,
        RaidAvailability availability,
        DateTimeOffset? lateArrivalUtc,
        IEnumerable<SignupOption> options,
        string? comment,
        DateTimeOffset nowUtc)
    {
        if (Status != RaidStatus.OpenForSignups)
        {
            throw new UnknownDomainException("Raid is not open for signups.");
        }

        if (nowUtc > SignupDeadlineUtc)
        {
            throw new UnknownDomainException("Raid signup deadline has passed.");
        }

        var signup = _signups.SingleOrDefault(candidate => candidate.UserId == userId);
        if (signup is null)
        {
            signup = RaidSignup.Create(userId, availability, lateArrivalUtc, options, comment);
            _signups.Add(signup);
        }
        else
        {
            signup.Refresh(availability, lateArrivalUtc, options, comment);
        }

        RaiseDomainEvent(new RaidSignupSubmitted(Id, signup.Id, userId));
        return signup.Id;
    }

    /// <summary>Selects exactly one offered character loadout for a user and assigns it to a raid-group position.</summary>
    /// <param name="userId">The participant user.</param>
    /// <param name="characterId">The selected offered character.</param>
    /// <param name="loadoutId">The selected offered loadout.</param>
    /// <param name="groupNumber">The target one-based raid group.</param>
    /// <param name="position">The target one-based group position.</param>
    public void SelectRosterOption(UserId userId, CharacterId characterId, LoadoutId loadoutId, int groupNumber, int position)
    {
        var signup = _signups.SingleOrDefault(candidate => candidate.UserId == userId)
            ?? throw new UnknownDomainException("User must have a raid signup before being selected for the roster.");

        if (!signup.Options.Contains(new SignupOption(characterId, loadoutId)))
        {
            throw new UnknownDomainException("Selected character loadout was not offered by this user.");
        }

        if (_rosterSelections.Any(selection => selection.GroupNumber == groupNumber && selection.Position == position && selection.UserId != userId))
        {
            throw new UnknownDomainException("Requested raid roster position is already occupied.");
        }

        var existingSelection = _rosterSelections.SingleOrDefault(selection => selection.UserId == userId);
        if (existingSelection is not null)
        {
            _rosterSelections.Remove(existingSelection);
        }

        _rosterSelections.Add(RosterSelection.Create(userId, characterId, loadoutId, groupNumber, position));
        RaiseDomainEvent(new RosterSelectionChanged(Id, userId, characterId, loadoutId));
    }

    /// <summary>Publishes the current selected roster.</summary>
    public void PublishRoster()
    {
        if (Status != RaidStatus.OpenForSignups)
        {
            throw new UnknownDomainException("Only a raid open for signups can publish its roster.");
        }

        if (_rosterSelections.Count == 0)
        {
            throw new UnknownDomainException("A raid roster cannot be published without selected participants.");
        }

        Status = RaidStatus.RosterPublished;
    }
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures signup closes before the raid starts.</summary>
    /// <param name="startsAtUtc">The raid start instant.</param>
    /// <param name="signupDeadlineUtc">The signup deadline.</param>
    /// <exception cref="DomainException">Thrown when the signup deadline is not before raid start.</exception>
    private static void EnsureSchedule(DateTimeOffset startsAtUtc, DateTimeOffset signupDeadlineUtc)
    {
        if (signupDeadlineUtc >= startsAtUtc)
        {
            throw new UnknownDomainException("Raid signup deadline must be before raid start time.");
        }
    }
    #endregion Invariants
}
