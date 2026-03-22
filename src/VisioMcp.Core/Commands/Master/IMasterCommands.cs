using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Master;

/// <summary>
/// Slide master and layout operations: list masters, list layouts, get placeholders.
/// </summary>
[ServiceCategory("master")]
[McpTool("master", Title = "Master & Layout Operations", Destructive = false, Category = "design",
    Description = "Inspect and edit slide masters and layouts. Use 'list' to see all masters with their layouts. "
    + "'list-layouts' for layouts of a specific master. 'list-shapes' to see shapes on a master. "
    + "'edit-shape-text' to change text on master shapes (e.g. company name in footer). "
    + "'delete-unused' removes masters not referenced by any slide. master_index: 1-based.")]
public interface IMasterCommands
{
    /// <summary>List all slide masters and their custom layouts.</summary>
    [ServiceAction("list")]
    MasterListResult List(IVisioBatch batch);

    /// <summary>List all shapes on a specific slide master.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterIndex">1-based slide master index</param>
    [ServiceAction("list-shapes")]
    OperationResult ListShapes(IVisioBatch batch, int masterIndex);

    /// <summary>Edit the text content of a shape on a slide master.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterIndex">1-based slide master index</param>
    /// <param name="shapeName">Name of the shape to edit</param>
    /// <param name="text">New text content</param>
    [ServiceAction("edit-shape-text")]
    OperationResult EditShapeText(IVisioBatch batch, int masterIndex, string shapeName, string text);

    /// <summary>List all custom layouts for a specific slide master.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterIndex">1-based slide master index</param>
    [ServiceAction("list-layouts")]
    OperationResult ListLayouts(IVisioBatch batch, int masterIndex);

    /// <summary>Delete unused slide masters that have no slides referencing them. Will not delete the last remaining master.</summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("delete-unused")]
    OperationResult DeleteUnused(IVisioBatch batch);
}
