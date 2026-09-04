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
    /// <param name="printLandscape">Document PrintLandscape option</param>
    /// <param name="printCenteredH">Document PrintCenteredH option</param>
    /// <param name="printCenteredV">Document PrintCenteredV option</param>
    /// <param name="paperSize">Document PaperSize value</param>
    /// <param name="printer">Document Printer name. Passing the current name is safe and does not print</param>
    /// <param name="printFitOnPages">Document PrintFitOnPages option</param>
    /// <param name="printPagesAcross">Document PrintPagesAcross value</param>
    /// <param name="printPagesDown">Document PrintPagesDown value</param>
    /// <param name="printScale">Document PrintScale percentage</param>
    /// <param name="printPageOrientation">PageSheet PrintPageOrientation value</param>
    /// <param name="printGrid">PageSheet PrintGrid cell</param>
    /// <param name="paperKind">PageSheet PaperKind value</param>
    /// <param name="centerX">PageSheet CenterX cell</param>
    /// <param name="centerY">PageSheet CenterY cell</param>
    /// <param name="pageLeftMarginInches">PageSheet PageLeftMargin cell, in inches</param>
    /// <param name="pageRightMarginInches">PageSheet PageRightMargin cell, in inches</param>
    /// <param name="pageTopMarginInches">PageSheet PageTopMargin cell, in inches</param>
    /// <param name="pageBottomMarginInches">PageSheet PageBottomMargin cell, in inches</param>
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
