namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Describes a user's current membership in a Discord server, as Discord reports it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Separates "not in the server" from "Discord didn't answer": the first is an answer and can be cached, the
/// second is a failure that must refuse the action (ADR-0022).
/// </remarks>
public sealed record DiscordMembership
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DiscordMembership"/> class.</summary>
    /// <param name="isMember">Whether the user is in the server.</param>
    /// <param name="roleIds">The user's Discord role snowflakes in the server.</param>
    private DiscordMembership(bool isMember, IReadOnlyList<string> roleIds)
    {
        IsMember = isMember;
        RoleIds = roleIds;
    }
    #endregion Constructors

    #region Static Instances
    /// <summary>Gets the membership of a user who isn't in the server, or of any user once the bot was removed from it.</summary>
    public static DiscordMembership NotMember { get; } = new(false, []);
    #endregion Static Instances

    #region Properties
    /// <summary>Gets a value indicating whether the user is in the server.</summary>
    public bool IsMember { get; }

    /// <summary>Gets the user's Discord role snowflakes in the server; empty when they have none or aren't a member.</summary>
    public IReadOnlyList<string> RoleIds { get; }
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates the membership of a user who is in the server.</summary>
    /// <param name="roleIds">The user's Discord role snowflakes in the server.</param>
    /// <returns>The membership.</returns>
    public static DiscordMembership Member(IEnumerable<string> roleIds) => new(true, roleIds.ToList());
    #endregion Factory Methods
}
