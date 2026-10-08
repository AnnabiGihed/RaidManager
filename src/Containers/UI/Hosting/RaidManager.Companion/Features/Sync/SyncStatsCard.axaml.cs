using Avalonia.Controls;

namespace RaidManager.Companion.Features.Sync;

/// <summary>The stats card of the sync boards.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Shows last success, queued uploads and watched accounts on boards 2, 3 and 4 of <c>companion-sync</c>
/// (#551, story #17's third criterion).
/// </remarks>
public sealed partial class SyncStatsCard : UserControl
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SyncStatsCard"/> class.</summary>
    public SyncStatsCard() => InitializeComponent();
    #endregion Constructors
}
