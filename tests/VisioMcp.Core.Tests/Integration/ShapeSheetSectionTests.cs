using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Cell;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Models;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for generic ShapeSheet section and row access (#33).
///
/// Per Rule 30 these run against a real Visio instance. The section indices and the behaviour of
/// <c>AddRow</c>, <c>AddNamedRow</c>, <c>CellsSRC</c> and <c>DeleteRow</c> are only knowable by
/// executing them: the widely-repeated mapping of Actions to 238 and Character to 4 is wrong —
/// they are 240 and 3 — which probing a live instance found and no mocked test could have.
///
/// Every write is verified through a reader rather than by return code, so each test proves the
/// value reached Visio rather than that the call did not throw.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Cell")]
public sealed class ShapeSheetSectionTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly CellCommands _cells = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void ListSections_ReportsSectionsPresentOnANewShape()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var result = _cells.ListSections(batch, 1, shapeName);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotEmpty(result.Sections);

        // A drawn rectangle always has geometry; Prop is created on demand.
        Assert.Contains(result.Sections, s => s.SectionName == "Geometry1");
        Assert.DoesNotContain(result.Sections, s => s.SectionName == "Prop");
    }

    [Fact]
    public void AddRow_CreatesTheSectionAndANamedAddressableCell()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var added = _cells.AddRow(batch, 1, shapeName, "Prop", "Cost");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.NotNull(added.Row);
        Assert.Equal("Cost", added.Row!.RowName);
        Assert.Equal("Prop.Cost", added.Row.CellName);

        // The new row is reachable by name through the pre-existing cell surface.
        var set = _cells.SetFormula(batch, 1, shapeName, "Prop.Cost", "\"42\"");
        Assert.True(set.Success, set.ErrorMessage);

        var read = _cells.ReadFormula(batch, 1, shapeName, "Prop.Cost");
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("\"42\"", read.Cell!.Formula);
    }

    [Fact]
    public void AddRow_WithoutAName_CreatesAPositionalRow()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var added = _cells.AddRow(batch, 1, shapeName, "Connections");

        Assert.True(added.Success, added.ErrorMessage);
        Assert.NotNull(added.Row);
        Assert.Equal(0, added.Row!.RowIndex);

        // Connection point rows are positional: no row name, but a usable cell name.
        Assert.Equal(string.Empty, added.Row.RowName);
        Assert.Equal("Connections.X1", added.Row.CellName);
    }

    [Fact]
    public void WriteSrc_AcceptsAFormula_AndReadSrcReturnsIt()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _cells.AddRow(batch, 1, shapeName, "Connections");

        // A connection point pinned by formula stays centred when the shape is resized. That is why
        // these cells take expressions rather than values, and why write-src exists at all.
        var write = _cells.WriteSrc(batch, 1, shapeName, "Connections", 0, 0, "Width*0.5");
        Assert.True(write.Success, write.ErrorMessage);

        var read = _cells.ReadSrc(batch, 1, shapeName, "Connections", 0, 0);

        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("Width*0.5", read.Cell!.Formula);
        Assert.Equal("Connections.X1", read.Cell.CellName);
    }

    [Fact]
    public void ListRows_ReportsEveryRowWithItsName()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _cells.AddRow(batch, 1, shapeName, "Prop", "Cost");
        _cells.AddRow(batch, 1, shapeName, "Prop", "Owner");

        var rows = _cells.ListRows(batch, 1, shapeName, "Prop");

        Assert.True(rows.Success, rows.ErrorMessage);
        Assert.Equal("Prop", rows.SectionName);
        Assert.Equal(243, rows.SectionIndex);
        Assert.Equal(2, rows.Rows.Count);
        Assert.Contains(rows.Rows, r => r.RowName == "Cost" && r.CellName == "Prop.Cost");
        Assert.Contains(rows.Rows, r => r.RowName == "Owner" && r.CellName == "Prop.Owner");
    }

    [Fact]
    public void DeleteRow_RemovesTheRow()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _cells.AddRow(batch, 1, shapeName, "User", "Origin");
        Assert.Single(_cells.ListRows(batch, 1, shapeName, "User").Rows);

        var deleted = _cells.DeleteRow(batch, 1, shapeName, "User", 0);

        Assert.True(deleted.Success, deleted.ErrorMessage);
        Assert.Empty(_cells.ListRows(batch, 1, shapeName, "User").Rows);
    }

    [Fact]
    public void Section_IsAcceptedByNameOrByIndex()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _cells.AddRow(batch, 1, shapeName, "Prop", "ByName");
        _cells.AddRow(batch, 1, shapeName, "243", "ByIndex");

        var rows = _cells.ListRows(batch, 1, shapeName, "Prop");

        Assert.Equal(2, rows.Rows.Count);
        Assert.Contains(rows.Rows, r => r.RowName == "ByName");
        Assert.Contains(rows.Rows, r => r.RowName == "ByIndex");
    }

    [Fact]
    public void UnknownSectionName_IsRejectedByNamingTheValidOnes()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var ex = Assert.Throws<ArgumentException>(() =>
            _cells.ListRows(batch, 1, shapeName, "Slides"));

        // Naming the alternatives matters: "unknown section" alone leaves an agent guessing, which
        // is exactly the failure mode #55 describes on the MCP error path.
        Assert.Contains("Prop", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Connections", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteRow_BeyondTheEnd_ReportsTheActualRowCount()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        _cells.AddRow(batch, 1, shapeName, "Prop", "Only");

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _cells.DeleteRow(batch, 1, shapeName, "Prop", 5));

        Assert.Contains("1 row(s)", ex.Message, StringComparison.Ordinal);
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"ShapeSheetSectionTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    /// <summary>Draws a rectangle on page 1 and returns its generated name.</summary>
    private string AddRectangle(IVisioBatch batch)
    {
        var added = _shapes.AddShape(batch, 1, 1, 1.0f, 1.0f, 2.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        ShapeListResult listed = _shapes.List(batch, 1);
        Assert.True(listed.Success, listed.ErrorMessage);
        Assert.NotEmpty(listed.Shapes);

        return listed.Shapes[^1].Name;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // The file may still be briefly held after the batch disposes.
            }
        }
    }
}
