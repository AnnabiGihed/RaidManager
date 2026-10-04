using Avalonia.Controls;

namespace RaidManager.Companion.Features.Pairing;

/// <summary>Shows the companion's pairing states.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The view of the pairing view model, built to boards 5 to 8, 19 and 20 of the companion pairing mockup;
/// every decision stays in the view model.
/// </remarks>
public sealed partial class PairingView : UserControl
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="PairingView"/> class.</summary>
    public PairingView() => InitializeComponent();
    #endregion Constructors
}
