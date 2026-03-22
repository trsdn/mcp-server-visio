using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Slide;

/// <summary>
/// Legacy PowerPoint-only slide lifecycle commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("slide")]
[McpTool("slide", Title = "Legacy PowerPoint Slide Operations", Destructive = true, Category = "slides", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "Prefer page/shape/text/cell/stencil for Visio workflows. "
    + "If you still use this legacy surface: create, duplicate, move, delete, and inspect slides. "
    + "WORKFLOW: file(open) → slide(create, layoutName='Blank') → shape(add-shape) → text(set). "
    + "layout_name: 'Title Slide', 'Title and Content', 'Blank', 'Two Content', 'Section Header', 'Title Only' (from master). "
    + "Use 'list' to see all slides. 'read' for detailed slide info with all shapes. "
    + "'summary' for presentation overview. 'clone-with-replace' for mail-merge style duplication. "
    + "All indices are 1-based. position=0 means 'at end'.")]
public interface ISlideCommands
{
    /// <summary>
    /// List all slides in the presentation with metadata.
    /// </summary>
    [ServiceAction("list")]
    SlideListResult List(IVisioBatch batch);

    /// <summary>
    /// Get detailed information about a specific slide including all shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("read")]
    SlideDetailResult Read(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Add a new slide at the specified position with a layout.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="position">1-based insert position (0 = at end)</param>
    /// <param name="layoutName">Layout name from the slide master (e.g. "Title Slide", "Blank")</param>
    [ServiceAction("create")]
    OperationResult Create(IVisioBatch batch, int position, string layoutName);

    /// <summary>
    /// Duplicate an existing slide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based index of slide to duplicate</param>
    [ServiceAction("duplicate")]
    OperationResult Duplicate(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Move a slide to a new position.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based index of slide to move</param>
    /// <param name="newPosition">1-based target position</param>
    [ServiceAction("move")]
    OperationResult Move(IVisioBatch batch, int slideIndex, int newPosition);

    /// <summary>
    /// Delete a slide by index.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based index of slide to delete</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Apply a layout to an existing slide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="layoutName">Layout name from the slide master</param>
    [ServiceAction("apply-layout")]
    OperationResult ApplyLayout(IVisioBatch batch, int slideIndex, string layoutName);

    /// <summary>Set the name of a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="name">New name for the slide</param>
    [ServiceAction("set-name")]
    OperationResult SetName(IVisioBatch batch, int slideIndex, string name);

    /// <summary>
    /// Clone a slide multiple times and replace text placeholders in each clone.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based index of the source slide to clone</param>
    /// <param name="count">Number of clones to create</param>
    /// <param name="searchText">Text to search for in each clone</param>
    /// <param name="replaceText">Text to replace with in each clone</param>
    [ServiceAction("clone-with-replace")]
    OperationResult CloneWithReplace(IVisioBatch batch, int slideIndex, int count, string searchText, string replaceText);

    /// <summary>
    /// Hide a slide so it is skipped during slideshow playback.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("hide")]
    OperationResult Hide(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Unhide a slide so it is included during slideshow playback.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("unhide")]
    OperationResult Unhide(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Export a slide as a PNG thumbnail to the specified file path.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="destinationPath">Full path for the output PNG file</param>
    [ServiceAction("get-thumbnail")]
    OperationResult GetThumbnail(IVisioBatch batch, int slideIndex, string destinationPath);

    /// <summary>
    /// Get a summary of the presentation including slide count, dimensions, and metadata.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("summary")]
    OperationResult Summary(IVisioBatch batch);

    /// <summary>
    /// Show or hide master slide shapes (background graphics) on a slide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="display">Whether to display master shapes</param>
    [ServiceAction("set-display-master")]
    OperationResult SetDisplayMaster(IVisioBatch batch, int slideIndex, bool display);

    /// <summary>
    /// Copy a slide to the clipboard.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("copy")]
    OperationResult CopyToClipboard(IVisioBatch batch, int slideIndex);
}
