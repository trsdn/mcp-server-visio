using System.Globalization;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Cell;
using VisioMcp.Core.Commands.Shape;
using VisioMcp.Core.Commands.Text;
using Xunit;
using Xunit.Abstractions;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Integration coverage for the text actions reimplemented against Visio's Character and Paragraph
/// ShapeSheet sections in #20.
///
/// All 13 were written against PowerPoint's <c>TextFrame.TextRange.Font</c> and
/// <c>ParagraphFormat</c> and threw <c>RuntimeBinderException</c> on every call against a
/// <c>.vsdx</c>. Per Rule 30 they are covered by integration tests against a real Visio instance.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("Feature", "Text")]
public sealed class TextFormattingTests(ITestOutputHelper output) : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly TextCommands _text = new();
    private readonly ShapeCommands _shapes = new();
    private readonly CellCommands _cells = new();

    [Fact]
    public void Format_WritesCharacterAndParagraphCells()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        var result = _text.Format(batch, 1, shapeName,
            fontName: "Consolas", fontSize: 18f, bold: true, italic: null,
            color: "#FF0000", alignment: "center", verticalAlignment: "bottom");

        Assert.True(result.Success, result.ErrorMessage);
        output.WriteLine(result.Message);

        Assert.Contains("Consolas", ReadFormula(batch, shapeName, "Char.Font"), StringComparison.Ordinal);
        Assert.Contains("RGB(255,0,0)", ReadFormula(batch, shapeName, "Char.Color"), StringComparison.Ordinal);
        Assert.Equal(18d / 72d, ReadNumber(batch, shapeName, "Char.Size"), precision: 4);
        Assert.Equal(1d, ReadNumber(batch, shapeName, "Para.HorzAlign"), precision: 3);
        Assert.Equal(2d, ReadNumber(batch, shapeName, "VerticalAlign"), precision: 3);
    }

    [Fact]
    public void Format_BoldThenItalic_DoesNotClearBold()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        // Char.Style is a bitfield. Setting italic must read-modify-write, or bold is lost - the
        // single most likely bug in this port.
        Assert.True(_text.Format(batch, 1, shapeName, null, null, bold: true, italic: null, null, null, null).Success);
        Assert.True(_text.Format(batch, 1, shapeName, null, null, bold: null, italic: true, null, null, null).Success);

        int style = (int)ReadNumber(batch, shapeName, "Char.Style");
        output.WriteLine($"Char.Style = {style}");

        Assert.Equal(1, style & 1);  // bold survived
        Assert.Equal(2, style & 2);  // italic applied
    }

    [Fact]
    public void Format_RejectsUnknownAlignment()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        Assert.Throws<ArgumentException>(() =>
            _text.Format(batch, 1, shapeName, null, null, null, null, null, "diagonal", null));
    }

    [Fact]
    public void FormatAdvanced_AppliesUnderlineAndReportsUnsupported()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        var result = _text.FormatAdvanced(batch, 1, shapeName,
            underline: true, strikethrough: true, subscript: true, superscript: null);

        Assert.True(result.Success, result.ErrorMessage);
        output.WriteLine(result.Message);

        Assert.Equal(4, (int)ReadNumber(batch, shapeName, "Char.Style") & 4);

        // Silently dropping these would leave the caller believing they applied.
        Assert.Contains("strikethrough", result.Message, StringComparison.Ordinal);
        Assert.Contains("subscript", result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("superscript", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetSpacing_WritesParagraphCellsAndReportsUnsupported()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        var result = _text.SetSpacing(batch, 1, shapeName,
            lineSpacing: 1.5f, spaceBefore: 6f, spaceAfter: 12f, characterSpacing: 2f);

        Assert.True(result.Success, result.ErrorMessage);
        output.WriteLine(result.Message);

        Assert.Equal(6d / 72d, ReadNumber(batch, shapeName, "Para.SpBefore"), precision: 4);
        Assert.Equal(12d / 72d, ReadNumber(batch, shapeName, "Para.SpAfter"), precision: 4);
        Assert.Contains("characterSpacing", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadSpacing_ReportsWhatSetSpacingWrote()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        Assert.True(_text.SetSpacing(batch, 1, shapeName, null, spaceBefore: 9f, spaceAfter: null, null).Success);

        var read = _text.ReadSpacing(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        // Also pins invariant formatting: a German locale would render "9" as "9" but 0.5 as "0,5".
        Assert.Contains("SpaceBefore: 9pt", read.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetBullets_ThenReadBullets_RoundTrips()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        Assert.True(_text.SetBullets(batch, 1, shapeName, bulletType: 2, bulletCharacter: null, indentLevel: 2).Success);

        var read = _text.ReadBullets(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine(read.Message);

        Assert.Contains("style 2", read.Message, StringComparison.Ordinal);
        // indentLevel 2 = 0.5in = 36pt
        Assert.Contains("indent 36pt", read.Message, StringComparison.Ordinal);

        Assert.True(_text.SetBullets(batch, 1, shapeName, 0, null, 0).Success);
        Assert.Equal("Bullets: none", _text.ReadBullets(batch, 1, shapeName).Message);
    }

    [Fact]
    public void SetBullets_RejectsUnknownStyle()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        Assert.Throws<ArgumentOutOfRangeException>(() => _text.SetBullets(batch, 1, shapeName, 99, null, 0));
    }

    [Theory]
    [InlineData(0, "the quick brown fox. and more!")]
    [InlineData(1, "THE QUICK BROWN FOX. AND MORE!")]
    [InlineData(2, "The Quick Brown Fox. And More!")]
    [InlineData(3, "The quick brown fox. And more!")]
    public void ChangeCase_TransformsStoredText(int caseType, string expected)
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch, "the QUICK brown Fox. and more!");

        Assert.True(_text.ChangeCase(batch, 1, shapeName, caseType).Success);

        // Deliberately asserting the stored text, not a display attribute: Char.Case would leave
        // the text unchanged and text(get) would still return the original.
        var read = _text.GetText(batch, 1, shapeName);
        Assert.True(read.Success, read.ErrorMessage);
        output.WriteLine($"caseType {caseType} -> {read.Text}");

        Assert.Equal(expected, read.Text?.TrimEnd('\n', '\r'));
    }

    [Fact]
    public void ChangeCase_RejectsUnknownCaseType()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        Assert.Throws<ArgumentOutOfRangeException>(() => _text.ChangeCase(batch, 1, shapeName, 9));
    }

    [Fact]
    public void InsertSymbol_AppendsCodePoint()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch, "Temp: ");

        // U+00B0 DEGREE SIGN
        Assert.True(_text.InsertSymbol(batch, 1, shapeName, "Arial", 0x00B0).Success);

        var read = _text.GetText(batch, 1, shapeName);
        output.WriteLine(read.Text);
        Assert.Contains("°", read.Text ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void InsertSymbol_RejectsSurrogateCodePoint()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        // char.ConvertFromUtf32 throws for lone surrogates; the caller should get a parameter
        // error rather than an opaque conversion failure.
        Assert.Throws<ArgumentOutOfRangeException>(() => _text.InsertSymbol(batch, 1, shapeName, "Arial", 0xD800));
    }

    [Fact]
    public void InsertDateTime_AppendsInvariantFormattedDate()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch, "Updated ");

        Assert.True(_text.InsertDateTime(batch, 1, shapeName, 3).Success);

        var read = _text.GetText(batch, 1, shapeName);
        output.WriteLine(read.Text);

        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), read.Text ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void InsertSlideNumber_AppendsPageIndex()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch, "Page ");

        var result = _text.InsertSlideNumber(batch, 1, shapeName);
        Assert.True(result.Success, result.ErrorMessage);
        output.WriteLine(result.Message);

        Assert.Contains("Page 1", _text.GetText(batch, 1, shapeName).Text ?? string.Empty, StringComparison.Ordinal);
        // The caller must be told this is literal text, not a field that tracks reordering.
        Assert.Contains("literal text", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AltTextAudit_FindsShapesWithoutComment()
    {
        using var batch = CreateDocument();
        var documented = AddTextShape(batch);
        var undocumented = AddTextShape(batch);

        Assert.True(_shapes.SetAltText(batch, 1, documented, "Described").Success);

        var audit = _text.AltTextAudit(batch, 1);
        Assert.True(audit.Success, audit.ErrorMessage);
        output.WriteLine(audit.Message);

        Assert.Contains(undocumented, audit.Message, StringComparison.Ordinal);
        Assert.DoesNotContain($"{documented},", audit.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyPlaceholderAudit_ReportsNoVisioEquivalent()
    {
        using var batch = CreateDocument();

        var ex = Assert.Throws<NotSupportedException>(() => _text.EmptyPlaceholderAudit(batch, 1));
        output.WriteLine(ex.Message);

        Assert.Contains("no Visio equivalent", ex.Message, StringComparison.Ordinal);
        Assert.Contains("shape(list)", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InsertLink_ReportsNoVisioEquivalent()
    {
        using var batch = CreateDocument();
        var shapeName = AddTextShape(batch);

        var ex = Assert.Throws<NotSupportedException>(
            () => _text.InsertLink(batch, 1, shapeName, "docs", "https://example.com"));

        output.WriteLine(ex.Message);
        Assert.Contains("no Visio equivalent", ex.Message, StringComparison.Ordinal);
        Assert.Contains("#35", ex.Message, StringComparison.Ordinal);
    }

    private string ReadFormula(IVisioBatch batch, string shapeName, string cellName)
    {
        var result = _cells.ReadFormula(batch, 1, shapeName, cellName);
        Assert.True(result.Success, result.ErrorMessage);
        return result.Cell?.Formula ?? string.Empty;
    }

    private double ReadNumber(IVisioBatch batch, string shapeName, string cellName)
    {
        var result = _cells.Read(batch, 1, shapeName, cellName);
        Assert.True(result.Success, result.ErrorMessage);
        return double.Parse(result.Cell?.Value ?? "0", CultureInfo.InvariantCulture);
    }

    private IVisioBatch CreateDocument()
    {
        var path = Path.Join(Path.GetTempPath(), $"TextFormattingTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);

        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);
        return VisioSession.BeginBatch(path);
    }

    private string AddTextShape(IVisioBatch batch, string text = "Sample text")
    {
        var added = _shapes.AddTextbox(batch, 1, 1.0f, 1.0f, 3.0f, 1.0f, text);
        Assert.True(added.Success, added.ErrorMessage);

        var listed = _shapes.List(batch, 1);
        Assert.True(listed.Success, listed.ErrorMessage);
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
