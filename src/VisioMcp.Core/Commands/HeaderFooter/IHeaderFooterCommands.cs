using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.HeaderFooter;

/// <summary>
/// Document headers and footers: the six text fields printed above and below every page.
/// </summary>
[ServiceCategory("headerfooter")]
[McpTool("headerfooter", Title = "Headers & Footers", Destructive = true, Category = "headerfooter",
    Description = "Read and write the document's header and footer text. "
    + "SCOPE: headers and footers are DOCUMENT-wide in Visio, not per-page, so there is no page index. "
    + "They appear on printed output and in print preview, not on the drawing canvas — a shape placed "
    + "at the top of a page is a different thing. "
    + "FIELD CODES: the text may contain codes Visio expands at output time — '&p' page number, "
    + "'&P' page count, '&d' short date, '&D' long date, '&t' time, '&f' file name, '&n' page name, "
    + "'&&' a literal ampersand. They are stored verbatim, so 'get' returns the code rather than the "
    + "expanded value. "
    + "MARGINS: header_margin and footer_margin are the distance from the paper edge, in inches.")]
public interface IHeaderFooterCommands
{
    /// <summary>
    /// Read the six header/footer fields and both margins.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("get")]
    HeaderFooterResult GetInfo(IVisioBatch batch);

    /// <summary>
    /// Set header/footer text and margins. Every parameter is optional: omit one to leave it
    /// unchanged, or pass an empty string to clear a text field.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="headerLeft">Text in the left header position. Empty string clears it</param>
    /// <param name="headerCenter">Text in the centre header position. Empty string clears it</param>
    /// <param name="headerRight">Text in the right header position. Empty string clears it</param>
    /// <param name="footerLeft">Text in the left footer position. Empty string clears it</param>
    /// <param name="footerCenter">Text in the centre footer position. Empty string clears it</param>
    /// <param name="footerRight">Text in the right footer position. Empty string clears it</param>
    /// <param name="headerMargin">Header distance from the paper edge, in inches</param>
    /// <param name="footerMargin">Footer distance from the paper edge, in inches</param>
    [ServiceAction("set")]
    OperationResult Update(
        IVisioBatch batch,
        string? headerLeft,
        string? headerCenter,
        string? headerRight,
        string? footerLeft,
        string? footerCenter,
        string? footerRight,
        double? headerMargin,
        double? footerMargin);
}
