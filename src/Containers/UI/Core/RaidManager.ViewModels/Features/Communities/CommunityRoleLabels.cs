namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Gives the label of each RaidManager role, as the community pages show it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Keeps one wording of the roles for the roles card and the members page.
/// </remarks>
public static class CommunityRoleLabels
{
    #region Fields
    /// <summary>Stores the labels by the roles' API names.</summary>
    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        ["Administrator"] = "Administrator",
        ["Officer"] = "Officer",
        ["RaidLeader"] = "Raid leader",
        ["Member"] = "Member",
    };
    #endregion Fields

    #region Public Methods
    /// <summary>Gives a role's label.</summary>
    /// <param name="role">The role's API name, such as <c>RaidLeader</c>.</param>
    /// <returns>The label, such as "Raid leader"; an unknown name is shown as it is.</returns>
    public static string For(string role) => Labels.GetValueOrDefault(role, role);
    #endregion Public Methods
}
