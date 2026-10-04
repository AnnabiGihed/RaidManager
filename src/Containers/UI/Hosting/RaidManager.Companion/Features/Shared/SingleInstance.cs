using System.Runtime.Versioning;

namespace RaidManager.Companion.Features.Shared;

/// <summary>Keeps one companion per Windows user, and lets a second start bring the first one forward.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The single instance of ADR-0032: a named mutex in the user's session decides who runs, and a named event
/// lets a refused start ask the running companion to show its window. Named events exist only on Windows.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class SingleInstance : IActivationSignal, IDisposable
{
    #region Fields
    /// <summary>Stores the mutex held by the running companion.</summary>
    private readonly Mutex _mutex;

    /// <summary>Stores the event a second start sets.</summary>
    private readonly EventWaitHandle _activation;

    /// <summary>Stores the wait registered by <see cref="Listen"/>.</summary>
    private RegisteredWaitHandle? _registration;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SingleInstance"/> class.</summary>
    /// <param name="mutex">The mutex.</param>
    /// <param name="activation">The activation event.</param>
    /// <param name="isFirst">Whether this start owns the mutex.</param>
    private SingleInstance(Mutex mutex, EventWaitHandle activation, bool isFirst)
    {
        _mutex = mutex;
        _activation = activation;
        IsFirst = isFirst;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets a value indicating whether this start is the companion that runs.</summary>
    public bool IsFirst { get; }
    #endregion Public Properties

    #region Factory Methods
    /// <summary>Claims the companion's name for this Windows session.</summary>
    /// <param name="name">The name, unique to the companion.</param>
    /// <returns>The claim; <see cref="IsFirst"/> says whether it succeeded.</returns>
    public static SingleInstance Acquire(string name)
    {
        var mutex = new Mutex(initiallyOwned: true, $@"Local\{name}", out var createdNew);
        var activation = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{name}.Activate");
        return new SingleInstance(mutex, activation, createdNew);
    }
    #endregion Factory Methods

    #region Public Methods
    /// <summary>Asks the running companion to show its window.</summary>
    public void ActivateFirst() => _activation.Set();

    /// <inheritdoc />
    public void Listen(Action onActivated) => _registration = ThreadPool.RegisterWaitForSingleObject(
        _activation, (_, _) => onActivated(), null, Timeout.Infinite, executeOnlyOnce: false);

    /// <summary>Releases the claim.</summary>
    public void Dispose()
    {
        _registration?.Unregister(null);
        if (IsFirst)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _activation.Dispose();
    }
    #endregion Public Methods
}
