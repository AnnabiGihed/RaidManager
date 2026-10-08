using Avalonia;
using Avalonia.Controls.Primitives;

namespace RaidManager.Companion.Features.Shared;

/// <summary>A 24 px pill with one word, in the success, neutral, danger or warning tone.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The companion's version of the mockups' shared <c>badge</c> component, such as the sync boards' "Syncing"
/// and "Needs attention" (#551); its look comes from its control theme in <c>Theme/Controls.axaml</c>, and the classes
/// <c>success</c>, <c>danger</c> and <c>warning</c> pick the tone (neutral by default). Status is never color alone.
/// </remarks>
public sealed class Badge : TemplatedControl
{
    #region Fields
    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<Badge, string?>(nameof(Text));
    #endregion Fields

    #region Public Properties
    /// <summary>Gets or sets the word.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    #endregion Public Properties
}
