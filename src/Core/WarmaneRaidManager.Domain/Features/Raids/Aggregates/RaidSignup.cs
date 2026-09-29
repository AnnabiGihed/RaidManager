using WarmaneRaidManager.Domain.Features.Raids.Enums;
using WarmaneRaidManager.Domain.Features.Raids.ValueObjects;
using WarmaneRaidManager.Domain.Features.Shared.Identifiers;

namespace WarmaneRaidManager.Domain.Features.Raids.Aggregates;

/// <summary>Represents one user's raid availability together with all character loadouts they are willing to bring.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Ensures multiple offered characters remain one participant signup rather than consuming multiple roster places.
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
    /// <param name="status">The initial availability status.</param>
    /// <param name="comment">The optional player comment.</param>
    private RaidSignup(RaidSignupId id, UserId userId, RaidSignupStatus status, string? comment)
        : base(id)
    {
        UserId = userId;
        Status = status;
        Comment = comment;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the platform user who submitted the signup.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets the current signup status.</summary>
    public RaidSignupStatus Status { get; private set; }

    /// <summary>Gets the optional participant comment visible to raid organizers.</summary>
    public string? Comment { get; private set; }

    /// <summary>Gets the verified character loadouts the participant is willing to bring.</summary>
    public IReadOnlyCollection<SignupOption> Options => _options.AsReadOnly();
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates a raid signup.</summary>
    /// <param name="userId">The participant user.</param>
    /// <param name="status">The availability status.</param>
    /// <param name="options">The offered character loadouts.</param>
    /// <param name="comment">The optional participant comment.</param>
    /// <returns>The created signup.</returns>
    public static RaidSignup Create(UserId userId, RaidSignupStatus status, IEnumerable<SignupOption> options, string? comment)
    {
        var optionList = options.Distinct().ToList();
        if (status != RaidSignupStatus.Declined && optionList.Count == 0)
        {
            throw new DomainException("An available raid signup must offer at least one character loadout.");
        }

        var signup = new RaidSignup(new RaidSignupId(Guid.NewGuid()), userId, status, string.IsNullOrWhiteSpace(comment) ? null : comment.Trim());
        signup._options.AddRange(optionList);
        return signup;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Refreshes the participant's availability, offered loadouts and comment.</summary>
    /// <param name="status">The replacement availability status.</param>
    /// <param name="options">The replacement offered loadouts.</param>
    /// <param name="comment">The optional participant comment.</param>
    public void Refresh(RaidSignupStatus status, IEnumerable<SignupOption> options, string? comment)
    {
        var optionList = options.Distinct().ToList();
        if (status != RaidSignupStatus.Declined && optionList.Count == 0)
        {
            throw new DomainException("An available raid signup must offer at least one character loadout.");
        }

        Status = status;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        _options.Clear();
        _options.AddRange(optionList);
    }
    #endregion Domain Behavior
}
