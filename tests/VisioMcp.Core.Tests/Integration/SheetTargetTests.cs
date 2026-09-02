using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Cell;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Models;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// The cell surface addresses a shape, a page or the document (#36b).
///
/// Visio exposes the same section, row and cell API on a shape, on <c>Page.PageSheet</c> and on
/// <c>Document.DocumentSheet</c> — established while building #33, which is why this needed a
/// parameter rather than a new tool. Page size, drawing scale and the document's theme cells all
/// become reachable through the actions that already existed.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Cell")]
public sealed class SheetTargetTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly CellCommands _cells = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void PageTarget_ReadsThePagesOwnCells()
    {
        using var batch = CreateDocument();

        var width = _cells.Read(batch, 1, null, "PageWidth", "page");

        Assert.True(width.Success, width.ErrorMessage);
        Assert.Equal("PageSheet", width.ShapeName);
        Assert.NotNull(width.Cell!.Value);
        Assert.True(double.Parse(width.Cell.Value!, System.Globalization.CultureInfo.InvariantCulture) > 0);
    }

    [Fact]
    public void PageTarget_WritesPageSize()
    {
        using var batch = CreateDocument();

        var set = _cells.SetFormula(batch, 1, null, "PageWidth", "11 in", "page");
        Assert.True(set.Success, set.ErrorMessage);

        var read = _cells.ReadFormula(batch, 1, null, "PageWidth", "page");
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("11 in", read.Cell!.Formula);
    }

    [Fact]
    public void DocumentTarget_ReadsTheDocumentsOwnCells()
    {
        using var batch = CreateDocument();

        // ThemeIndex lives on the DocumentSheet — Document.Theme does not exist (#22).
        var theme = _cells.ReadFormula(batch, 1, null, "ThemeIndex", "document");

        Assert.True(theme.Success, theme.ErrorMessage);
        Assert.Equal("DocumentSheet", theme.ShapeName);
        Assert.NotNull(theme.Cell!.Formula);
    }

    [Fact]
    public void UserCells_WorkOnAPage()
    {
        using var batch = CreateDocument();

        var added = _cells.AddRow(batch, 1, null, "User", "Origin", "page");
        Assert.True(added.Success, added.ErrorMessage);
        Assert.Equal("User.Origin", added.Row!.CellName);

        _cells.SetFormula(batch, 1, null, "User.Origin", "\"generated\"", "page");
        var read = _cells.ReadFormula(batch, 1, null, "User.Origin", "page");

        Assert.Equal("\"generated\"", read.Cell!.Formula);
    }

    [Fact]
    public void UserCells_WorkOnTheDocument()
    {
        using var batch = CreateDocument();

        var added = _cells.AddRow(batch, 1, null, "User", "ToolVersion", "document");
        Assert.True(added.Success, added.ErrorMessage);

        _cells.SetFormula(batch, 1, null, "User.ToolVersion", "\"1.0\"", "document");
        var read = _cells.ReadFormula(batch, 1, null, "User.ToolVersion", "document");

        Assert.Equal("\"1.0\"", read.Cell!.Formula);
    }

    [Fact]
    public void ListSections_WorksOnAPageSheet()
    {
        using var batch = CreateDocument();

        _cells.AddRow(batch, 1, null, "User", "Marker", "page");
        var sections = _cells.ListSections(batch, 1, null, "page");

        Assert.True(sections.Success, sections.ErrorMessage);
        Assert.Contains(sections.Sections, s => s.SectionName == "User");
    }

    [Fact]
    public void ShapeTarget_RemainsTheDefault()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        // No sheetTarget passed: behaviour is unchanged for every existing caller.
        var read = _cells.Read(batch, 1, shapeName, "Width");

        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(shapeName, read.ShapeName);
    }

    [Fact]
    public void ShapeTarget_WithoutAShapeName_SaysWhatToDoInstead()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<ArgumentException>(() =>
            _cells.Read(batch, 1, null, "Width"));

        // The message must point at the alternative rather than only stating the requirement.
        Assert.Contains("sheet_target", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownSheetTarget_IsRejectedByNamingTheValidOnes()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<ArgumentException>(() =>
            _cells.Read(batch, 1, null, "PageWidth", "slide"));

        Assert.Contains("page", ex.Message, StringComparison.Ordinal);
        Assert.Contains("document", ex.Message, StringComparison.Ordinal);
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"SheetTargetTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    private string AddRectangle(IVisioBatch batch)
    {
        var added = _shapes.AddShape(batch, 1, 1, 1.0f, 1.0f, 2.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        ShapeListResult listed = _shapes.List(batch, 1);
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
