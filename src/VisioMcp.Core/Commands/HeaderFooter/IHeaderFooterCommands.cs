using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.HeaderFooter;

/// <summary>
/// Legacy PowerPoint-only presentation header/footer commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("headerfooter")]
[McpTool("headerfooter", Title = "Legacy PowerPoint Headers & Footers", Destructive = true, Category = "headerfooter", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: get and set presentation-wide footer text, slide numbers, and date display. "
    + "Use 'get' to see current settings. Use 'set' with show_footer/show_slide_number/show_date (bool) "
    + "and footer_text (string). Pass null to leave a setting unchanged.")]
public interface IHeaderFooterCommands
{
    /// <summary>Get header/footer settings for the presentation.</summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("get")]
    HeaderFooterResult GetInfo(IVisioBatch batch);

    /// <summary>
    /// Set header/footer options. Pass null to leave unchanged.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="footerText">Footer text (null = don't change)</param>
    /// <param name="showFooter">Show footer on slides</param>
    /// <param name="showSlideNumber">Show slide numbers</param>
    /// <param name="showDate">Show date/time</param>
    [ServiceAction("set")]
    OperationResult Update(IVisioBatch batch, string? footerText, bool? showFooter, bool? showSlideNumber, bool? showDate);
}
