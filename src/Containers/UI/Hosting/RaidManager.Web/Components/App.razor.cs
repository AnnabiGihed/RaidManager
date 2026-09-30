namespace RaidManager.Web.Components;

/// <summary>Renders the HTML document shell, the Radzen theme, and the interactive router.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Applies the InteractiveServer render mode to the whole application, as ADR-0012 decides.
/// </remarks>
public partial class App
{
    #region Properties
    /// <summary>Gets the Radzen assembly version, used to bust the browser cache of its script.</summary>
    private static string RadzenVersion => typeof(Radzen.Colors).Assembly.GetName().Version?.ToString() ?? "0";
    #endregion Properties
}
