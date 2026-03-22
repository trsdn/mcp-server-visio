using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Export;

/// <summary>
/// Export Visio documents and pages to portable output formats.
/// </summary>
[ServiceCategory("export")]
[McpTool("export", Title = "Export Operations", Destructive = false, Category = "export",
    Description = "Export Visio documents and pages to shareable output. "
    + "WORKFLOW: file(open) → build pages/shapes → export(to-pdf) or export(page-export). "
    + "page-export uses the destination file extension to choose the Visio export filter, for example .png, .jpg, .svg, .svgz, .emf, or .wmf. "
    + "PDF and XPS export operate on the full document or an optional page range.")]
public interface IExportCommands
{
    /// <summary>
    /// Export the active Visio document to PDF.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="destinationPath">Output PDF path</param>
    /// <param name="fromPage">Optional 1-based start page for ranged export</param>
    /// <param name="toPage">Optional 1-based end page for ranged export</param>
    [ServiceAction("to-pdf")]
    ExportResult ToPdf(IVisioBatch batch, string destinationPath, int? fromPage, int? toPage);

    /// <summary>
    /// Export the active Visio document to XPS.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="destinationPath">Output XPS path</param>
    /// <param name="fromPage">Optional 1-based start page for ranged export</param>
    /// <param name="toPage">Optional 1-based end page for ranged export</param>
    [ServiceAction("to-xps")]
    ExportResult ToXps(IVisioBatch batch, string destinationPath, int? fromPage, int? toPage);

    /// <summary>
    /// Export one page using the filter implied by the destination file extension.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="destinationPath">Output file path such as .png, .jpg, or .svg</param>
    [ServiceAction("page-export")]
    ExportResult PageExport(IVisioBatch batch, int pageIndex, string destinationPath);

    /// <summary>
    /// Print the active document using Visio's document print pipeline.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="copies">Number of copies (default 1)</param>
    /// <param name="fromPage">Optional 1-based start page for ranged print</param>
    /// <param name="toPage">Optional 1-based end page for ranged print</param>
    [ServiceAction("print")]
    OperationResult Print(IVisioBatch batch, int copies, int? fromPage, int? toPage);

    /// <summary>
    /// Save a copy of the current document without rebinding the active session.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="destinationPath">Full path for the copied document</param>
    [ServiceAction("save-copy")]
    ExportResult SaveCopy(IVisioBatch batch, string destinationPath);
}
