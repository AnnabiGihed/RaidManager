using Avalonia;
using Avalonia.Controls.Primitives;

namespace RaidManager.Companion.Features.Shared;

/// <summary>A message box with an icon, a title and one line of text, in the warning or danger tone.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The companion's version of the mockups' shared <c>notice</c> component (72 px, 10 px radius); its look
/// comes from its control theme in <c>Theme/Controls.axaml</c>, and the classes <c>warning</c> and <c>danger</c> pick
/// the tone.
/// </remarks>
public sealed class Notice : TemplatedControl
{
    #region Fields
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<Notice, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<Notice, string?>(nameof(Message));
    #endregion Fields

    #region Public Properties
    /// <summary>Gets or sets the title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the line of text under the title.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
    #endregion Public Properties
}
