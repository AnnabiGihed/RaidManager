using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Components;

namespace RaidManager.Web.Tests.Features.Shared.Components;

/// <summary>Verifies the <see cref="DataTable{TItem}"/> component.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Headings, one keyed row per item with the page's cells, and the row height.
/// </remarks>
public sealed class DataTableTests : BunitContext
{
    #region Tests
    /// <summary>Renders the headings and a row of cells per item.</summary>
    [Fact]
    public void ItemsBecomeRowsUnderTheHeadings()
    {
        RenderFragment<string> cells = item => builder =>
        {
            builder.OpenElement(0, "td");
            builder.AddContent(1, item);
            builder.CloseElement();
        };
        var table = Render<DataTable<string>>(parameters => parameters
            .Add(component => component.Items, ["Anguish", "Malarya"])
            .Add(component => component.Headings, ["Member"])
            .Add(component => component.Key, item => item)
            .Add(component => component.RowTemplate, cells)
            .Add(component => component.RowHeight, 72)
            .AddUnmatched("data-testid", "people"));

        table.Find("table").GetAttribute("data-testid").ShouldBe("people");
        table.Find("th").GetAttribute("scope").ShouldBe("col");
        table.Find("th").TextContent.ShouldBe("Member");
        var rows = table.FindAll("tbody tr");
        rows.Select(row => row.TextContent).ShouldBe(["Anguish", "Malarya"]);
        rows[0].GetAttribute("style").ShouldBe("height: 72px");
    }

    /// <summary>Sizes the columns given, and leaves out the column group otherwise.</summary>
    [Fact]
    public void ColumnWidthsSizeTheColumns()
    {
        RenderFragment<string> cells = item => builder => builder.AddContent(0, item);
        var sized = Render<DataTable<string>>(parameters => parameters
            .Add(component => component.Items, ["Anguish"])
            .Add(component => component.Headings, ["Member", "Role"])
            .Add(component => component.Key, item => item)
            .Add(component => component.RowTemplate, cells)
            .Add(component => component.ColumnWidths, [240, 0]));
        var plain = Render<DataTable<string>>(parameters => parameters
            .Add(component => component.Items, ["Anguish"])
            .Add(component => component.Headings, ["Member"])
            .Add(component => component.Key, item => item)
            .Add(component => component.RowTemplate, cells));

        var columns = sized.FindAll("col");
        columns[0].GetAttribute("style").ShouldBe("width: 240px");
        columns[1].HasAttribute("style").ShouldBeFalse();
        plain.FindAll("colgroup").ShouldBeEmpty();
    }
    #endregion Tests
}
