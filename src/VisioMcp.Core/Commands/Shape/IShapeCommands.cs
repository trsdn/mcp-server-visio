using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Shape;

/// <summary>
/// Shape management: list, read, create, move, resize, delete, z-order.
/// </summary>
[ServiceCategory("shape")]
[McpTool("shape", Title = "Shape Operations", Destructive = true, Category = "shapes",
    Description = "Create, move, resize, format, and manage shapes on pages. The primary building tool. "
    + "add-shape draws only two primitives: auto_shape_type=9 draws an ellipse, any other value draws a rectangle. "
    + "For any other shape, drop a stencil master with stencil(drop-master) — Visio has no auto-shape gallery. "
    + "color_hex: '#RRGGBB' (e.g. '#0B3D91'). Use 'none' for transparent fill/line. "
    + "z_order_cmd: 1=BringToFront, 2=SendToBack, 3=BringForward, 4=SendBackward. "
    + "merge_type: 1=Union, 2=Combine, 3=Fragment, 4=Intersect, 5=Subtract. "
    + "connector_type: 1=Straight, 2=Elbow (right-angle), 3=Curved. flip_type: 0=Horizontal, 1=Vertical. "
    + "CONNECTORS vs CONNECTION POINTS: a connector is the line between two shapes (add-connector, read-connector); "
    + "a connection point is an anchor on a shape that a connector glues to (add-connection-point). "
    + "PREFER connect-shapes over add-connector: it uses Visio's Dynamic connector, which routes around shapes in "
    + "the way and re-routes when either end moves, and it chains a whole path in one call — "
    + "shape_names='A,B,C' creates A→B and B→C. add-connector draws a straight glued line between exactly two shapes. "
    + "Connection point x/y are ShapeSheet formulas such as 'Width*0.5' so the point follows the shape when it is resized. "
    + "gradient_style: 1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown. "
    + "find-by-type takes a Visio shape type: 2=Group, 3=Shape, 4=ForeignObject (images and OLE), 5=Guide. "
    + "Positions/sizes in points (72pt = 1 inch).")]
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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    [ServiceAction("list-properties")]
    ShapePropertyListResult ListProperties(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Read a single custom Shape Data property from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="propertyName">Shape Data property name, matched against the row label or the underlying Prop.&lt;row&gt; name, case-insensitively</param>
    [ServiceAction("get-property")]
    ShapePropertyResult GetProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName);

    /// <summary>
    /// Set a custom Shape Data property on a shape, creating the row if needed.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="propertyName">Shape Data property name. Names that are not valid Prop.&lt;row&gt; row names are normalized automatically</param>
    /// <param name="propertyValue">Property value, stored as a string. Omit to store an empty value</param>
    [ServiceAction("set-property")]
    OperationResult SetProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName, string? propertyValue = null);

    /// <summary>
    /// Delete a custom Shape Data property row from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="propertyName">Shape Data property name, matched against the row label or the underlying Prop.&lt;row&gt; name, case-insensitively</param>
    [ServiceAction("delete-property")]
    OperationResult DeleteProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName);

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
    /// List the connection points on a shape — the anchors a connector can glue to.
    /// </summary>
    /// <remarks>
    /// Connection points are not connectors. A connector is the line between two shapes; a
    /// connection point is a position on a shape that a connector attaches to.
    /// </remarks>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by list</param>
    [ServiceAction("list-connection-points")]
    ConnectionPointListResult ListConnectionPoints(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Add a connection point to a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by list</param>
    /// <param name="connectionPointX">X position as a ShapeSheet formula. Prefer a relative expression such as 'Width*0.5' so the point stays put when the shape is resized; a literal needs units, for example '1 in'</param>
    /// <param name="connectionPointY">Y position as a ShapeSheet formula, for example 'Height*1' for the top edge. Visio measures Y upward</param>
    /// <param name="connectionPointName">Optional name. A named point becomes the glue target 'Connections.&lt;name&gt;', which survives rows being added or deleted; an unnamed point is addressed only by index</param>
    [ServiceAction("add-connection-point")]
    ConnectionPointResult AddConnectionPoint(IVisioBatch batch, int pageIndex, string shapeName, string? connectionPointX = null, string? connectionPointY = null, string? connectionPointName = null);

    /// <summary>
    /// Move an existing connection point.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by list</param>
    /// <param name="connectionPointIndex">0-based index in the shape's connection point list, as reported by list-connection-points</param>
    /// <param name="connectionPointX">New X position as a ShapeSheet formula</param>
    /// <param name="connectionPointY">New Y position as a ShapeSheet formula</param>
    [ServiceAction("set-connection-point")]
    ConnectionPointResult SetConnectionPoint(IVisioBatch batch, int pageIndex, string shapeName, int connectionPointIndex = 0, string? connectionPointX = null, string? connectionPointY = null);

    /// <summary>
    /// Delete a connection point from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by list</param>
    /// <param name="connectionPointIndex">0-based index. Points below it shift up, so delete from the highest index first when removing several. Any connector glued to the point loses its attachment</param>
    [ServiceAction("delete-connection-point")]
    OperationResult DeleteConnectionPoint(IVisioBatch batch, int pageIndex, string shapeName, int connectionPointIndex);

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
    /// Draw a rectangle or an ellipse on a page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="autoShapeType">Shape to draw: 9 draws an ellipse, any other value draws a rectangle. Only these two primitives are supported — Visio has no auto-shape gallery, so richer shapes come from stencil masters via the 'stencil' tool.</param>
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
    /// Set the fill color of a shape. Use 'none' to remove fill (transparent).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="colorHex">Hex color string like #FF0000 for red, or 'none' for no fill</param>
    [ServiceAction("set-fill")]
    OperationResult SetFill(IVisioBatch batch, int pageIndex, string shapeName, string colorHex);

    /// <summary>
    /// Set the line/border color and width of a shape. Use 'none' to remove the line.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="colorHex">Hex color like #000000 or 'none' to remove border</param>
    /// <param name="lineWidth">Line width in points (default 0.75)</param>
    [ServiceAction("set-line")]
    OperationResult SetLine(IVisioBatch batch, int pageIndex, string shapeName, string colorHex, float lineWidth);

    /// <summary>
    /// Set the rotation angle of a shape in degrees.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="degrees">Rotation in degrees, clockwise. Visio's underlying Angle cell measures anticlockwise in radians; this parameter is converted for you</param>
    [ServiceAction("set-rotation")]
    OperationResult SetRotation(IVisioBatch batch, int pageIndex, string shapeName, float degrees);

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
    /// Set the alternative text (alt text) of a shape for accessibility.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="altText">Alternative text describing the shape for assistive technology. Stored in the shape's Comment ShapeSheet cell, which is where Visio keeps alt text</param>
    [ServiceAction("set-alt-text")]
    OperationResult SetAltText(IVisioBatch batch, int pageIndex, string shapeName, string altText);

    /// <summary>
    /// Copy a shape to another page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based source page index</param>
    /// <param name="shapeName">Name of the shape to copy</param>
    /// <param name="targetPageIndex">1-based target page index</param>
    [ServiceAction("copy-to-page")]
    OperationResult CopyToPage(IVisioBatch batch, int pageIndex, string shapeName, int targetPageIndex);

    /// <summary>
    /// Set shadow effect on a shape. Use visible=false to remove shadow.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="visible">Show or hide shadow</param>
    /// <param name="offsetX">Shadow offset X in points</param>
    /// <param name="offsetY">Shadow offset Y in points</param>
    [ServiceAction("set-shadow")]
    OperationResult SetShadow(IVisioBatch batch, int pageIndex, string shapeName, bool visible, float offsetX, float offsetY);

    /// <summary>
    /// Add a straight connector line between two shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="connectorType">Routing style: 1=Straight, 2=Elbow (right-angle), 3=Curved</param>
    /// <param name="startShapeName">Starting shape name</param>
    /// <param name="endShapeName">Ending shape name</param>
    [ServiceAction("add-connector")]
    OperationResult AddConnector(IVisioBatch batch, int pageIndex, int connectorType, string startShapeName, string endShapeName);

    /// <summary>
    /// Connect two or more shapes with Visio dynamic connectors, chained in the order given.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated shape names to chain, in order. Two or more required; N names produce N-1 connectors</param>
    /// <param name="connectorType">Routing style applied to every connector created: 1=Straight, 2=Elbow (right-angle), 3=Curved. Omit to keep Visio's default routing</param>
    [ServiceAction("connect-shapes")]
    ConnectorListResult ConnectShapes(IVisioBatch batch, int pageIndex, string shapeNames, int? connectorType = null);

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

    /// <summary>
    /// Flip a shape horizontally or vertically.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name</param>
    /// <param name="flipType">0=Horizontal, 1=Vertical</param>
    [ServiceAction("flip")]
    OperationResult Flip(IVisioBatch batch, int pageIndex, string shapeName, int flipType);

    /// <summary>
    /// Set TextFrame properties of a shape (margins, word wrap, auto size).
    /// Margins are in points. Pass null to leave a property unchanged.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="marginLeft">Left margin in points (null = don't change)</param>
    /// <param name="marginRight">Right margin in points (null = don't change)</param>
    /// <param name="marginTop">Top margin in points (null = don't change)</param>
    /// <param name="marginBottom">Bottom margin in points (null = don't change)</param>
    /// <param name="wordWrap">Enable/disable word wrap (null = don't change)</param>
    /// <param name="autoSize">0=None, 1=ShapeToFitText, 2=TextToFitShape (null = don't change)</param>
    [ServiceAction("set-text-frame")]
    OperationResult SetTextFrame(IVisioBatch batch, int pageIndex, string shapeName, float? marginLeft, float? marginRight, float? marginTop, float? marginBottom, bool? wordWrap, int? autoSize);

    /// <summary>
    /// Apply a two-color gradient fill to a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="color1">First gradient color as hex (#RRGGBB)</param>
    /// <param name="color2">Second gradient color as hex (#RRGGBB)</param>
    /// <param name="gradientStyle">1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown, 5=FromCorner, 6=FromCenter</param>
    [ServiceAction("set-gradient-fill")]
    OperationResult SetGradientFill(IVisioBatch batch, int pageIndex, string shapeName, string color1, string color2, int gradientStyle);

    /// <summary>
    /// Set glow effect on a shape. Use radius=0 to remove glow.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="radius">Glow radius in points (0 = remove glow)</param>
    /// <param name="colorHex">Glow color as hex (#RRGGBB)</param>
    [ServiceAction("set-glow")]
    OperationResult SetGlow(IVisioBatch batch, int pageIndex, string shapeName, float radius, string colorHex);

    /// <summary>
    /// Set reflection effect on a shape. Use reflectionType=0 to remove reflection.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="reflectionType">0=None, 1-9=msoReflectionType1 through msoReflectionType9</param>
    [ServiceAction("set-reflection")]
    OperationResult SetReflection(IVisioBatch batch, int pageIndex, string shapeName, int reflectionType);

    /// <summary>
    /// Set the opacity (transparency) of a shape's fill.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="opacity">Opacity value from 0.0 (fully transparent) to 1.0 (fully opaque)</param>
    [ServiceAction("set-opacity")]
    OperationResult SetOpacity(IVisioBatch batch, int pageIndex, string shapeName, float opacity);

    /// <summary>
    /// Read the fill properties of a shape: fill type, color (if solid), and transparency.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    [ServiceAction("read-fill")]
    OperationResult ReadFill(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Read the line/border properties of a shape: visible, color, weight.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    [ServiceAction("read-line")]
    OperationResult ReadLine(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Find all shapes on a page whose <c>Shape.Type</c> matches a given Visio shape type.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeType">Visio VisShapeTypes integer: 1=Page, 2=Group, 3=Shape, 4=ForeignObject (images and OLE), 5=Guide, 6=Document. An ordinary drawn or dropped shape is 3.</param>
    [ServiceAction("find-by-type")]
    OperationResult FindByType(IVisioBatch batch, int pageIndex, int shapeType);

    /// <summary>
    /// Copy all formatting from one shape to another using Format Painter (PickUp/Apply).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="sourceShapeName">Name of the shape to copy formatting from</param>
    /// <param name="targetShapeName">Name of the shape to apply formatting to</param>
    [ServiceAction("copy-formatting")]
    OperationResult CopyFormatting(IVisioBatch batch, int pageIndex, string sourceShapeName, string targetShapeName);

    /// <summary>
    /// Scale a shape by width and height factors relative to its current size.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="scaleX">Width scale factor (e.g. 1.5 = 150%)</param>
    /// <param name="scaleY">Height scale factor (e.g. 1.5 = 150%)</param>
    [ServiceAction("scale")]
    OperationResult Scale(IVisioBatch batch, int pageIndex, string shapeName, float scaleX, float scaleY);

    /// <summary>
    /// Lock or unlock the aspect ratio of a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="locked">True to lock aspect ratio, false to unlock</param>
    [ServiceAction("lock-aspect-ratio")]
    OperationResult SetLockAspectRatio(IVisioBatch batch, int pageIndex, string shapeName, bool locked);

    /// <summary>
    /// Set soft edge effect on a shape. Use radius=0 to remove soft edge.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="radius">Soft edge radius in points (0 = remove soft edge)</param>
    [ServiceAction("set-soft-edge")]
    OperationResult SetSoftEdge(IVisioBatch batch, int pageIndex, string shapeName, float radius);

    /// <summary>
    /// Read the shadow properties of a shape: visible, offsetX, offsetY, blur, color.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    [ServiceAction("read-shadow")]
    OperationResult ReadShadow(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Add a decorative text effect (WordArt) to a page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="presetEffect">MsoPresetTextEffect integer (0-based preset index)</param>
    /// <param name="text">Text content</param>
    /// <param name="fontName">Font name (e.g. "Arial")</param>
    /// <param name="fontSize">Font size in points</param>
    /// <param name="left">Position from left in points</param>
    /// <param name="top">Position from top in points</param>
    [ServiceAction("add-text-effect")]
    OperationResult AddTextEffect(IVisioBatch batch, int pageIndex, int presetEffect, string text, string fontName, float fontSize, float left, float top);

    /// <summary>
    /// Set 3D rotation and bevel effects on a shape. Only non-null values are changed.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the shape</param>
    /// <param name="rotationX">X-axis rotation in degrees (null = don't change)</param>
    /// <param name="rotationY">Y-axis rotation in degrees (null = don't change)</param>
    /// <param name="rotationZ">Z-axis rotation in degrees (null = don't change)</param>
    /// <param name="bevelType">Bevel top type: 0=None, 1=Circle, 2=RelaxedInset, 3=Cross, 4=Angle, etc. (null = don't change)</param>
    /// <param name="bevelDepth">Bevel top depth in points (null = don't change)</param>
    [ServiceAction("set-3d")]
    OperationResult Set3D(IVisioBatch batch, int pageIndex, string shapeName, float? rotationX, float? rotationY, float? rotationZ, int? bevelType, float? bevelDepth);
}
