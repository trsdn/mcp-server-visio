using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Placeholder;

/// <summary>
/// Legacy PowerPoint-only slide placeholder operations retained from the bootstrap template.
/// </summary>
[ServiceCategory("placeholder")]
[McpTool("placeholder", Title = "Legacy PowerPoint Slide Placeholders", Destructive = true, Category = "placeholders", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: list and fill layout placeholders (title, subtitle, content, footer). "
    + "Use 'list' to discover available placeholders and their indices. "
    + "'set-text' fills a placeholder with text. 'set-image' replaces placeholder content with an image. "
    + "placeholder_index: 1-based (from list results). Prefer placeholders over freeform shapes for consistent layouts.")]
public interface IPlaceholderCommands
{
    /// <summary>List all placeholders on a slide with type and current text.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("list")]
    PlaceholderListResult List(IVisioBatch batch, int slideIndex);

    /// <summary>Set text content of a placeholder by index.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="placeholderIndex">1-based placeholder index</param>
    /// <param name="text">Text to set</param>
    [ServiceAction("set-text")]
    OperationResult SetText(IVisioBatch batch, int slideIndex, int placeholderIndex, string text);

    /// <summary>Replace placeholder content with an image from a file path.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="placeholderIndex">1-based placeholder index</param>
    /// <param name="imagePath">Absolute path to the image file</param>
    [ServiceAction("set-image")]
    OperationResult SetImage(IVisioBatch batch, int slideIndex, int placeholderIndex, string imagePath);
}
