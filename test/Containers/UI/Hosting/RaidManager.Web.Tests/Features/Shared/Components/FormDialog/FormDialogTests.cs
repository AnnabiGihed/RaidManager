using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="FormDialog"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The dialog names itself for screen readers, shows its fields and actions, and cancels on Escape.
/// </remarks>
public sealed class FormDialogTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="FormDialogTests"/> class.</summary>
    public FormDialogTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows the title, fields and actions, and cancels on Escape only.</summary>
    [Fact]
    public void DialogShowsItsPartsAndCancelsOnEscape()
    {
        var cancelled = 0;
        var dialog = Render<FormDialog>(parameters => parameters
            .Add(component => component.Title, "Create a role")
            .Add(component => component.ChildContent, "fields")
            .Add(component => component.Actions, "buttons")
            .Add(component => component.OnCancel, () => cancelled++)
            .AddUnmatched("data-testid", "role-form"));

        var box = dialog.Find("[role=dialog]");
        (box.GetAttribute("aria-modal"), box.GetAttribute("data-testid")).ShouldBe(("true", "role-form"));
        dialog.Find($"#{box.GetAttribute("aria-labelledby")}").TextContent.ShouldBe("Create a role");
        dialog.Find(".form-dialog-body").TextContent.ShouldBe("fields");
        dialog.Find(".form-dialog-actions").TextContent.ShouldBe("buttons");
        box.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        box.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelled.ShouldBe(1);
    }

    /// <summary>Leaves out the actions row when none are given.</summary>
    [Fact]
    public void ActionsAreOptional() =>
        Render<FormDialog>(parameters => parameters.Add(component => component.Title, "Edit")).FindAll(".form-dialog-actions").ShouldBeEmpty();
    #endregion Tests
}
