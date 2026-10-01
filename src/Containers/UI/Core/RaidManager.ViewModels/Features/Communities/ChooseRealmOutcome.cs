namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Names how finishing the link ended.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Tells the realm page where to go next.
/// </remarks>
public enum ChooseRealmOutcome
{
    /// <summary>The community was linked; open its page.</summary>
    Linked,

    /// <summary>Someone linked the server in the meantime; show it as already linked.</summary>
    AlreadyLinked,

    /// <summary>Nothing was linked; the page stays with a retry.</summary>
    Failed,
}
