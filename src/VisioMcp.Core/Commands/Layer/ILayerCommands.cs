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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("list")]
    LayerListResult List(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Read one layer and include its member shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name, as reported by list</param>
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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name to delete. Shapes on the layer are not deleted</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex, string layerName);

    /// <summary>
    /// Add a shape to a layer.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Target layer name, as reported by list</param>
    /// <param name="shapeName">Shape to add, as reported by shape(list)</param>
    /// <param name="preserveMembership">True keeps the shape's existing layer memberships; false makes this its only layer. A Visio shape may belong to several layers at once</param>
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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer to remove the shape from, as reported by list</param>
    /// <param name="shapeName">Shape to remove, as reported by shape(list)</param>
    /// <param name="preserveMembership">True leaves the shape's other layer memberships intact. The shape itself is not deleted</param>
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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name</param>
    /// <param name="visible">True to show the layer in the drawing window</param>
    [ServiceAction("set-visibility")]
    OperationResult SetVisibility(IVisioBatch batch, int pageIndex, string layerName, bool visible);

    /// <summary>
    /// Set whether a layer is printable.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name</param>
    /// <param name="printable">True to include the layer when the page is printed or exported</param>
    [ServiceAction("set-print")]
    OperationResult SetPrint(IVisioBatch batch, int pageIndex, string layerName, bool printable);

    /// <summary>
    /// Set whether a layer is locked.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name</param>
    /// <param name="locked">True to prevent shapes on the layer from being selected or edited</param>
    [ServiceAction("set-lock")]
    OperationResult SetLock(IVisioBatch batch, int pageIndex, string layerName, bool locked);

    /// <summary>
    /// Set the Visio color index for a layer.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="layerName">Layer name</param>
    /// <param name="colorIndex">Visio layer palette index. 255 is the default, meaning shapes keep their own colours rather than taking the layer colour</param>
    [ServiceAction("set-color")]
    OperationResult SetColor(IVisioBatch batch, int pageIndex, string layerName, int colorIndex);
}