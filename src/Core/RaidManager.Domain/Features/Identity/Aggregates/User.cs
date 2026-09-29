using RaidManager.Domain.Features.Identity.Events;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Identity.Aggregates;

/// <summary>Represents the local application profile associated with exactly one Discord identity.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides the user aggregate without introducing local passwords or a second authentication identity.
/// </remarks>
public sealed class User : AggregateRoot<UserId>
{
    #region Constructors
    /// <summary>Initializes an empty instance for persistence materialization.</summary>
    private User()
        : base(new UserId(Guid.NewGuid()))
    {
        DiscordUserId = DiscordUserId.Create("1");
        DisplayName = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
    /// <param name="id">The application user identifier.</param>
    /// <param name="discordUserId">The external Discord identity.</param>
    /// <param name="displayName">The Discord display name.</param>
    /// <param name="avatarUrl">The optional Discord avatar URL.</param>
    private User(UserId id, DiscordUserId discordUserId, string displayName, string? avatarUrl)
        : base(id)
    {
        DiscordUserId = discordUserId;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the external Discord identifier used for authentication.</summary>
    public DiscordUserId DiscordUserId { get; private set; }

    /// <summary>Gets the latest Discord display name.</summary>
    public string DisplayName { get; private set; }

    /// <summary>Gets the optional Discord avatar URL.</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>Gets the optional IANA or Windows timezone identifier selected for local raid-time display.</summary>
    public string? TimeZoneId { get; private set; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Registers a local user profile for an authenticated Discord account.</summary>
    /// <param name="discordUserId">The external Discord identifier.</param>
    /// <param name="displayName">The Discord display name.</param>
    /// <param name="avatarUrl">The optional Discord avatar URL.</param>
    /// <returns>The registered user aggregate.</returns>
    public static User Register(DiscordUserId discordUserId, string displayName, string? avatarUrl)
    {
        EnsureDisplayName(displayName);
        var user = new User(new UserId(Guid.NewGuid()), discordUserId, displayName.Trim(), avatarUrl);
        user.RaiseDomainEvent(new UserRegistered(user.Id, discordUserId.Value, user.DisplayName));
        return user;
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Refreshes mutable profile information retrieved from Discord.</summary>
    /// <param name="displayName">The latest Discord display name.</param>
    /// <param name="avatarUrl">The latest optional Discord avatar URL.</param>
    public void UpdateDiscordProfile(string displayName, string? avatarUrl)
    {
        EnsureDisplayName(displayName);
        DisplayName = displayName.Trim();
        AvatarUrl = avatarUrl;
        RaiseDomainEvent(new DiscordProfileUpdated(Id, DisplayName));
    }

    /// <summary>Sets the timezone used to render raid times for the user.</summary>
    /// <param name="timeZoneId">The timezone identifier.</param>
    public void SetTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new UnknownDomainException("Timezone identifier is required.");
        }

        TimeZoneId = timeZoneId.Trim();
    }
    #endregion Domain Behavior

    #region Invariants
    /// <summary>Ensures a Discord display name is present.</summary>
    /// <param name="displayName">The display name to validate.</param>
    /// <exception cref="DomainException">Thrown when the display name is missing.</exception>
    private static void EnsureDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new UnknownDomainException("Discord display name is required.");
        }
    }
    #endregion Invariants
}
