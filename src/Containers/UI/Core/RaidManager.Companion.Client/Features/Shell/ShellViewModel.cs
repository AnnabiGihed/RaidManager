using System.ComponentModel;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Sync;

namespace RaidManager.Companion.Client.Features.Shell;

/// <summary>Chooses what the companion's window shows, pairing or sync, and the window's size.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The window follows each board: 480 x 600 for the pairing states, 560 x 680 for the sync screens, title bar
/// included (owner decision on #551). A computer already paired opens on the sync; board 6 shows only right after a
/// new pairing, until the player chooses "Choose folders"; any other pairing state shows the pairing.
/// </remarks>
public sealed class ShellViewModel : ViewModelBase
{
    #region Constants
    /// <summary>Defines the height of the window's title bar, which the boards include and Windows draws.</summary>
    private const double TitleBarHeight = 44;
    #endregion Constants

    #region Fields
    /// <summary>Stores the pairing.</summary>
    private readonly PairingViewModel _pairing;

    /// <summary>Stores the sync.</summary>
    private readonly SyncViewModel _sync;

    /// <summary>Stores the pairing state seen last, to tell a new pairing from a stored one.</summary>
    private PairingState _lastState;

    /// <summary>Stores what the window shows.</summary>
    private ViewModelBase _current;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ShellViewModel"/> class.</summary>
    /// <param name="pairing">The pairing.</param>
    /// <param name="sync">The sync.</param>
    public ShellViewModel(PairingViewModel pairing, SyncViewModel sync)
    {
        ArgumentNullException.ThrowIfNull(pairing);
        _pairing = pairing;
        _sync = sync;
        _lastState = pairing.State;
        _current = pairing;
        pairing.PropertyChanged += OnPairingChanged;
        pairing.ChooseFoldersRequested += (_, _) =>
        {
            _sync.ShowFolders();
            Current = _sync;
        };
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets what the window shows: the pairing or the sync.</summary>
    public ViewModelBase Current
    {
        get => _current;
        private set
        {
            if (SetProperty(ref _current, value))
            {
                OnPropertyChanged(nameof(WindowWidth));
                OnPropertyChanged(nameof(WindowHeight));
            }
        }
    }

    /// <summary>Gets the window's width: 480 for the pairing, 560 for the sync.</summary>
    public double WindowWidth => IsShowingSync ? 560 : 480;

    /// <summary>Gets the window's height under its title bar: the board's 600 or 680 less the title bar.</summary>
    public double WindowHeight => (IsShowingSync ? 680 : 600) - TitleBarHeight;
    #endregion Public Properties

    #region Private Properties
    /// <summary>Gets a value indicating whether the window shows the sync.</summary>
    private bool IsShowingSync => ReferenceEquals(_current, _sync);
    #endregion Private Properties

    #region Private Helpers
    /// <summary>Follows the pairing's state: the sync once paired, unless the pairing was just confirmed (board 6).</summary>
    /// <param name="sender">The pairing.</param>
    /// <param name="e">The event data.</param>
    private void OnPairingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PairingViewModel.State))
        {
            return;
        }

        var state = _pairing.State;
        if (state == PairingState.Paired && _lastState != PairingState.Waiting)
        {
            _sync.Open();
            Current = _sync;
        }
        else
        {
            Current = _pairing;
        }

        _lastState = state;
    }
    #endregion Private Helpers
}
