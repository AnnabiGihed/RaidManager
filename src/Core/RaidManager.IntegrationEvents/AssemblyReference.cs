namespace RaidManager.IntegrationEvents;

/// <summary>Provides a stable assembly marker for scanning and registration.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Allows container composition roots to discover this project assembly without depending on feature implementation types.
/// </remarks>
public static class AssemblyReference
{
    #region Public Properties
    /// <summary>Gets this project's assembly.</summary>
    public static System.Reflection.Assembly Assembly => typeof(AssemblyReference).Assembly;
    #endregion Public Properties
}
