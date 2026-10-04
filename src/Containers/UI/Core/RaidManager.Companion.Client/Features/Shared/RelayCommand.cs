using System.Windows.Input;

namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Runs a synchronous action when a bound control invokes it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets views bind controls such as the tray menu entries to view model actions without code-behind.
/// </remarks>
public sealed class RelayCommand : ICommand
{
    #region Fields
    /// <summary>Stores the action to run.</summary>
    private readonly Action _execute;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="RelayCommand"/> class.</summary>
    /// <param name="execute">The action to run.</param>
    public RelayCommand(Action execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
    }
    #endregion Constructors

    #region Events
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;
    #endregion Events

    #region Public Methods
    /// <inheritdoc />
    public bool CanExecute(object? parameter) => true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

    /// <summary>Tells bound controls to ask <see cref="CanExecute"/> again.</summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    #endregion Public Methods
}
