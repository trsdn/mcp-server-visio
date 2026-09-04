using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.PrintOptions;
using Xunit;

namespace VisioMcp.Core.Tests.Integration;

/// <summary>
/// Print options ported to Visio <c>Document.Print*</c> properties and PageSheet print cells (#65).
///
/// Integration tests against real Visio (Rule 30). This domain must never call
/// <c>Document.Print</c>, <c>Document.PrintOut</c>, or <c>Page.Print</c>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Speed", "Medium")]
[Trait("Layer", "Core")]
[Trait("RequiresVisio", "true")]
[Trait("Feature", "PrintOptions")]
public sealed class PrintOptionsTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly PrintOptionsCommands _printOptions = new();

    [Fact]
    public void ANewDocument_ReadsVisioNativePrintDefaults()
    {
        using var batch = CreateDocument();

        var read = _printOptions.GetSettings(batch);

        Assert.True(read.Success, read.ErrorMessage);
        Assert.Equal(1, read.PageIndex);
        Assert.True(read.PaperSize > 0);
        Assert.True(read.PaperHeightInches > 0);
        Assert.True(read.PaperWidthInches > 0);
        Assert.True(read.PageLeftMarginInches > 0);
        Assert.True(read.PageTopMarginInches > 0);
    }

    [Fact]
    public void DocumentPrintSettings_RoundTrip()
    {
        using var batch = CreateDocument();
        var initial = _printOptions.GetSettings(batch);
        Assert.False(string.IsNullOrWhiteSpace(initial.Printer));

        _printOptions.SetSettings(
            batch,
            pageIndex: 1,
            printLandscape: true,
            printCenteredH: true,
            printCenteredV: true,
            paperSize: 9,
            printer: initial.Printer,
            printFitOnPages: true,
            printPagesAcross: 2,
            printPagesDown: 3,
            printScale: 75,
            printPageOrientation: null,
            printGrid: null,
            paperKind: null,
            centerX: null,
            centerY: null,
            pageLeftMarginInches: null,
            pageRightMarginInches: null,
            pageTopMarginInches: null,
            pageBottomMarginInches: null);

        var read = _printOptions.GetSettings(batch, pageIndex: 1);

        Assert.True(read.PrintLandscape);
        Assert.True(read.PrintCenteredH);
        Assert.True(read.PrintCenteredV);
        Assert.Equal(9, read.PaperSize);
        Assert.Equal(initial.Printer, read.Printer);
        Assert.True(read.PrintFitOnPages);
        Assert.Equal(2, read.PrintPagesAcross);
        Assert.Equal(3, read.PrintPagesDown);
        Assert.Equal(75, read.PrintScale, precision: 3);
    }

    [Fact]
    public void PageSheetPrintSettings_RoundTrip()
    {
        using var batch = CreateDocument();

        _printOptions.SetSettings(
            batch,
            pageIndex: 1,
            printLandscape: null,
            printCenteredH: null,
            printCenteredV: null,
            paperSize: null,
            printer: null,
            printFitOnPages: null,
            printPagesAcross: null,
            printPagesDown: null,
            printScale: null,
            printPageOrientation: 1,
            printGrid: true,
            paperKind: 9,
            centerX: true,
            centerY: true,
            pageLeftMarginInches: 0.5,
            pageRightMarginInches: 0.55,
            pageTopMarginInches: 0.6,
            pageBottomMarginInches: 0.65);

        var read = _printOptions.GetSettings(batch);

        Assert.Equal(1, read.PrintPageOrientation);
        Assert.True(read.PrintGrid);
        Assert.Equal(9, read.PaperKind);
        Assert.True(read.CenterX);
        Assert.True(read.CenterY);
        Assert.Equal(0.5, read.PageLeftMarginInches, precision: 3);
        Assert.Equal(0.55, read.PageRightMarginInches, precision: 3);
        Assert.Equal(0.6, read.PageTopMarginInches, precision: 3);
        Assert.Equal(0.65, read.PageBottomMarginInches, precision: 3);
    }

    [Fact]
    public void OmittedSettings_AreLeftUnchanged()
    {
        using var batch = CreateDocument();
        SetKnownValues(batch);

        _printOptions.SetSettings(
            batch,
            pageIndex: 1,
            printLandscape: false,
            printCenteredH: null,
            printCenteredV: null,
            paperSize: null,
            printer: null,
            printFitOnPages: null,
            printPagesAcross: null,
            printPagesDown: null,
            printScale: null,
            printPageOrientation: null,
            printGrid: null,
            paperKind: null,
            centerX: null,
            centerY: null,
            pageLeftMarginInches: null,
            pageRightMarginInches: null,
            pageTopMarginInches: null,
            pageBottomMarginInches: null);

        var read = _printOptions.GetSettings(batch);

        Assert.False(read.PrintLandscape);
        Assert.True(read.PrintCenteredH);
        Assert.Equal(2, read.PrintPagesAcross);
        Assert.True(read.PrintGrid);
        Assert.Equal(0.5, read.PageLeftMarginInches, precision: 3);
    }

    [Fact]
    public void SupplyingNothing_SucceedsAndChangesNothing()
    {
        using var batch = CreateDocument();
        SetKnownValues(batch);

        var result = _printOptions.SetSettings(
            batch,
            pageIndex: 1,
            printLandscape: null,
            printCenteredH: null,
            printCenteredV: null,
            paperSize: null,
            printer: null,
            printFitOnPages: null,
            printPagesAcross: null,
            printPagesDown: null,
            printScale: null,
            printPageOrientation: null,
            printGrid: null,
            paperKind: null,
            centerX: null,
            centerY: null,
            pageLeftMarginInches: null,
            pageRightMarginInches: null,
            pageTopMarginInches: null,
            pageBottomMarginInches: null);

        var read = _printOptions.GetSettings(batch);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(read.PrintCenteredH);
        Assert.Equal(0.65, read.PageBottomMarginInches, precision: 3);
    }

    [Fact]
    public void Settings_SurviveSaveAndReopen()
    {
        var path = NewDocumentPath();
        VisioSession.CreateNew(path, isMacroEnabled: false, (ctx, ct) => 0);

        using (var batch = VisioSession.BeginBatch(path))
        {
            _printOptions.SetSettings(
                batch,
                pageIndex: 1,
                printLandscape: true,
                printCenteredH: true,
                printCenteredV: false,
                paperSize: 9,
                printer: null,
                printFitOnPages: true,
                printPagesAcross: 2,
                printPagesDown: 3,
                printScale: 80,
                printPageOrientation: 1,
                printGrid: true,
                paperKind: 9,
                centerX: true,
                centerY: false,
                pageLeftMarginInches: 0.4,
                pageRightMarginInches: 0.45,
                pageTopMarginInches: 0.5,
                pageBottomMarginInches: 0.55);

            batch.Save();
        }

        using var reopened = VisioSession.BeginBatch(path);
        var read = _printOptions.GetSettings(reopened);

        Assert.Equal(1, read.PrintPageOrientation);
        Assert.True(read.PrintGrid);
        Assert.Equal(9, read.PaperKind);
        Assert.True(read.CenterX);
        Assert.False(read.CenterY);
        Assert.Equal(0.4, read.PageLeftMarginInches, precision: 3);
        Assert.Equal(0.45, read.PageRightMarginInches, precision: 3);
        Assert.Equal(0.5, read.PageTopMarginInches, precision: 3);
        Assert.Equal(0.55, read.PageBottomMarginInches, precision: 3);
    }

    private void SetKnownValues(IVisioBatch batch)
    {
        _printOptions.SetSettings(
            batch,
            pageIndex: 1,
            printLandscape: true,
            printCenteredH: true,
            printCenteredV: true,
            paperSize: 9,
            printer: null,
            printFitOnPages: true,
            printPagesAcross: 2,
            printPagesDown: 3,
            printScale: 75,
            printPageOrientation: 1,
            printGrid: true,
            paperKind: 9,
            centerX: true,
            centerY: true,
            pageLeftMarginInches: 0.5,
            pageRightMarginInches: 0.55,
            pageTopMarginInches: 0.6,
            pageBottomMarginInches: 0.65);
    }

    private string NewDocumentPath()
    {
        var path = Path.Join(Path.GetTempPath(), $"PrintOptionsTests_{Guid.NewGuid():N}.vsdx");
        _tempFiles.Add(path);
        return path;
    }

    private IVisioBatch CreateDocument()
    {
        var path = NewDocumentPath();
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
