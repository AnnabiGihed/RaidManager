using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="TextField"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The label names the input, and typing reports the text.
/// </remarks>
public sealed class TextFieldTests : BunitContext
{
    #region Tests
    /// <summary>Ties the label to the input and reports what is typed.</summary>
    [Fact]
    public void TypingReportsTheText()
    {
        var typed = string.Empty;
        var field = Render<TextField>(parameters => parameters
            .Add(component => component.Label, "Name")
            .Add(component => component.Value, "Vet")
            .Add(component => component.MaxLength, 50)
            .Add(component => component.ValueChanged, value => typed = value)
            .AddUnmatched("data-testid", "role-name"));

        var input = field.Find("input");
        field.Find("label").GetAttribute("for").ShouldBe(input.Id);
        (input.GetAttribute("value"), input.GetAttribute("maxlength"), input.GetAttribute("data-testid")).ShouldBe(("Vet", "50", "role-name"));
        input.Input("Veteran");

        typed.ShouldBe("Veteran");
    }
    #endregion Tests
}
