using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="ConfirmDialog"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The dialog asks, confirms, cancels by button or Escape, and names itself for screen readers.
/// </remarks>
public sealed class ConfirmDialogTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ConfirmDialogTests"/> class.</summary>
    public ConfirmDialogTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the question and confirms or cancels through its buttons.</summary>
    [Fact]
    public void ButtonsConfirmOrCancel()
    {
        var confirmed = 0;
        var cancelled = 0;
        var dialog = Render<ConfirmDialog>(parameters => parameters
            .Add(component => component.Title, "Reject Jainaice?")
            .Add(component => component.Message, "Jainaice won't become one of your characters.")
            .Add(component => component.Detail, "Reject it only if it isn't yours.")
            .Add(component => component.ConfirmText, "Reject")
            .Add(component => component.ConfirmAppearance, ActionButtonAppearance.Danger)
            .Add(component => component.OnConfirm, () => confirmed++)
            .Add(component => component.OnCancel, () => cancelled++));

        var box = dialog.Find("[role=alertdialog]");
        box.GetAttribute("aria-modal").ShouldBe("true");
        dialog.Find($"#{box.GetAttribute("aria-labelledby")}").TextContent.ShouldBe("Reject Jainaice?");
        dialog.FindAll($"#{box.GetAttribute("aria-describedby")} p").Select(paragraph => paragraph.TextContent)
            .ShouldBe(["Jainaice won't become one of your characters.", "Reject it only if it isn't yours."]);
        dialog.FindAll("button").First(button => button.TextContent.Trim() == "Reject").Click();
        dialog.FindAll("button").First(button => button.TextContent.Trim() == "Cancel").Click();

        confirmed.ShouldBe(1);
        cancelled.ShouldBe(1);
    }

    /// <summary>Cancels on Escape and ignores other keys.</summary>
    [Fact]
    public void EscapeCancels()
    {
        var cancelled = 0;
        var dialog = Render<ConfirmDialog>(parameters => parameters
            .Add(component => component.Title, "Leave?")
            .Add(component => component.Message, "Nothing is saved.")
            .Add(component => component.OnCancel, () => cancelled++));

        dialog.Find("[role=alertdialog]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        dialog.Find("[role=alertdialog]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        cancelled.ShouldBe(1);
        dialog.FindAll(".confirm-dialog-message p").Count.ShouldBe(1);
    }
    #endregion Tests
}
