using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Shows items as a table in a card: uppercase column headings, then one row per item.</summary>
/// <typeparam name="TItem">The item's type.</typeparam>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The table look of the design system (ADR-0019), shared by the members and character review pages. The
/// page gives each row's cells; the table gives the card, the headings, the dividers and the row height.
/// </remarks>
public sealed partial class DataTable<TItem>
{
    #region Properties
    /// <summary>Gets or sets the items, one row each.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Gets or sets the column headings, shown in uppercase.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<string> Headings { get; set; } = [];

    /// <summary>Gets or sets the row's identity, so rows keep their state when the list changes.</summary>
    [Parameter]
    [EditorRequired]
    public Func<TItem, object> Key { get; set; } = item => item!;

    /// <summary>Gets or sets a row's cells, as <c>td</c> elements.</summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<TItem> RowTemplate { get; set; } = _ => _ => { };

    /// <summary>Gets or sets optional column widths in pixels, in column order; 0 lets a column take the rest.</summary>
    [Parameter]
    public IReadOnlyList<int>? ColumnWidths { get; set; }

    /// <summary>Gets or sets the least height of a row in pixels; 64 by default.</summary>
    [Parameter]
    public int RowHeight { get; set; } = 64;

    /// <summary>Gets or sets attributes passed through to the table, such as a test id.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Gets the row's height style.</summary>
    private string RowStyle => $"height: {RowHeight.ToString(CultureInfo.InvariantCulture)}px";
    #endregion Properties

    #region Private Helpers
    /// <summary>Gets a column's width style.</summary>
    /// <param name="width">The width in pixels, or 0 for the rest of the table.</param>
    /// <returns>The style, or <see langword="null"/> for a column that takes the rest.</returns>
    private static string? ColumnStyle(int width) => width > 0 ? $"width: {width.ToString(CultureInfo.InvariantCulture)}px" : null;
    #endregion Private Helpers
}
