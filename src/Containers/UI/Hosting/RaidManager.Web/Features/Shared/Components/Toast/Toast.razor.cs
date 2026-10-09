using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows a short confirmation at the top right of the window: a title, one line of text and a close button.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The 380 by 72 px notification of ADR-0019, such as "Officer roles saved", placed above the page so it
/// doesn't cover the content under it. It renders into the layout's <see cref="NotificationArea"/>, 16 px under the
/// top bar, and closes by itself after <see cref="Lifetime"/> or at once with its × (#577, mockup confirmed on #578).
/// A new title, message or tone shows it again for a full <see cref="Lifetime"/>. An optional link under the message
/// leads to another page and closes the notification (<c>character-sync</c> board 1, #595).
/// </remarks>
public sealed partial class Toast : IDisposable
{
    #region Fields
    /// <summary>Gets how long a notification stays before it closes by itself.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

    /// <summary>Stores the content shown, so a new one restarts the notification.</summary>
    private (string Title, string Message, ToastTone Tone)? _shown;

    /// <summary>Stores the token that stops the wait before closing.</summary>
    private CancellationTokenSource? _wait;

    /// <summary>Stores whether the notification is closed.</summary>
    private bool _closed;
    #endregion Fields

    #region Properties
    /// <summary>Gets or sets the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the line under the title.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the accent; <see cref="ToastTone.Success"/> by default.</summary>
    [Parameter]
    public ToastTone Tone { get; set; } = ToastTone.Success;

    /// <summary>Gets or sets the text of a link under the message, such as "Review them"; none by default.</summary>
    [Parameter]
    public string? ActionText { get; set; }

    /// <summary>Gets or sets where the link leads; following it closes the notification.</summary>
    [Parameter]
    public string? ActionHref { get; set; }

    /// <summary>Gets or sets what the page does once the notification closes, such as forgetting it.</summary>
    [Parameter]
    public EventCallback Closed { get; set; }

    /// <summary>Gets or sets attributes passed through to the notification, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets or sets the clock the notification waits on.</summary>
    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    /// <summary>Gets the classes for the tone.</summary>
    private string CssClass => $"toast toast-{Tone.ToString().ToLowerInvariant()}";

    /// <summary>Gets the close button's name for screen readers, such as "Close Officer roles saved".</summary>
    private string CloseLabel => $"Close {Title}";
    #endregion Properties

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => StopWaiting();
    #endregion Public Methods

    #region Overrides
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var content = (Title, Message, Tone);
        if (_shown == content)
        {
            return;
        }

        _shown = content;
        _closed = false;
        StopWaiting();
        _wait = new CancellationTokenSource();
        _ = CloseLaterAsync(_wait.Token);
    }
    #endregion Overrides

    #region Private Helpers
    /// <summary>Closes the notification once its lifetime has passed, unless it closed or changed meanwhile.</summary>
    /// <param name="cancellationToken">The token that stops the wait.</param>
    /// <returns>A task that completes when the notification closed or the wait stopped.</returns>
    private async Task CloseLaterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Lifetime, TimeProvider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // A click on the close button may come first, while this waits for the page's thread: it stops the wait.
        await InvokeAsync(() => cancellationToken.IsCancellationRequested ? Task.CompletedTask : CloseAsync());
    }

    /// <summary>Closes the notification and tells the page.</summary>
    /// <returns>A task that completes when the page has been told.</returns>
    private async Task CloseAsync()
    {
        _closed = true;
        StopWaiting();
        StateHasChanged();
        await Closed.InvokeAsync();
    }

    /// <summary>Stops the wait before closing, if any.</summary>
    private void StopWaiting()
    {
        _wait?.Cancel();
        _wait?.Dispose();
        _wait = null;
    }
    #endregion Private Helpers
}
