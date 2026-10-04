using System.Windows.Input;

namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Runs an asynchronous action when a bound control invokes it, one run at a time.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Disables itself while running and hands every failure to its owner, so no exception reaches the UI
/// dispatcher and the view model shows the failure as a state instead (avalonia-desktop §4).
/// </remarks>
public sealed class AsyncRelayCommand : ICommand
{
    #region Fields
    /// <summary>Stores the action to run.</summary>
    private readonly Func<Task> _execute;

    /// <summary>Stores what to do with a failure of the action.</summary>
    private readonly Action<Exception> _onFailure;

    /// <summary>Stores whether the action is running.</summary>
    private bool _isRunning;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.</summary>
    /// <param name="execute">The action to run.</param>
    /// <param name="onFailure">What to do with a failure of the action.</param>
    public AsyncRelayCommand(Func<Task> execute, Action<Exception> onFailure)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(onFailure);
        _execute = execute;
        _onFailure = onFailure;
    }
    #endregion Constructors

    #region Events
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;
    #endregion Events

    #region Public Methods
    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning;

    /// <inheritdoc />
    public void Execute(object? parameter) => _ = ExecuteAsync();

    /// <summary>Runs the action unless it is already running, and hands a failure to the owner.</summary>
    /// <returns>A task that completes when the action has finished, successfully or not.</returns>
    public async Task ExecuteAsync()
    {
        if (_isRunning)
        {
            return;
        }

        SetRunning(true);
        try
        {
            await _execute();
        }
        catch (Exception exception)
        {
            // The command is the last place a failure can be turned into a state the player sees; rethrowing would
            // reach the dispatcher and end the companion.
            _onFailure(exception);
        }
        finally
        {
            SetRunning(false);
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Records whether the action is running and tells bound controls.</summary>
    /// <param name="isRunning">Whether the action is running.</param>
    private void SetRunning(bool isRunning)
    {
        _isRunning = isRunning;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion Private Helpers
}
