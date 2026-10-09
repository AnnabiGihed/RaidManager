using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="Toast"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The notification shows its title and message as a status screen readers announce, in the layout's
/// notification area rather than in the page; it closes by itself after six seconds or at once with its close button,
/// and a new message shows it again for a full six seconds (#577).
/// </remarks>
public sealed class ToastTests : BunitContext
{
    #region Fields
    /// <summary>Stores the clock the notification waits on.</summary>
    private readonly FakeTimeProvider _time = new();

    /// <summary>Stores the number of times the page was told the notification closed.</summary>
    private int _closed;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ToastTests"/> class.</summary>
    public ToastTests() => Services.AddSingleton<TimeProvider>(_time);
    #endregion Constructors

    #region Tests
    /// <summary>Shows the title and the message as a status, in the notification area and not where the page puts it.</summary>
    [Fact]
    public void ToastShowsItsTitleAndMessageInTheNotificationArea()
    {
        var area = Render<NotificationArea>();

        var toast = RenderToast();

        toast.FindAll(".toast").ShouldBeEmpty();
        area.Find(".toast").GetAttribute("role").ShouldBe("status");
        area.Find(".toast-title").TextContent.ShouldBe("Officer roles saved");
        area.Find(".toast-message").TextContent.ShouldBe("Members get them at their next check.");
    }

    /// <summary>Gives each tone its own class, teal by default.</summary>
    /// <param name="tone">The tone, or <see langword="null"/> for the default.</param>
    /// <param name="expectedClass">The class it adds.</param>
    [Theory]
    [InlineData(null, "toast-success")]
    [InlineData(ToastTone.Info, "toast-info")]
    [InlineData(ToastTone.Warning, "toast-warning")]
    [InlineData(ToastTone.Danger, "toast-danger")]
    public void EachToneHasItsClass(ToastTone? tone, string expectedClass)
    {
        var area = Render<NotificationArea>();

        Render<Toast>(parameters =>
        {
            parameters.Add(component => component.Title, "Saved").Add(component => component.Message, "Done.");
            if (tone is { } value)
            {
                parameters.Add(component => component.Tone, value);
            }
        });

        area.Find(".toast").ClassList.ShouldContain(expectedClass);
    }

    /// <summary>Closes by itself after six seconds, and tells the page once.</summary>
    [Fact]
    public void ClosesByItselfAfterSixSeconds()
    {
        var area = Render<NotificationArea>();
        RenderToast();

        _time.Advance(Toast.Lifetime - TimeSpan.FromMilliseconds(1));
        area.FindAll(".toast").Count.ShouldBe(1);
        _time.Advance(TimeSpan.FromMilliseconds(1));

        area.WaitForAssertion(() => area.FindAll(".toast").ShouldBeEmpty());
        _closed.ShouldBe(1);
    }

    /// <summary>The close button is a named button that closes the notification at once and stops its timer.</summary>
    [Fact]
    public void CloseButtonClosesAtOnce()
    {
        var area = Render<NotificationArea>();
        RenderToast();
        var close = area.Find(".toast-close");
        close.TagName.ShouldBe("BUTTON");
        close.GetAttribute("type").ShouldBe("button");
        close.GetAttribute("aria-label").ShouldBe("Close Officer roles saved");

        close.Click();

        area.FindAll(".toast").ShouldBeEmpty();
        _time.Advance(Toast.Lifetime);
        _closed.ShouldBe(1);
    }

    /// <summary>A link under the message leads to its page and closes the notification; none by default (#595).</summary>
    [Fact]
    public void ActionLinkLeadsOnAndCloses()
    {
        var area = Render<NotificationArea>();
        RenderToast();
        area.FindAll(".toast-action").ShouldBeEmpty();

        Render<Toast>(parameters => parameters
            .Add(component => component.Title, "2 new characters to review")
            .Add(component => component.Message, "Your companion found Uthertank and Valeerarog.")
            .Add(component => component.ActionText, "Review them")
            .Add(component => component.ActionHref, "/characters/review")
            .Add(component => component.Closed, EventCallback.Factory.Create(this, () => _closed++)));
        var link = area.Find(".toast-action");
        link.GetAttribute("href").ShouldBe("/characters/review");
        link.Click();

        area.FindAll(".toast").ShouldBeEmpty();
        _closed.ShouldBe(1);
    }

    /// <summary>A lasting notification doesn't close by itself, only with its button (#595).</summary>
    [Fact]
    public void LastingNotificationStaysUntilClosed()
    {
        var area = Render<NotificationArea>();
        Render<Toast>(parameters => parameters
            .Add(component => component.Title, "2 new characters to review")
            .Add(component => component.Message, "Your companion found Uthertank and Valeerarog.")
            .Add(component => component.Lasting, true)
            .Add(component => component.Closed, EventCallback.Factory.Create(this, () => _closed++)));

        _time.Advance(Toast.Lifetime * 10);

        area.FindAll(".toast").Count.ShouldBe(1);
        area.Find(".toast-close").Click();
        area.FindAll(".toast").ShouldBeEmpty();
        _closed.ShouldBe(1);
    }

    /// <summary>A new message shows the notification again for a full six seconds.</summary>
    [Fact]
    public void NewMessageRestartsTheNotification()
    {
        var area = Render<NotificationArea>();
        var toast = RenderToast();
        _time.Advance(TimeSpan.FromSeconds(4));

        toast.Render(parameters => parameters.Add(component => component.Title, "Roles saved"));
        _time.Advance(TimeSpan.FromSeconds(4));

        area.Find(".toast-title").TextContent.ShouldBe("Roles saved");
        _closed.ShouldBe(0);
        _time.Advance(TimeSpan.FromSeconds(2));
        area.WaitForAssertion(() => area.FindAll(".toast").ShouldBeEmpty());
    }

    /// <summary>A closed notification stays closed when the page shows again with the same message.</summary>
    [Fact]
    public void ClosedNotificationStaysClosed()
    {
        var area = Render<NotificationArea>();
        var toast = RenderToast();
        area.Find(".toast-close").Click();

        toast.Render(parameters => parameters.Add(component => component.Title, "Officer roles saved"));

        area.FindAll(".toast").ShouldBeEmpty();
    }

    /// <summary>A notification that goes with its page no longer waits to close.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task LeavingThePageStopsTheTimer()
    {
        RenderToast();

        await DisposeComponentsAsync();
        _time.Advance(Toast.Lifetime);

        _closed.ShouldBe(0);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Renders the notification of the boards, counting the times it tells the page it closed.</summary>
    /// <returns>The rendered notification.</returns>
    private IRenderedComponent<Toast> RenderToast() => Render<Toast>(parameters => parameters
        .Add(component => component.Title, "Officer roles saved")
        .Add(component => component.Message, "Members get them at their next check.")
        .Add(component => component.Closed, EventCallback.Factory.Create(this, () => _closed++)));
    #endregion Private Helpers
}
