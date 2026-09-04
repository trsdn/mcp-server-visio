using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.PrintOptions;

/// <summary>
/// Read and write Visio print options on the document and on a page's Print Properties section.
/// </summary>
[ServiceCategory("printoptions")]
[McpTool("printoptions", Title = "Print Options", Destructive = true, Category = "print",
    Description = "Options only: this tool never calls Document.Print, Document.PrintOut, or Page.Print. "
    + "Use it to read and update Document.Print* settings plus PageSheet print cells for one page. "
    + "Fixed-format output remains in the export tool; PDF/XPS export is not duplicated here. "
    + "SCALE: print_scale is a fraction of full size: 1.0 = 100%, 0.75 = 75%, 2.0 = 200%; passing 75 means 7500%. "
    + "MARGINS: page_*_margin_inches values are inches. "
    + "COPIES: Visio exposes copy count only through Document.PrintOut, so there is no print_copies parameter.")]
public interface IPrintOptionsCommands
{
    /// <summary>
    /// Read document print settings and PageSheet print cells for a page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index whose PageSheet print cells should be used</param>
    [ServiceAction("get")]
    PrintOptionsResult GetSettings(IVisioBatch batch, int pageIndex = 1);

    /// <summary>
    /// Set document print settings and PageSheet print cells. Only non-null values are changed.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index whose PageSheet print cells should be used</param>
    /// <param name="printLandscape">Document-wide printer page orientation toggle: false = portrait, true = landscape</param>
    /// <param name="printCenteredH">Whether to center the drawing horizontally on the printer page</param>
    /// <param name="printCenteredV">Whether to center the drawing vertically on the printer page</param>
    /// <param name="paperSize">Document VisPaperSizes value: 0=Unknown, 1=Letter, 5=Legal, 8=A3, 9=A4, 11=A5, 12=B4 JIS, 13=B5 JIS, 14=Folio, 18=Note, 24=C, 25=D, 26=E</param>
    /// <param name="printer">Document Printer name. Passing the current name is safe and does not print</param>
    /// <param name="printFitOnPages">Whether to fit the drawing to a specific number of printed pages using print_pages_across and print_pages_down</param>
    /// <param name="printPagesAcross">Number of printed pages across when print_fit_on_pages is true</param>
    /// <param name="printPagesDown">Number of printed pages down when print_fit_on_pages is true</param>
    /// <param name="printScale">Print scale as a fraction of full size: 1.0 = 100%, 0.75 = 75%, 2.0 = 200%. This is NOT a percentage - passing 75 means 7500%.</param>
    /// <param name="printPageOrientation">PageSheet PrintPageOrientation cell: 0=same as printer, 1=portrait, 2=landscape</param>
    /// <param name="printGrid">Whether to print the drawing grid</param>
    /// <param name="paperKind">PageSheet PaperKind cell using Windows DMPAPER values; common values include 1=Letter, 5=Legal, 8=A3, 9=A4, 11=A5</param>
    /// <param name="centerX">Whether to center the page horizontally on the printer page</param>
    /// <param name="centerY">Whether to center the page vertically on the printer page</param>
    /// <param name="pageLeftMarginInches">PageSheet PageLeftMargin cell. Value is in inches.</param>
    /// <param name="pageRightMarginInches">PageSheet PageRightMargin cell. Value is in inches.</param>
    /// <param name="pageTopMarginInches">PageSheet PageTopMargin cell. Value is in inches.</param>
    /// <param name="pageBottomMarginInches">PageSheet PageBottomMargin cell. Value is in inches.</param>
    [ServiceAction("set")]
    OperationResult SetSettings(
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
        double? pageBottomMarginInches = null);
}
