using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.PrintOptions;

public class PrintOptionsCommands : IPrintOptionsCommands
{
    private const int VisNoCast = 0;
    private const string PrintPageOrientationCell = "PrintPageOrientation";
    private const string PrintGridCell = "PrintGrid";
    private const string PaperKindCell = "PaperKind";
    private const string CenterXCell = "CenterX";
    private const string CenterYCell = "CenterY";
    private const string PageLeftMarginCell = "PageLeftMargin";
    private const string PageRightMarginCell = "PageRightMargin";
    private const string PageTopMarginCell = "PageTopMargin";
    private const string PageBottomMarginCell = "PageBottomMargin";

    public PrintOptionsResult GetSettings(IVisioBatch batch, int pageIndex = 1)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic doc = ctx.Document;
            dynamic? page = null;
            dynamic? pageSheet = null;
            try
            {
                page = GetPage(ctx, pageIndex);
                pageSheet = page.PageSheet;

                return new PrintOptionsResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    PrintLandscape = Convert.ToBoolean(doc.PrintLandscape),
                    PrintCenteredH = Convert.ToBoolean(doc.PrintCenteredH),
                    PrintCenteredV = Convert.ToBoolean(doc.PrintCenteredV),
                    PaperSize = Convert.ToInt32(doc.PaperSize),
                    Printer = doc.Printer?.ToString(),
                    PrintFitOnPages = Convert.ToBoolean(doc.PrintFitOnPages),
                    PrintPagesAcross = Convert.ToInt32(doc.PrintPagesAcross),
                    PrintPagesDown = Convert.ToInt32(doc.PrintPagesDown),
                    PrintScale = Convert.ToDouble(doc.PrintScale, CultureInfo.InvariantCulture),
                    PaperHeightInches = Convert.ToDouble(doc.PaperHeight(VisNoCast), CultureInfo.InvariantCulture),
                    PaperWidthInches = Convert.ToDouble(doc.PaperWidth(VisNoCast), CultureInfo.InvariantCulture),
                    PrintPageOrientation = ReadIntCell(pageSheet, PrintPageOrientationCell),
                    PrintGrid = ReadBoolCell(pageSheet, PrintGridCell),
                    PaperKind = ReadIntCell(pageSheet, PaperKindCell),
                    CenterX = ReadBoolCell(pageSheet, CenterXCell),
                    CenterY = ReadBoolCell(pageSheet, CenterYCell),
                    PageLeftMarginInches = ReadDoubleCell(pageSheet, PageLeftMarginCell),
                    PageRightMarginInches = ReadDoubleCell(pageSheet, PageRightMarginCell),
                    PageTopMarginInches = ReadDoubleCell(pageSheet, PageTopMarginCell),
                    PageBottomMarginInches = ReadDoubleCell(pageSheet, PageBottomMarginCell)
                };
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetSettings(
        IVisioBatch batch,
        int pageIndex = 1,
        bool? printLandscape = null,
        bool? printCenteredH = null,
        bool? printCenteredV = null,
        int? paperSize = null,
        string? printer = null,
        bool? printFitOnPages = null,
        int? printPagesAcross = null,
        int? printPagesDown = null,
        double? printScale = null,
        int? printPageOrientation = null,
        bool? printGrid = null,
        int? paperKind = null,
        bool? centerX = null,
        bool? centerY = null,
        double? pageLeftMarginInches = null,
        double? pageRightMarginInches = null,
        double? pageTopMarginInches = null,
        double? pageBottomMarginInches = null)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic doc = ctx.Document;
            dynamic? page = null;
            dynamic? pageSheet = null;
            try
            {
                var changes = new List<string>();

                page = GetPage(ctx, pageIndex);
                pageSheet = page.PageSheet;

                if (printLandscape.HasValue) { doc.PrintLandscape = printLandscape.Value; changes.Add("print_landscape"); }
                if (printCenteredH.HasValue) { doc.PrintCenteredH = printCenteredH.Value; changes.Add("print_centered_h"); }
                if (printCenteredV.HasValue) { doc.PrintCenteredV = printCenteredV.Value; changes.Add("print_centered_v"); }
                if (paperSize.HasValue) { doc.PaperSize = paperSize.Value; changes.Add("paper_size"); }
                if (printer is not null) { doc.Printer = printer; changes.Add("printer"); }
                if (printFitOnPages.HasValue) { doc.PrintFitOnPages = printFitOnPages.Value; changes.Add("print_fit_on_pages"); }
                if (printPagesAcross.HasValue) { doc.PrintPagesAcross = printPagesAcross.Value; changes.Add("print_pages_across"); }
                if (printPagesDown.HasValue) { doc.PrintPagesDown = printPagesDown.Value; changes.Add("print_pages_down"); }
                if (printScale.HasValue) { doc.PrintScale = printScale.Value; changes.Add("print_scale"); }
                if (printPageOrientation.HasValue) { SetIntCell(pageSheet, PrintPageOrientationCell, printPageOrientation.Value); changes.Add("print_page_orientation"); }
                if (printGrid.HasValue) { SetBoolCell(pageSheet, PrintGridCell, printGrid.Value); changes.Add("print_grid"); }
                if (paperKind.HasValue) { SetIntCell(pageSheet, PaperKindCell, paperKind.Value); changes.Add("paper_kind"); }
                if (centerX.HasValue) { SetBoolCell(pageSheet, CenterXCell, centerX.Value); changes.Add("center_x"); }
                if (centerY.HasValue) { SetBoolCell(pageSheet, CenterYCell, centerY.Value); changes.Add("center_y"); }
                if (pageLeftMarginInches.HasValue) { SetDoubleCell(pageSheet, PageLeftMarginCell, pageLeftMarginInches.Value); changes.Add("page_left_margin_inches"); }
                if (pageRightMarginInches.HasValue) { SetDoubleCell(pageSheet, PageRightMarginCell, pageRightMarginInches.Value); changes.Add("page_right_margin_inches"); }
                if (pageTopMarginInches.HasValue) { SetDoubleCell(pageSheet, PageTopMarginCell, pageTopMarginInches.Value); changes.Add("page_top_margin_inches"); }
                if (pageBottomMarginInches.HasValue) { SetDoubleCell(pageSheet, PageBottomMarginCell, pageBottomMarginInches.Value); changes.Add("page_bottom_margin_inches"); }

                return new OperationResult
                {
                    Success = true,
                    Action = "set",
                    Message = changes.Count == 0
                        ? "No print settings supplied; nothing changed"
                        : $"Updated {string.Join(", ", changes)}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static int ReadIntCell(dynamic pageSheet, string cellName)
        => Convert.ToInt32(ReadDoubleCell(pageSheet, cellName));

    private static bool ReadBoolCell(dynamic pageSheet, string cellName)
        => ReadDoubleCell(pageSheet, cellName) != 0;

    private static double ReadDoubleCell(dynamic pageSheet, string cellName)
    {
        dynamic? cell = null;
        try
        {
            cell = pageSheet.CellsU(cellName);
            return Convert.ToDouble(cell.ResultIU, CultureInfo.InvariantCulture);
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static void SetIntCell(dynamic pageSheet, string cellName, int value)
        => SetDoubleCell(pageSheet, cellName, value);

    private static void SetBoolCell(dynamic pageSheet, string cellName, bool value)
    {
        dynamic? cell = null;
        try
        {
            cell = pageSheet.CellsU(cellName);
            cell.FormulaU = value ? "TRUE" : "FALSE";
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static void SetDoubleCell(dynamic pageSheet, string cellName, double value)
    {
        dynamic? cell = null;
        try
        {
            cell = pageSheet.CellsU(cellName);
            cell.ResultIU = value;
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }
}
