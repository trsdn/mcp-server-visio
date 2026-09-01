using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Shape;

/// <summary>
/// Shape management: list, read, create, move, resize, delete, z-order, group, connect.
/// </summary>
[ServiceCategory("shape")]
[McpTool("shape", Title = "Shape Operations", Destructive = true, Category = "shapes",
    Description = "Create, move, resize, group, connect, and manage shapes on pages. The primary building tool. "
    + "'add-shape' draws a primitive: auto_shape_type 9=Oval, anything else=Rectangle. "
    + "For real Visio geometry use the 'stencil' tool to drop a master instead. "
    + "z_order_cmd: 1=BringToFront, 2=SendToBack, 3=BringForward, 4=SendBackward. "
    + "merge_type: 1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract. "
    + "connector_type: 1=Straight, 2=Elbow, 3=Curve. "
    + "Positions/sizes in points (72pt = 1 inch), measured from the top-left of the page. "
    + "Fill, line, rotation, and scale are not part of this tool: write the corresponding "
    + "ShapeSheet cells with the 'cell' tool (for example FillForegnd, LineColor, LineWeight, Angle, Width, Height).")]
public interface IShapeCommands
{
    /// <summary>
    /// List all shapes on a page.
    /// </summary>
    [ServiceAction("list")]
    ShapeListResult List(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Get detailed info about a specific shape.
    /// </summary>
    [ServiceAction("read")]
    ShapeDetailResult Read(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// List all group shapes on a page.
    /// </summary>
    [ServiceAction("list-groups")]
    ShapeListResult ListGroups(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Read a single group shape together with its member shapes.
    /// </summary>
    [ServiceAction("read-group")]
    ShapeDetailResult ReadGroup(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// List the shapes currently selected in the active window for the specified page.
    /// </summary>
    [ServiceAction("list-selection")]
    ShapeSelectionResult ListSelection(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Replace the current page selection with the specified shapes.
    /// </summary>
    [ServiceAction("select-shapes")]
    OperationResult SelectShapes(IVisioBatch batch, int pageIndex, string shapeNames);

    /// <summary>
    /// Add shapes to the current page selection.
    /// </summary>
    [ServiceAction("add-to-selection")]
    OperationResult AddToSelection(IVisioBatch batch, int pageIndex, string shapeNames);

    /// <summary>
    /// Remove shapes from the current page selection.
    /// </summary>
    [ServiceAction("remove-from-selection")]
    OperationResult RemoveFromSelection(IVisioBatch batch, int pageIndex, string shapeNames);

    /// <summary>
    /// Clear the current page selection.
    /// </summary>
    [ServiceAction("clear-selection")]
    OperationResult ClearSelection(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// List all custom Shape Data properties on a shape.
    /// </summary>
    [ServiceAction("list-properties")]
    ShapePropertyListResult ListProperties(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Read a single custom Shape Data property from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape to read from</param>
    /// <param name="propertyName">Shape Data row name to read (required for this action)</param>
    [ServiceAction("get-property")]
    ShapePropertyResult GetProperty(IVisioBatch batch, int pageIndex, string shapeName, string? propertyName = null);

    /// <summary>
    /// Set a custom Shape Data property on a shape, creating the row if needed.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape to modify</param>
    /// <param name="propertyName">Shape Data row name to write (required for this action)</param>
    /// <param name="propertyValue">Value to store in the Shape Data row (required for this action)</param>
    [ServiceAction("set-property")]
    OperationResult SetProperty(IVisioBatch batch, int pageIndex, string shapeName, string? propertyName = null, string? propertyValue = null);

    /// <summary>
    /// Delete a custom Shape Data property row from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape to modify</param>
    /// <param name="propertyName">Shape Data row name to delete (required for this action)</param>
    [ServiceAction("delete-property")]
    OperationResult DeleteProperty(IVisioBatch batch, int pageIndex, string shapeName, string? propertyName = null);

    /// <summary>
    /// List all 1-D connector shapes on a page together with their glued endpoints.
    /// </summary>
    [ServiceAction("list-connectors")]
    ConnectorListResult ListConnectors(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Read the topology of a specific connector shape, including start and end targets.
    /// </summary>
    [ServiceAction("read-connector")]
    ConnectorDetailResult ReadConnector(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// List connection records for a shape or connector using real Visio topology.
    /// </summary>
    [ServiceAction("list-connections")]
    ShapeConnectionListResult ListConnections(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Disconnect one endpoint of a connector while keeping the connector shape on the page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Connector shape name</param>
    /// <param name="connectorEnd">Connector endpoint selector: start or end</param>
    [ServiceAction("disconnect-connector")]
    ConnectorDetailResult DisconnectConnector(IVisioBatch batch, int pageIndex, string shapeName, string connectorEnd);

    /// <summary>
    /// Reconnect one endpoint of a connector to a target shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Connector shape name</param>
    /// <param name="connectorEnd">Connector endpoint selector: start or end</param>
    /// <param name="targetShapeName">Target shape name for the selected endpoint</param>
    [ServiceAction("reconnect-connector")]
    ConnectorDetailResult ReconnectConnector(IVisioBatch batch, int pageIndex, string shapeName, string connectorEnd, string targetShapeName);

    /// <summary>
    /// Add a textbox shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="left">Position from left in points</param>
    /// <param name="top">Position from top in points</param>
    /// <param name="width">Width in points</param>
    /// <param name="height">Height in points</param>
    /// <param name="text">Initial text content</param>
    [ServiceAction("add-textbox")]
    OperationResult AddTextbox(IVisioBatch batch, int pageIndex, float left, float top, float width, float height, string text);

    /// <summary>
    /// Add a rectangle, ellipse, or other auto-shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="autoShapeType">MsoAutoShapeType integer (1=Rectangle, 9=Oval, etc.)</param>
    /// <param name="left">Position from left in points</param>
    /// <param name="top">Position from top in points</param>
    /// <param name="width">Width in points</param>
    /// <param name="height">Height in points</param>
    [ServiceAction("add-shape")]
    OperationResult AddShape(IVisioBatch batch, int pageIndex, int autoShapeType, float left, float top, float width, float height);

    /// <summary>
    /// Move and/or resize a shape.
    /// </summary>
    [ServiceAction("move-resize")]
    OperationResult MoveResize(IVisioBatch batch, int pageIndex, string shapeName, float? left, float? top, float? width, float? height);

    /// <summary>
    /// Delete a shape by name.
    /// </summary>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Change the z-order of a shape (bring to front, send to back, etc.).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="zOrderCmd">1=BringToFront, 2=SendToBack, 3=BringForward, 4=SendBackward</param>
    [ServiceAction("z-order")]
    OperationResult ZOrder(IVisioBatch batch, int pageIndex, string shapeName, int zOrderCmd);

    /// <summary>
    /// Group multiple shapes into a single group shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated list of shape names to group</param>
    [ServiceAction("group")]
    OperationResult Group(IVisioBatch batch, int pageIndex, string shapeNames);

    /// <summary>
    /// Ungroup a group shape into individual shapes.
    /// </summary>
    [ServiceAction("ungroup")]
    OperationResult Ungroup(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Add a connector line between two shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="connectorType">1=Straight, 2=Elbow, 3=Curve</param>
    /// <param name="startShapeName">Starting shape name</param>
    /// <param name="endShapeName">Ending shape name</param>
    [ServiceAction("add-connector")]
    OperationResult AddConnector(IVisioBatch batch, int pageIndex, int connectorType, string startShapeName, string endShapeName);

    /// <summary>
    /// Merge shapes using boolean operations.
    /// mergeType: 1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated shape names to merge</param>
    /// <param name="mergeType">1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract</param>
    [ServiceAction("merge")]
    OperationResult MergeShapes(IVisioBatch batch, int pageIndex, string shapeNames, int mergeType);
    /// <summary>
    /// Duplicate a shape on the same page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape to duplicate</param>
    [ServiceAction("duplicate")]
    OperationResult Duplicate(IVisioBatch batch, int pageIndex, string shapeName);
}
