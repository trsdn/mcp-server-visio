using System.Globalization;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Cell;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for the shape formatting actions reimplemented against Visio's ShapeSheet
/// in #20.
///
/// These were previously written against PowerPoint COM (<c>Shape.Fill</c>, <c>Shape.Line</c>,
/// <c>Shape.Rotation</c>, <c>ScaleWidth</c>, <c>LockAspectRatio</c>) and threw
/// <c>RuntimeBinderException</c> on every call against a <c>.vsdx</c>.
///
/// Per Rule 30 they are covered by integration tests against a real Visio instance — a mocked
/// version would assert only that the mock returns what it was told, and would not have caught
/// either bug real execution found here: <c>CellExistsU</c> returning <c>short</c> rather than
/// <c>bool</c>, and invariant-culture formatting of formula values.
///
/// Each setter is verified through a reader rather than by return code, so the test proves the
/// value reached Visio rather than merely that the call did not throw.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("Feature", "Shape")]
public sealed class ShapeFormattingTests(ITestOutputHelper output) : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly ShapeCommands _shapes = new();
    private readonly CellCommands _cells = new();

    [Fact]
    public void SetFill_ThenReadFill_RoundTripsColour()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var set = _shapes.SetFill(batch, 1, shapeName, "#FF0000");
        Assert.True(set.Success, set.ErrorMessage);

        var read = _shapes.ReadFill(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        Assert.Contains("RGB(255,0,0)", read.Message, StringComparison.Ordinal);
        Assert.Contains("Solid", read.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetFill_None_ReportsNoFill()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetFill(batch, 1, shapeName, "#00FF00").Success);
        Assert.True(_shapes.SetFill(batch, 1, shapeName, "none").Success);

        var read = _shapes.ReadFill(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("Fill: None", read.Message);
    }

    [Fact]
    public void SetFill_RejectsMalformedColour()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        // A bad colour must fail loudly rather than write a broken formula that corrupts the shape.
        Assert.Throws<ArgumentException>(() => _shapes.SetFill(batch, 1, shapeName, "not-a-colour"));
    }

    [Fact]
    public void SetLine_ThenReadLine_RoundTripsColourAndWeight()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var set = _shapes.SetLine(batch, 1, shapeName, "#0000FF", 3f);
        Assert.True(set.Success, set.ErrorMessage);

        var read = _shapes.ReadLine(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        Assert.Contains("RGB(0,0,255)", read.Message, StringComparison.Ordinal);
        // LineWeight is stored in inches internally; 3pt must survive the round trip.
        Assert.Contains("3pt", read.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetRotation_WritesNegatedAngle()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetRotation(batch, 1, shapeName, 45f).Success);

        var angle = _cells.ReadFormula(batch, 1, shapeName, "Angle");
        Assert.True(angle.Success, angle.ErrorMessage);
        output.WriteLine($"Angle = {angle.Cell?.Formula}");

        // Visio measures anticlockwise, so a clockwise 45 is stored as -45.
        Assert.Contains("-45", angle.Cell?.Formula ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void Flip_TogglesFlipXCell()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.Flip(batch, 1, shapeName, 0).Success);
        var afterFirst = ReadCellValue(batch, shapeName, "FlipX");
        output.WriteLine($"FlipX after first flip = {afterFirst}");

        // Flipping twice must return the shape to its original orientation.
        Assert.True(_shapes.Flip(batch, 1, shapeName, 0).Success);
        var afterSecond = ReadCellValue(batch, shapeName, "FlipX");
        output.WriteLine($"FlipX after second flip = {afterSecond}");

        Assert.NotEqual(afterFirst, afterSecond);
    }

    [Fact]
    public void Flip_RejectsUnknownFlipType()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.Throws<ArgumentOutOfRangeException>(() => _shapes.Flip(batch, 1, shapeName, 7));
    }

    [Fact]
    public void Scale_MultipliesWidthAndHeight()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        double beforeWidth = ReadCellNumber(batch, shapeName, "Width");
        double beforeHeight = ReadCellNumber(batch, shapeName, "Height");

        Assert.True(_shapes.Scale(batch, 1, shapeName, 1.5f, 2f).Success);

        double afterWidth = ReadCellNumber(batch, shapeName, "Width");
        double afterHeight = ReadCellNumber(batch, shapeName, "Height");

        output.WriteLine($"width {beforeWidth} -> {afterWidth}, height {beforeHeight} -> {afterHeight}");

        Assert.Equal(beforeWidth * 1.5, afterWidth, precision: 3);
        Assert.Equal(beforeHeight * 2.0, afterHeight, precision: 3);
    }

    [Fact]
    public void SetOpacity_WritesTransparencyAsInverse()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetFill(batch, 1, shapeName, "#123456").Success);
        Assert.True(_shapes.SetOpacity(batch, 1, shapeName, 0.25f).Success);

        var read = _shapes.ReadFill(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        // opacity 0.25 -> transparency 0.75. Asserting the exact string also pins the
        // invariant-culture formatting: a German-locale "0,75" would fail here.
        Assert.Contains("Transparency: 0.75", read.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetOpacity_RejectsOutOfRange()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.Throws<ArgumentOutOfRangeException>(() => _shapes.SetOpacity(batch, 1, shapeName, 1.5f));
    }

    [Fact]
    public void SetLockAspectRatio_AddsProtectionRowWhenMissing()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        // A freshly drawn shape has no Protection section, so this exercises the
        // AddSection / AddRow path rather than a plain cell write.
        Assert.True(_shapes.SetLockAspectRatio(batch, 1, shapeName, locked: true).Success);
        var locked = ReadCellValue(batch, shapeName, "LockAspect");
        output.WriteLine($"LockAspect locked = {locked}");

        Assert.True(_shapes.SetLockAspectRatio(batch, 1, shapeName, locked: false).Success);
        var unlocked = ReadCellValue(batch, shapeName, "LockAspect");
        output.WriteLine($"LockAspect unlocked = {unlocked}");

        Assert.NotEqual(locked, unlocked);
    }

    [Fact]
    public void SetShadow_ThenReadShadow_RoundTripsOffsets()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetShadow(batch, 1, shapeName, visible: true, offsetX: 6f, offsetY: 4f).Success);

        var read = _shapes.ReadShadow(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        Assert.Contains("Visible: true", read.Message, StringComparison.Ordinal);
        // Offsets are stored in inches internally; points must survive the round trip.
        Assert.Contains("OffsetX: 6pt", read.Message, StringComparison.Ordinal);
        Assert.Contains("OffsetY: 4pt", read.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetShadow_Hidden_ReportsNotVisible()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetShadow(batch, 1, shapeName, visible: true, offsetX: 3f, offsetY: 3f).Success);
        Assert.True(_shapes.SetShadow(batch, 1, shapeName, visible: false, offsetX: 0f, offsetY: 0f).Success);

        var read = _shapes.ReadShadow(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal("Visible: false", read.Message);
    }

    [Fact]
    public void SetAltText_WritesCommentCell()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        Assert.True(_shapes.SetAltText(batch, 1, shapeName, "Process step").Success);

        // Comment is a string cell, so its numeric ResultIU is always 0. The formula is where the
        // text lives, which is why this reads the formula rather than the value.
        var comment = _cells.ReadFormula(batch, 1, shapeName, "Comment");
        Assert.True(comment.Success, comment.ErrorMessage);
        output.WriteLine($"Comment formula = {comment.Cell?.Formula}");

        Assert.Equal("\"Process step\"", comment.Cell?.Formula);
    }

    [Fact]
    public void SetAltText_EscapesEmbeddedQuotes()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        // An unescaped quote would terminate the ShapeSheet string formula early and either throw
        // or silently truncate, so this is the case worth pinning.
        Assert.True(_shapes.SetAltText(batch, 1, shapeName, "The \"main\" step").Success);

        var comment = _cells.ReadFormula(batch, 1, shapeName, "Comment");
        Assert.True(comment.Success, comment.ErrorMessage);
        output.WriteLine($"Comment formula = {comment.Cell?.Formula}");

        // Visio doubles embedded quotes inside a string formula.
        Assert.Equal("\"The \"\"main\"\" step\"", comment.Cell?.Formula);
    }

    [Fact]
    public void CopyToSlide_CopiesShapeToAnotherPage()
    {
        using var batch = CreateDocument();
        var shapeName = AddRectangle(batch);

        var pages = new Commands.Page.PageCommands();
        Assert.True(pages.Create(batch, 0, "Target").Success);

        var before = _shapes.List(batch, 2);
        Assert.True(before.Success, before.ErrorMessage);
        int beforeCount = before.Shapes.Count;

        var copied = _shapes.CopyToSlide(batch, 1, shapeName, 2);
        Assert.True(copied.Success, copied.ErrorMessage);
        output.WriteLine(copied.Message);

        var after = _shapes.List(batch, 2);
        Assert.True(after.Success, after.ErrorMessage);
        Assert.Equal(beforeCount + 1, after.Shapes.Count);
    }

    [Fact]
    public void FindByType_MatchesVisioShapeTypes()
    {
        using var batch = CreateDocument();
        AddRectangle(batch);

        // visTypeShape = 3, confirmed against a live instance. The PowerPoint original compared
        // against MsoShapeType values, where 3 means something else entirely.
        var found = _shapes.FindByType(batch, 1, 3);
        Assert.True(found.Success, found.ErrorMessage);
        output.WriteLine(found.Message);
        Assert.Contains("Found", found.Message, StringComparison.Ordinal);
        Assert.Contains("Shape (3)", found.Message, StringComparison.Ordinal);

        // visTypeGroup = 2; nothing on the page is grouped.
        var none = _shapes.FindByType(batch, 1, 2);
        Assert.True(none.Success, none.ErrorMessage);
        output.WriteLine(none.Message);
        Assert.Contains("No shapes", none.Message, StringComparison.Ordinal);
    }

    private string? ReadCellValue(IVisioBatch batch, string shapeName, string cellName)
    {
        var result = _cells.Read(batch, 1, shapeName, cellName);
        Assert.True(result.Success, result.ErrorMessage);
        return result.Cell?.Value;
    }

    private double ReadCellNumber(IVisioBatch batch, string shapeName, string cellName)
    {
        var raw = ReadCellValue(batch, shapeName, cellName);
        Assert.False(string.IsNullOrWhiteSpace(raw), $"Cell '{cellName}' returned no value.");
        return double.Parse(raw!, CultureInfo.InvariantCulture);
    }

    /// <summary>Creates an empty document and returns an open batch over it.</summary>
    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"ShapeFormattingTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    /// <summary>Draws a rectangle on page 1 and returns its generated name.</summary>
    private string AddRectangle(IVisioBatch batch)
    {
        // autoShapeType 1 = rectangle.
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
                // The file may still be briefly held after the batch disposes. Leaving a stray
                // temp file is preferable to failing the run during cleanup.
            }
        }
    }
}
