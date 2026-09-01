using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Master;

/// <summary>
/// Legacy PowerPoint slide master and layout operations.
///
/// SUPPRESSED (#19): every action is implemented against PowerPoint COM (<c>SlideMasters</c>,
/// <c>CustomLayouts</c>) and throws <c>RuntimeBinderException</c> on a Visio Document, which has
/// <c>Masters</c> instead. Kept off the public surface until reimplemented against
/// <c>Document.Masters</c> (#34).
/// </summary>
[ServiceCategory("master")]
[McpTool("master", Title = "Master & Layout Operations", Destructive = false, Category = "design",
    PublicSurface = false,
    Description = "Legacy PowerPoint slide master operations. Not exposed: throws on Visio documents. "
    + "Pending reimplementation against Document.Masters (#34).")]
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
