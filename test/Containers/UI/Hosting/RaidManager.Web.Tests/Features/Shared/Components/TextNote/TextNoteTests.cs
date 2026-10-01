using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="TextNote"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: The note aligns its text from its parameter.
/// </remarks>
public sealed class TextNoteTests : BunitContext
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="TextNoteTests"/> class.</summary>
    public TextNoteTests()
    {
        Services.AddRadzenComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
    #endregion Constructors

    #region Tests
    /// <summary>Shows a note aligned as told.</summary>
    /// <param name="alignment">The alignment.</param>
    /// <param name="centered">Whether the note is centered.</param>
    [Theory]
    [InlineData(ContentAlignment.Center, true)]
    [InlineData(ContentAlignment.Start, false)]
    public void TextNoteShowsItsTextAligned(ContentAlignment alignment, bool centered)
    {
        var note = Render<TextNote>(parameters => parameters
            .Add(component => component.Text, "Your first sign-in creates your RaidManager account.")
            .Add(component => component.Alignment, alignment));

        var paragraph = note.Find("p.text-note");
        paragraph.TextContent.ShouldBe("Your first sign-in creates your RaidManager account.");
        paragraph.ClassList.Contains("text-note-center").ShouldBe(centered);
    }

    #endregion Tests
}
