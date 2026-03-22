using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Layer;

/// <summary>
/// Visio layer management for page-level organization, visibility, and shape membership.
/// </summary>
[ServiceCategory("layer")]
[McpTool("layer", Title = "Layer Operations", Destructive = true, Category = "layers",
    Description = "Manage Visio page layers. Use layers to group shapes, toggle visibility/print/lock, "
    + "and organize diagram structure. color_index uses Visio layer palette values (default Visio color is 255). "
    + "WORKFLOW: file(open) -> page(create) -> layer(create) -> shape(add-shape) -> layer(add-shape).")]
public interface ILayerCommands
{
    /// <summary>
    /// List all layers on a page.
    /// </summary>
    [ServiceAction("list")]
    LayerListResult List(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Read one layer and include its member shapes.
    /// </summary>
    [ServiceAction("read")]
    LayerDetailResult Read(IVisioBatch batch, int pageIndex, string layerName);

    /// <summary>
    /// Create a new layer on a page.
    /// </summary>
    [ServiceAction("create")]
    OperationResult Create(
        IVisioBatch batch,
        int pageIndex,
        string layerName,
        int? colorIndex = null,
        bool? visible = null,
        bool? printable = null,
        bool? locked = null);

    /// <summary>
    /// Delete a layer from a page.
    /// </summary>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex, string layerName);

    /// <summary>
    /// Add a shape to a layer.
    /// </summary>
    [ServiceAction("add-shape")]
    OperationResult AddShape(
        IVisioBatch batch,
        int pageIndex,
        string layerName,
        string shapeName,
        bool preserveMembership = true);

    /// <summary>
    /// Remove a shape from a layer.
    /// </summary>
    [ServiceAction("remove-shape")]
    OperationResult RemoveShape(
        IVisioBatch batch,
        int pageIndex,
        string layerName,
        string shapeName,
        bool preserveMembership = true);

    /// <summary>
    /// Set whether a layer is visible.
    /// </summary>
    [ServiceAction("set-visibility")]
    OperationResult SetVisibility(IVisioBatch batch, int pageIndex, string layerName, bool visible);

    /// <summary>
    /// Set whether a layer is printable.
    /// </summary>
    [ServiceAction("set-print")]
    OperationResult SetPrint(IVisioBatch batch, int pageIndex, string layerName, bool printable);

    /// <summary>
    /// Set whether a layer is locked.
    /// </summary>
    [ServiceAction("set-lock")]
    OperationResult SetLock(IVisioBatch batch, int pageIndex, string layerName, bool locked);

    /// <summary>
    /// Set the Visio color index for a layer.
    /// </summary>
    [ServiceAction("set-color")]
    OperationResult SetColor(IVisioBatch batch, int pageIndex, string layerName, int colorIndex);
}
