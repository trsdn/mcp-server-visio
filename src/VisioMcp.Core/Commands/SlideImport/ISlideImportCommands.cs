using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.SlideImport;

/// <summary>
/// Legacy PowerPoint-only slide import commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("slideimport")]
[McpTool("slideimport", Title = "Legacy PowerPoint Slide Import", Destructive = true, Category = "slideimport", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: import slides from another .pptx/.pptm file into the current presentation. "
    + "slide_indices: comma-separated 1-based (e.g. '1,3,5'). Empty = import all slides. "
    + "insert_at: 0 = append at end. Source file must not be open in another session.")]
public interface ISlideImportCommands
{
    /// <summary>
    /// Import slides from another PowerPoint file.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="sourceFilePath">Path to the source .pptx file</param>
    /// <param name="slideIndices">Comma-separated 1-based slide indices to import (empty = all)</param>
    /// <param name="insertAt">Position to insert (0 = at end)</param>
    [ServiceAction("import")]
    OperationResult ImportSlides(IVisioBatch batch, string sourceFilePath, string slideIndices, int insertAt);
}
