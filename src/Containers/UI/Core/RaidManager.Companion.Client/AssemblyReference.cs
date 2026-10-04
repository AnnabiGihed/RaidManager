namespace RaidManager.Companion.Client;

/// <summary>Provides a stable assembly marker for scanning and registration.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Allows the companion's composition root to find this project assembly without depending on feature types.
/// </remarks>
public static class AssemblyReference
{
    #region Public Properties
    /// <summary>Gets this project's assembly.</summary>
    public static System.Reflection.Assembly Assembly => typeof(AssemblyReference).Assembly;
    #endregion Public Properties
}
