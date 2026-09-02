using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Commands.Style;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Named styles (#36d).
///
/// A Visio style is reusable named formatting held by the document. It carries its own ShapeSheet,
/// so its appearance is set with the same cell names used on shapes — which is why the tool exposes
/// <c>set-formula</c> rather than a fixed list of formatting parameters.
///
/// Integration tests against real Visio (Rule 30).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "Style")]
public sealed class StyleTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly StyleCommands _styles = new();
    private readonly ShapeCommands _shapes = new();

    [Fact]
    public void List_ReturnsTheBuiltInStyles()
    {
        using var batch = CreateDocument();

        var listed = _styles.List(batch);

        Assert.True(listed.Success, listed.ErrorMessage);

        // A blank Visio document is not empty of styles, unlike its Masters collection.
        Assert.Contains(listed.Styles, s => s.Name == "Normal");
        Assert.Contains(listed.Styles, s => s.Name == "No Style");
    }

    [Fact]
    public void Create_AddsAStyle()
    {
        using var batch = CreateDocument();

        var created = _styles.Create(batch, "Callout", "Normal");

        Assert.True(created.Success, created.ErrorMessage);
        Assert.Equal("Callout", created.Style!.Name);
        Assert.True(created.Style.IncludesFill);
        Assert.True(created.Style.IncludesLine);
        Assert.True(created.Style.IncludesText);
        Assert.Contains(_styles.List(batch).Styles, s => s.Name == "Callout");
    }

    [Fact]
    public void Create_CanCarryOnlySomeAspects()
    {
        using var batch = CreateDocument();

        var created = _styles.Create(batch, "LineOnly", includesFill: false, includesLine: true, includesText: false);

        // Styles.Add takes its flags in the order TEXT, LINE, FILL — the reverse of the intuitive
        // one. Passing them the obvious way yields the wrong three flags, and Visio raises nothing:
        // the resulting style then silently refuses both writes and application.
        Assert.False(created.Style!.IncludesFill);
        Assert.True(created.Style.IncludesLine);
        Assert.False(created.Style.IncludesText);
    }

    [Fact]
    public void SetFormula_ForAnAspectTheStyleDoesNotCarry_FailsLoudly()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "LineOnly", includesFill: false, includesLine: true, includesText: false);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _styles.SetFormula(batch, "LineOnly", "FillForegnd", "RGB(10,120,10)"));

        // Visio ignores the write and leaves the cell alone without raising anything, so without
        // this check the caller is told a colour was set that was not.
        Assert.Contains("had no effect", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_ForAnAspectTheStyleDoesNotCarry_FailsLoudly()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "LineOnly", includesFill: false, includesLine: true, includesText: false);
        var shape = AddRect(batch);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _styles.Apply(batch, 1, shape, "LineOnly", "fill"));

        // Visio leaves FillStyle at 'Normal' and raises nothing.
        Assert.Contains("had no effect", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithNoAspects_IsRejected()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<ArgumentException>(() =>
            _styles.Create(batch, "Useless", includesFill: false, includesLine: false, includesText: false));

        // Visio accepts this and produces a style that can never change a shape.
        Assert.Contains("at least one", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WithADuplicateName_NamesTheStyle()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout");

        var ex = Assert.Throws<ArgumentException>(() => _styles.Create(batch, "Callout"));

        // Visio's own message is "The name '%s' is already in use." — the placeholder is never
        // expanded, so it does not say which name.
        Assert.Contains("Callout", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("%s", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetFormula_WritesToTheStylesOwnShapeSheet()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout", "Normal");

        var set = _styles.SetFormula(batch, "Callout", "LineWeight", "3 pt");

        Assert.True(set.Success, set.ErrorMessage);
        Assert.Equal("3 pt", set.Formula);
        Assert.Equal("3 pt", _styles.ReadFormula(batch, "Callout", "LineWeight").Formula);
    }

    [Fact]
    public void Apply_StylesTheShape()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout", "Normal");
        _styles.SetFormula(batch, "Callout", "FillForegnd", "RGB(200,30,30)");
        var shape = AddRect(batch);

        var applied = _styles.Apply(batch, 1, shape, "Callout");

        Assert.True(applied.Success, applied.ErrorMessage);

        // The point of a style: the shape's own fill cell now resolves through it.
        Assert.Equal("RGB(200,30,30)", ReadShapeCell(batch, shape, "FillForegnd"));
    }

    [Fact]
    public void ChangingAStyle_RestylesEveryShapeUsingIt()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout", "Normal");
        _styles.SetFormula(batch, "Callout", "FillForegnd", "RGB(200,30,30)");
        var first = AddRect(batch);
        var second = AddRect(batch, 4.0f);
        _styles.Apply(batch, 1, first, "Callout");
        _styles.Apply(batch, 1, second, "Callout");

        _styles.SetFormula(batch, "Callout", "FillForegnd", "RGB(30,90,200)");

        // This is the whole reason to use a style rather than formatting each shape.
        Assert.Equal("RGB(30,90,200)", ReadShapeCell(batch, first, "FillForegnd"));
        Assert.Equal("RGB(30,90,200)", ReadShapeCell(batch, second, "FillForegnd"));
    }

    [Fact]
    public void Apply_CanTargetOneAspectOnly()
    {
        using var batch = CreateDocument();

        // Each style must carry the aspect it is applied for, or Visio refuses silently.
        _styles.Create(batch, "Lines", includesFill: false, includesLine: true, includesText: false);
        _styles.Create(batch, "Fills", includesFill: true, includesLine: false, includesText: false);
        _styles.SetFormula(batch, "Lines", "LineWeight", "4 pt");
        _styles.SetFormula(batch, "Fills", "FillForegnd", "RGB(10,120,10)");
        var shape = AddRect(batch);

        _styles.Apply(batch, 1, shape, "Lines", "line");
        _styles.Apply(batch, 1, shape, "Fills", "fill");

        // A shape combines separate line and fill styles, which 'all' would not allow.
        Assert.Equal("4 pt", ReadShapeCell(batch, shape, "LineWeight"));
        Assert.Equal("RGB(10,120,10)", ReadShapeCell(batch, shape, "FillForegnd"));
    }

    [Fact]
    public void Apply_WithAnUnknownAspect_NamesTheValidOnes()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout");
        var shape = AddRect(batch);

        var ex = Assert.Throws<ArgumentException>(() => _styles.Apply(batch, 1, shape, "Callout", "shadow"));

        Assert.Contains("fill", ex.Message, StringComparison.Ordinal);
        Assert.Contains("line", ex.Message, StringComparison.Ordinal);
        Assert.Contains("text", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rename_ChangesTheNameUsedForLookup()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout");

        var renamed = _styles.Rename(batch, "Callout", "Highlight");

        Assert.Equal("Highlight", renamed.Style!.Name);
        Assert.True(_styles.Read(batch, "Highlight").Success);
    }

    [Fact]
    public void Delete_ReportsHowManyShapesLoseTheirFormatting()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout", "Normal");
        var first = AddRect(batch);
        var second = AddRect(batch, 4.0f);
        _styles.Apply(batch, 1, first, "Callout");
        _styles.Apply(batch, 1, second, "Callout");

        var deleted = _styles.Delete(batch, "Callout");

        // Visio deletes a style in use without complaint and silently reverts those shapes to
        // 'No Style', so the count has to be taken before the delete and reported.
        Assert.True(deleted.Success, deleted.ErrorMessage);
        Assert.Contains("2 shape(s)", deleted.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(_styles.List(batch).Styles, s => s.Name == "Callout");
    }

    [Fact]
    public void Delete_WhenUnused_SaysSo()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout");

        var deleted = _styles.Delete(batch, "Callout");

        Assert.Contains("No shapes were using it", deleted.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownStyle_ListsTheOnesThatExist()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<ArgumentException>(() => _styles.Read(batch, "NoSuchStyle"));

        // Visio's own message is "Object name not found", which names neither.
        Assert.Contains("Normal", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadFormula_ForACellTheStyleDoesNotHave_ExplainsTheNaming()
    {
        using var batch = CreateDocument();
        _styles.Create(batch, "Callout");

        var ex = Assert.Throws<ArgumentException>(() => _styles.ReadFormula(batch, "Callout", "NotACell"));

        Assert.Contains("FillForegnd", ex.Message, StringComparison.Ordinal);
    }

    private static string ReadShapeCell(IVisioBatch batch, string shapeName, string cellName) =>
        batch.Execute((ctx, ct) =>
        {
            dynamic page = ctx.Document.Pages[1];
            dynamic shape = page.Shapes.Item(shapeName);
            return (string)shape.CellsU[cellName].FormulaU;
        });

    private string AddRect(IVisioBatch batch, float left = 1.0f)
    {
        var added = _shapes.AddShape(batch, 1, 1, left, 1.0f, 2.0f, 1.0f);
        Assert.True(added.Success, added.ErrorMessage);

        return _shapes.List(batch, 1).Shapes[^1].Name;
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"StyleTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
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
