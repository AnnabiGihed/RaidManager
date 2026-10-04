using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Raises property change notifications for the companion's view models.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives every view model one way to change a property and notify its bindings, without an MVVM framework
/// (ADR-0032).
/// </remarks>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    #region Events
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    #endregion Events

    #region Protected Methods
    /// <summary>Sets a field and raises <see cref="PropertyChanged"/> when its value really changes.</summary>
    /// <typeparam name="T">The field's type.</typeparam>
    /// <param name="field">The field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property's name, given by the compiler.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Raises <see cref="PropertyChanged"/> for a property.</summary>
    /// <param name="propertyName">The property's name, given by the compiler.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion Protected Methods
}
