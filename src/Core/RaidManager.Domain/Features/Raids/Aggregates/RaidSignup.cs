using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Aggregates;

/// <summary>Represents one user's raid response: availability plus all character loadouts they are willing to bring.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Ensures multiple offered characters remain one participant signup rather than consuming multiple roster places.
/// Availability is the player's statement only; selection and bench are officer decisions kept on the raid roster.
/// </remarks>
public sealed class RaidSignup : Entity<RaidSignupId>
{
    #region Fields
    /// <summary>Stores the verified character-loadout choices offered by the user.</summary>
    private readonly List<SignupOption> _options = [];
    #endregion Fields

    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private RaidSignup()
        : base(new RaidSignupId(Guid.NewGuid()))
    {
        UserId = new UserId(Guid.NewGuid());
    }

    /// <summary>Initializes a new instance of the <see cref="RaidSignup"/> class.</summary>
    /// <param name="id">The signup identifier.</param>
    /// <param name="userId">The participant user identifier.</param>
    private RaidSignup(RaidSignupId id, UserId userId)
        : base(id)
    {
        UserId = userId;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the platform user who submitted the signup.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets whether the participant plans to attend.</summary>
    public RaidAvailability Availability { get; private set; }

    /// <summary>Gets the expected UTC arrival instant for a late signup; otherwise <see langword="null"/>.</summary>
    public DateTimeOffset? LateArrivalUtc { get; private set; }

    /// <summary>Gets the optional participant comment visible to raid organizers.</summary>
    public string? Comment { get; private set; }

    /// <summary>Gets the verified character loadouts the participant is willing to bring.</summary>
    public IReadOnlyCollection<SignupOption> Options => _options.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a raid signup.</summary>
    /// <param name="userId">The participant user.</param>
    /// <param name="availability">Whether the participant plans to attend.</param>
    /// <param name="lateArrivalUtc">The expected arrival instant; required for a late signup, forbidden otherwise.</param>
    /// <param name="options">The offered character loadouts.</param>
    /// <param name="comment">The optional participant comment.</param>
    /// <returns>The created signup.</returns>
    /// <exception cref="DomainException">Thrown when the availability, arrival time and options are inconsistent.</exception>
    public static RaidSignup Create(
        UserId userId,
        RaidAvailability availability,
        DateTimeOffset? lateArrivalUtc,
        IEnumerable<SignupOption> options,
        string? comment)
    {
        var signup = new RaidSignup(new RaidSignupId(Guid.NewGuid()), userId);
        signup.Refresh(availability, lateArrivalUtc, options, comment);
        return signup;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Refreshes the participant's availability, offered loadouts and comment.</summary>
    /// <param name="availability">The replacement availability.</param>
    /// <param name="lateArrivalUtc">The expected arrival instant; required for a late signup, forbidden otherwise.</param>
    /// <param name="options">The replacement offered loadouts.</param>
    /// <param name="comment">The optional participant comment.</param>
    /// <exception cref="DomainException">Thrown when the availability, arrival time and options are inconsistent.</exception>
    public void Refresh(RaidAvailability availability, DateTimeOffset? lateArrivalUtc, IEnumerable<SignupOption> options, string? comment)
    {
        var optionList = options.Distinct().ToList();
        if (availability != RaidAvailability.Declined && optionList.Count == 0)
        {
            throw new UnknownDomainException("A signup that is not declined must offer at least one character loadout.");
        }

        if ((availability == RaidAvailability.Late) != lateArrivalUtc.HasValue)
        {
            throw new UnknownDomainException("A late arrival time is required for a late signup and only for a late signup.");
        }

        Availability = availability;
        LateArrivalUtc = lateArrivalUtc;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        _options.Clear();
        _options.AddRange(optionList);
    }
    #endregion Domain Behavior
}
