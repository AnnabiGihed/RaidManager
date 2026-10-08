using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Shows one account row of board 1 and holds the player's unsaved choice for it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: An account of <c>companion-sync</c> board 1: its checkbox, its name and "3 characters" or "Excluded · 4
/// characters" (#551). The choice is saved by "Save and sync".
/// </remarks>
public sealed class WatchedAccountViewModel : ViewModelBase
{
    #region Fields
    /// <summary>Stores whether the player wants the account watched.</summary>
    private bool _isWatched;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="WatchedAccountViewModel"/> class.</summary>
    /// <param name="id">The account's hash.</param>
    /// <param name="name">The account folder's name.</param>
    /// <param name="characterCount">The number of characters WoW keeps for it.</param>
    /// <param name="isWatched">Whether it is watched now.</param>
    public WatchedAccountViewModel(string id, string name, int characterCount, bool isWatched)
    {
        Id = id;
        Name = name;
        CharacterCount = characterCount;
        WasWatched = isWatched;
        _isWatched = isWatched;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the account's hash.</summary>
    public string Id { get; }

    /// <summary>Gets the account folder's name, such as <c>ARTHASACC</c>.</summary>
    public string Name { get; }

    /// <summary>Gets the number of characters WoW keeps for the account.</summary>
    public int CharacterCount { get; }

    /// <summary>Gets a value indicating whether the account is watched in the saved choices.</summary>
    public bool WasWatched { get; }

    /// <summary>Gets or sets a value indicating whether the player wants the account watched.</summary>
    public bool IsWatched
    {
        get => _isWatched;
        set
        {
            if (SetProperty(ref _isWatched, value))
            {
                OnPropertyChanged(nameof(CountText));
            }
        }
    }

    /// <summary>Gets the text at the row's right, such as "3 characters" or "Excluded · 4 characters".</summary>
    public string CountText
    {
        get
        {
            var count = CharacterCount == 1 ? "1 character" : $"{CharacterCount} characters";
            return IsWatched ? count : $"Excluded · {count}";
        }
    }
    #endregion Public Properties
}
