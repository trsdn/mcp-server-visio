using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Shape;

public class ShapeCommands : IShapeCommands
{
    private const int VisSectionProp = 243;

    /// <summary>
    /// Fill, line, shadow and text-block cells copied by <c>copy-formatting</c>. Geometry,
    /// position and text content are deliberately excluded - copying formatting should not move
    /// or resize the target.
    /// </summary>
    private static readonly string[] FormattingCellNames =
    [
        "FillForegnd",
        "FillBkgnd",
        "FillPattern",
        "FillForegndTrans",
        "LineColor",
        "LinePattern",
        "LineWeight",
        "LineColorTrans",
        "Rounding",
        "ShdwPattern",
        "ShdwForegnd",
        "ShdwOffsetX",
        "ShdwOffsetY",
        "LeftMargin",
        "RightMargin",
        "TopMargin",
        "BottomMargin"
    ];
    private const int VisTagDefault = 0;
    private const int VisDeselect = 1;
    private const int VisSelect = 2;
    private const string StartShapeNameProperty = "VisioMcpStartShapeName";
    private const string EndShapeNameProperty = "VisioMcpEndShapeName";

    public ShapeListResult List(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                int count = Convert.ToInt32(shapes.Count);

                var result = new ShapeListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        result.Shapes.Add(ReadVisioShapeInfo(shape));
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeDetailResult Read(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                return new ShapeDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    Shape = ReadVisioShapeInfo(shape)
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeListResult ListGroups(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                int count = Convert.ToInt32(shapes.Count);

                var result = new ShapeListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        if (IsGroupShape(shape))
                        {
                            result.Shapes.Add(ReadVisioShapeInfo(shape, includeGroupItems: false));
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeDetailResult ReadGroup(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (!IsGroupShape(shape))
                {
                    throw new InvalidOperationException($"Shape '{shapeName}' is not a group.");
                }

                return new ShapeDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    Shape = ReadVisioShapeInfo(shape, includeGroupItems: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeSelectionResult ListSelection(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            dynamic? selection = null;
            try
            {
                EnsureWindowPage(window, page);
                selection = window.Selection;

                return new ShapeSelectionResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Shapes = ReadSelectionShapeInfos(selection)
                };
            }
            finally
            {
                if (selection != null) ComUtilities.Release(ref selection!);
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SelectShapes(IVisioBatch batch, int pageIndex, string shapeNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            try
            {
                string[] names = ParseShapeNames(shapeNames);
                EnsureWindowPage(window, page);
                window.DeselectAll();
                SelectPageShapes(page, window, names, VisSelect);

                return new OperationResult
                {
                    Success = true,
                    Action = "select-shapes",
                    Message = $"Selected {names.Length} shapes on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddToSelection(IVisioBatch batch, int pageIndex, string shapeNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            try
            {
                string[] names = ParseShapeNames(shapeNames);
                EnsureWindowPage(window, page);
                SelectPageShapes(page, window, names, VisSelect);

                return new OperationResult
                {
                    Success = true,
                    Action = "add-to-selection",
                    Message = $"Added {names.Length} shapes to selection on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult RemoveFromSelection(IVisioBatch batch, int pageIndex, string shapeNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            try
            {
                string[] names = ParseShapeNames(shapeNames);
                EnsureWindowPage(window, page);
                SelectPageShapes(page, window, names, VisDeselect);

                return new OperationResult
                {
                    Success = true,
                    Action = "remove-from-selection",
                    Message = $"Removed {names.Length} shapes from selection on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult ClearSelection(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            try
            {
                EnsureWindowPage(window, page);
                window.DeselectAll();

                return new OperationResult
                {
                    Success = true,
                    Action = "clear-selection",
                    Message = $"Cleared selection on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapePropertyListResult ListProperties(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                return new ShapePropertyListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shapeName,
                    Properties = ReadShapePropertyRows((object)shape)
                        .Select(row => new ShapePropertyInfo
                        {
                            PropertyName = row.PropertyName,
                            PropertyValue = row.PropertyValue
                        })
                        .ToList()
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapePropertyResult GetProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var property = FindShapePropertyRow((object)shape, propertyName)
                    ?? throw new InvalidOperationException($"Shape property '{propertyName}' was not found on shape '{shapeName}'.");

                return new ShapePropertyResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shapeName,
                    Property = new ShapePropertyInfo
                    {
                        PropertyName = property.PropertyName,
                        PropertyValue = property.PropertyValue
                    }
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName, string? propertyValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic? labelCell = null;
            dynamic? typeCell = null;
            dynamic? valueCell = null;
            try
            {
                string rowName = ResolveTargetShapePropertyRowName((object)shape, propertyName);
                EnsureShapeDataRow(shape, rowName);

                labelCell = shape.CellsU[$"Prop.{rowName}.Label"];
                typeCell = shape.CellsU[$"Prop.{rowName}.Type"];
                valueCell = shape.CellsU[$"Prop.{rowName}.Value"];

                labelCell.FormulaU = QuoteShapeDataValue(propertyName);
                typeCell.FormulaU = "0";
                valueCell.FormulaU = QuoteShapeDataValue(propertyValue ?? string.Empty);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-property",
                    Message = $"Set shape property '{propertyName}' on '{shapeName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (valueCell != null) ComUtilities.Release(ref valueCell!);
                if (typeCell != null) ComUtilities.Release(ref typeCell!);
                if (labelCell != null) ComUtilities.Release(ref labelCell!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult DeleteProperty(IVisioBatch batch, int pageIndex, string shapeName, string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var property = FindShapePropertyRow((object)shape, propertyName)
                    ?? throw new InvalidOperationException($"Shape property '{propertyName}' was not found on shape '{shapeName}'.");

                shape.DeleteRow(VisSectionProp, property.RowIndex);

                return new OperationResult
                {
                    Success = true,
                    Action = "delete-property",
                    Message = $"Deleted shape property '{property.PropertyName}' from '{shapeName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ConnectorListResult ListConnectors(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                int count = Convert.ToInt32(shapes.Count);
                var result = new ConnectorListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        if (IsConnectorShape(shape))
                        {
                            result.Connectors.Add(ReadConnectorInfo(shape));
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ConnectorDetailResult ReadConnector(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (!IsConnectorShape(shape))
                {
                    throw new InvalidOperationException($"Shape '{shapeName}' on page {pageIndex} is not a connector.");
                }

                return new ConnectorDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Connector = ReadConnectorInfo(shape)
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeConnectionListResult ListConnections(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                return new ShapeConnectionListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shapeName,
                    Connections = ReadShapeConnections(page, shape)
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ConnectorDetailResult DisconnectConnector(IVisioBatch batch, int pageIndex, string shapeName, string connectorEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        string normalizedConnectorEnd = NormalizeConnectorEnd(connectorEnd);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic connector = page.Shapes.Item(shapeName);
            dynamic? endpointX = null;
            dynamic? endpointY = null;
            try
            {
                if (!IsConnectorShape(connector))
                {
                    throw new InvalidOperationException($"Shape '{shapeName}' on page {pageIndex} is not a connector.");
                }

                string endpointPrefix = normalizedConnectorEnd == "start" ? "Begin" : "End";
                endpointX = connector.CellsU[$"{endpointPrefix}X"];
                endpointY = connector.CellsU[$"{endpointPrefix}Y"];

                endpointX.ResultIU = Convert.ToDouble(endpointX.ResultIU);
                endpointY.ResultIU = Convert.ToDouble(endpointY.ResultIU);
                SetConnectorEndpointMetadata(connector, normalizedConnectorEnd, null);

                return new ConnectorDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Connector = ReadConnectorInfo(connector)
                };
            }
            finally
            {
                if (endpointY != null) ComUtilities.Release(ref endpointY!);
                if (endpointX != null) ComUtilities.Release(ref endpointX!);
                ComUtilities.Release(ref connector!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ConnectorDetailResult ReconnectConnector(IVisioBatch batch, int pageIndex, string shapeName, string connectorEnd, string targetShapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetShapeName);
        string normalizedConnectorEnd = NormalizeConnectorEnd(connectorEnd);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? connector = null;
            dynamic? targetShape = null;
            dynamic? endpointX = null;
            try
            {
                connector = page.Shapes.Item(shapeName);
                if (!IsConnectorShape(connector))
                {
                    throw new InvalidOperationException($"Shape '{shapeName}' on page {pageIndex} is not a connector.");
                }

                targetShape = page.Shapes.Item(targetShapeName);

                float targetX = ReadCellResultIU(targetShape, "PinX");
                float targetY = ReadCellResultIU(targetShape, "PinY");
                float otherX;
                float otherY;
                double xPercent;
                double yPercent;

                if (normalizedConnectorEnd == "start")
                {
                    otherX = ReadCellResultIU(connector, "EndX");
                    otherY = ReadCellResultIU(connector, "EndY");
                    (xPercent, yPercent) = GetGluePercentages(targetX, targetY, otherX, otherY, true);
                    endpointX = connector.CellsU["BeginX"];
                }
                else
                {
                    otherX = ReadCellResultIU(connector, "BeginX");
                    otherY = ReadCellResultIU(connector, "BeginY");
                    (xPercent, yPercent) = GetGluePercentages(otherX, otherY, targetX, targetY, false);
                    endpointX = connector.CellsU["EndX"];
                }

                endpointX.GlueToPos(targetShape, xPercent, yPercent);
                SetConnectorEndpointMetadata(connector, normalizedConnectorEnd, targetShapeName);

                return new ConnectorDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Connector = ReadConnectorInfo(connector)
                };
            }
            finally
            {
                if (endpointX != null) ComUtilities.Release(ref endpointX!);
                if (targetShape != null) ComUtilities.Release(ref targetShape!);
                if (connector != null) ComUtilities.Release(ref connector!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddTextbox(IVisioBatch batch, int pageIndex, float left, float top, float width, float height, string text)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.DrawRectangle(ToPageX(left), ToPageY(top), ToPageX(left + width), ToPageY(top + height));
            try
            {
                shape.Text = text;
                string name = shape.Name?.ToString() ?? "";
                return new OperationResult
                {
                    Success = true,
                    Action = "add-textbox",
                    Message = $"Added textbox '{name}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddShape(IVisioBatch batch, int pageIndex, int autoShapeType, float left, float top, float width, float height)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = AddVisioShape(page, autoShapeType, left, top, width, height);
            try
            {
                string name = shape.Name?.ToString() ?? "";
                return new OperationResult
                {
                    Success = true,
                    Action = "add-shape",
                    Message = $"Added shape '{name}' (type {autoShapeType}) on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult MoveResize(IVisioBatch batch, int pageIndex, string shapeName, float? left, float? top, float? width, float? height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                TrySetCell(shape, "PinX", left.HasValue ? ToPageCoordinate(left.Value) : (float?)null);
                TrySetCell(shape, "PinY", top.HasValue ? ToPageCoordinate(top.Value) : (float?)null);
                TrySetCell(shape, "Width", width.HasValue ? ToPageCoordinate(width.Value) : (float?)null);
                TrySetCell(shape, "Height", height.HasValue ? ToPageCoordinate(height.Value) : (float?)null);

                return new OperationResult
                {
                    Success = true,
                    Action = "move-resize",
                    Message = $"Updated position/size of shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                shape.Delete();
                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted shape '{shapeName}' from page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult ZOrder(IVisioBatch batch, int pageIndex, string shapeName, int zOrderCmd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                string actionName = zOrderCmd switch
                {
                    1 => ExecuteZOrderCommand(shape, zOrderCmd),
                    2 => ExecuteZOrderCommand(shape, zOrderCmd),
                    3 => ExecuteZOrderCommand(shape, zOrderCmd),
                    4 => ExecuteZOrderCommand(shape, zOrderCmd),
                    _ => throw new ArgumentOutOfRangeException(nameof(zOrderCmd), "zOrderCmd must be 1=BringToFront, 2=SendToBack, 3=BringForward, or 4=SendBackward.")
                };

                return new OperationResult
                {
                    Success = true,
                    Action = "z-order",
                    Message = $"Changed z-order of shape '{shapeName}' on page {pageIndex} ({actionName})",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetFill(IVisioBatch batch, int pageIndex, string shapeName, string colorHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorHex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (colorHex.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    // FillPattern 0 = no fill. The foreground colour is left untouched so it is
                    // restored if the pattern is turned back on.
                    ShapeSheetHelpers.SetFormula(shape, "FillPattern", "0");
                }
                else
                {
                    ShapeSheetHelpers.SetFormula(shape, "FillForegnd", ShapeSheetHelpers.ToRgbFormula(colorHex, nameof(colorHex)));
                    ShapeSheetHelpers.SetFormula(shape, "FillPattern", "1");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-fill",
                    Message = $"Set fill of shape '{shapeName}' to '{colorHex}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetLine(IVisioBatch batch, int pageIndex, string shapeName, string colorHex, float lineWidth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorHex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (colorHex.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    // LinePattern 0 = no line.
                    ShapeSheetHelpers.SetFormula(shape, "LinePattern", "0");
                }
                else
                {
                    ShapeSheetHelpers.SetFormula(shape, "LineColor", ShapeSheetHelpers.ToRgbFormula(colorHex, nameof(colorHex)));
                    ShapeSheetHelpers.SetFormula(shape, "LinePattern", "1");

                    if (lineWidth > 0)
                    {
                        // LineWeight is a distance cell; state the unit so the value is not
                        // reinterpreted in the document's default units.
                        ShapeSheetHelpers.SetFormula(shape, "LineWeight", ShapeSheetHelpers.FormatInvariant(lineWidth) + " pt");
                    }
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-line",
                    Message = $"Set line of shape '{shapeName}' to '{colorHex}' (weight {lineWidth}pt) on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetRotation(IVisioBatch batch, int pageIndex, string shapeName, float degrees)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio rotates about PinX/PinY and measures anticlockwise; PowerPoint's Rotation
                // property was clockwise. The sign is negated so a positive value keeps the
                // clockwise sense callers already expect.
                ShapeSheetHelpers.SetFormula(shape, "Angle", ShapeSheetHelpers.FormatInvariant(-degrees) + " deg");

                return new OperationResult
                {
                    Success = true,
                    Action = "set-rotation",
                    Message = $"Rotated shape '{shapeName}' to {degrees}° on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Group(IVisioBatch batch, int pageIndex, string shapeNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            dynamic? selection = null;
            try
            {
                string[] names = shapeNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                window.DeselectAll();

                foreach (string name in names)
                {
                    dynamic? shape = null;
                    try
                    {
                        shape = page.Shapes.Item(name);
                        window.Select(shape, 2);
                    }
                    finally
                    {
                        if (shape != null) ComUtilities.Release(ref shape!);
                    }
                }

                dynamic? grouped = null;
                try
                {
                    selection = window.Selection;
                    grouped = selection.Group();
                    string groupName = grouped.Name?.ToString() ?? "";
                    return new OperationResult
                    {
                        Success = true,
                        Action = "group",
                        Message = $"Grouped {names.Length} shapes into '{groupName}' on page {pageIndex}",
                        FilePath = ctx.DocumentPath
                    };
                }
                finally
                {
                    if (grouped != null) ComUtilities.Release(ref grouped!);
                }
            }
            finally
            {
                if (selection != null) ComUtilities.Release(ref selection!);
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Ungroup(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (!IsGroupShape(shape))
                {
                    throw new InvalidOperationException($"Shape '{shapeName}' is not a group.");
                }

                shape.Ungroup();
                return new OperationResult
                {
                    Success = true,
                    Action = "ungroup",
                    Message = $"Ungrouped shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetAltText(IVisioBatch batch, int pageIndex, string shapeName, string altText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio has no AlternativeText property. The Comment cell is what surfaces as a
                // shape's screen tip and is what accessibility tooling reads, so it is the closest
                // equivalent. Its content is a formula, so the text must be quoted and escaped.
                ShapeSheetHelpers.SetFormula(shape, "Comment", ShapeSheetHelpers.ToStringFormula(altText));

                return new OperationResult
                {
                    Success = true,
                    Action = "set-alt-text",
                    Message = $"Set alt text of shape '{shapeName}' to '{altText}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult CopyToSlide(IVisioBatch batch, int pageIndex, string shapeName, int targetSlideIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic sourcePage = GetPage(ctx, pageIndex);
            dynamic shape = sourcePage.Shapes.Item(shapeName);
            dynamic? targetPage = null;
            dynamic? dropped = null;
            try
            {
                targetPage = GetPage(ctx, targetSlideIndex);

                // Page.Drop copies the shape directly between pages. The clipboard round trip the
                // PowerPoint version used (Copy + Shapes.Paste) is both slower and vulnerable to
                // anything else touching the clipboard mid-operation.
                double pinX = ShapeSheetHelpers.TryGetResult(shape, "PinX") ?? 0d;
                double pinY = ShapeSheetHelpers.TryGetResult(shape, "PinY") ?? 0d;

                dropped = targetPage.Drop(shape, pinX, pinY);
                string newName = dropped.NameU?.ToString() ?? string.Empty;

                return new OperationResult
                {
                    Success = true,
                    Action = "copy-to-slide",
                    Message = $"Copied shape '{shapeName}' from page {pageIndex} to page {targetSlideIndex}" +
                              (string.IsNullOrEmpty(newName) ? "" : $" as '{newName}'"),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (dropped != null) ComUtilities.Release(ref dropped!);
                if (targetPage != null) ComUtilities.Release(ref targetPage!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref sourcePage!);
            }
        });
    }

    public OperationResult SetShadow(IVisioBatch batch, int pageIndex, string shapeName, bool visible, float offsetX, float offsetY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (visible)
                {
                    // ShdwPattern 0 = none, 1 = solid. Offsets are distance cells, so the unit is
                    // stated explicitly rather than left to the document default.
                    ShapeSheetHelpers.SetFormula(shape, "ShdwPattern", "1");
                    ShapeSheetHelpers.SetFormula(shape, "ShdwOffsetX", ShapeSheetHelpers.FormatInvariant(offsetX) + " pt");
                    ShapeSheetHelpers.SetFormula(shape, "ShdwOffsetY", ShapeSheetHelpers.FormatInvariant(offsetY) + " pt");
                }
                else
                {
                    ShapeSheetHelpers.SetFormula(shape, "ShdwPattern", "0");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-shadow",
                    Message = visible
                        ? $"Set shadow on shape '{shapeName}' (offset {ShapeSheetHelpers.FormatInvariant(offsetX)},{ShapeSheetHelpers.FormatInvariant(offsetY)} pt)"
                        : $"Removed shadow from shape '{shapeName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddConnector(IVisioBatch batch, int pageIndex, int connectorType, string startShapeName, string endShapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startShapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endShapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? startShape = null;
            dynamic? endShape = null;
            dynamic? connector = null;
            dynamic? beginX = null;
            dynamic? endX = null;
            try
            {
                startShape = page.Shapes.Item(startShapeName);
                endShape = page.Shapes.Item(endShapeName);

                float sx = ReadCellResultIU(startShape, "PinX");
                float sy = ReadCellResultIU(startShape, "PinY");
                float ex = ReadCellResultIU(endShape, "PinX");
                float ey = ReadCellResultIU(endShape, "PinY");

                connector = page.DrawLine(sx, sy, ex, ey);

                beginX = connector.CellsU["BeginX"];
                endX = connector.CellsU["EndX"];
                (double startXPercent, double startYPercent) = GetGluePercentages(sx, sy, ex, ey, true);
                (double endXPercent, double endYPercent) = GetGluePercentages(sx, sy, ex, ey, false);

                beginX.GlueToPos(startShape, startXPercent, startYPercent);
                endX.GlueToPos(endShape, endXPercent, endYPercent);
                WriteConnectorMetadata(connector, StartShapeNameProperty, startShapeName);
                WriteConnectorMetadata(connector, EndShapeNameProperty, endShapeName);

                string name = connector.Name?.ToString() ?? "";
                return new OperationResult
                {
                    Success = true,
                    Action = "add-connector",
                    Message = $"Added connector '{name}' between '{startShapeName}' and '{endShapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (endX != null) ComUtilities.Release(ref endX!);
                if (beginX != null) ComUtilities.Release(ref beginX!);
                if (connector != null) ComUtilities.Release(ref connector!);
                if (endShape != null) ComUtilities.Release(ref endShape!);
                if (startShape != null) ComUtilities.Release(ref startShape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult MergeShapes(IVisioBatch batch, int pageIndex, string shapeNames, int mergeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);
        if (mergeType is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(mergeType), "mergeType must be between 1 and 5.");
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = ctx.Application.ActiveWindow;
            dynamic? selection = null;
            try
            {
                string[] names = shapeNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (names.Length < 2)
                {
                    throw new ArgumentException("At least 2 shape names are required for merge.");
                }

                HashSet<int> beforeShapeIds = ReadShapeIds(page);
                window.DeselectAll();

                foreach (string name in names)
                {
                    dynamic? shape = null;
                    try
                    {
                        shape = page.Shapes.Item(name);
                        window.Select(shape, 2);
                    }
                    finally
                    {
                        if (shape != null) ComUtilities.Release(ref shape!);
                    }
                }

                selection = window.Selection;
                ExecuteMergeOperation(selection, mergeType);

                string mergeName = GetMergeOperationName(mergeType);
                List<string> createdShapeNames = ReadCreatedShapeNames(page, beforeShapeIds);
                string messageSuffix = createdShapeNames.Count switch
                {
                    0 => $"using {mergeName} on page {pageIndex}",
                    1 => $"using {mergeName} into '{createdShapeNames[0]}' on page {pageIndex}",
                    _ => $"using {mergeName} into {createdShapeNames.Count} shapes ({string.Join(", ", createdShapeNames)}) on page {pageIndex}"
                };

                return new OperationResult
                {
                    Success = true,
                    Action = "merge",
                    Message = $"Merged {names.Length} shapes {messageSuffix}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (selection != null) ComUtilities.Release(ref selection!);
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Duplicate(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic? duplicate = null;
            try
            {
                duplicate = shape.Duplicate();
                string newName = TryGetDuplicateShapeName(duplicate);

                return new OperationResult
                {
                    Success = true,
                    Action = "duplicate",
                    Message = $"Duplicated shape '{shapeName}' as '{newName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (duplicate != null) ComUtilities.Release(ref duplicate!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Flip(IVisioBatch batch, int pageIndex, string shapeName, int flipType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (flipType is not (0 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(flipType), flipType, "flipType must be 0 (horizontal) or 1 (vertical).");
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio holds flip state in the FlipX / FlipY ShapeSheet cells rather than
                // offering a Flip() method, so this toggles the current value.
                string cellName = flipType == 0 ? "FlipX" : "FlipY";
                bool current = (ShapeSheetHelpers.TryGetResult(shape, cellName) ?? 0d) != 0d;
                ShapeSheetHelpers.SetFormula(shape, cellName, current ? "FALSE" : "TRUE");

                string dir = flipType == 0 ? "horizontally" : "vertically";
                return new OperationResult
                {
                    Success = true,
                    Action = "flip",
                    Message = $"Flipped shape '{shapeName}' {dir} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetTextFrame(IVisioBatch batch, int pageIndex, string shapeName, float? marginLeft, float? marginRight, float? marginTop, float? marginBottom, bool? wordWrap, int? autoSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var changes = new List<string>();

                // Visio has no TextFrame object; the text block margins are cells.
                if (marginLeft.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "LeftMargin", ShapeSheetHelpers.FormatInvariant(marginLeft.Value) + " pt");
                    changes.Add($"LeftMargin={ShapeSheetHelpers.FormatInvariant(marginLeft.Value)}pt");
                }
                if (marginRight.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "RightMargin", ShapeSheetHelpers.FormatInvariant(marginRight.Value) + " pt");
                    changes.Add($"RightMargin={ShapeSheetHelpers.FormatInvariant(marginRight.Value)}pt");
                }
                if (marginTop.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "TopMargin", ShapeSheetHelpers.FormatInvariant(marginTop.Value) + " pt");
                    changes.Add($"TopMargin={ShapeSheetHelpers.FormatInvariant(marginTop.Value)}pt");
                }
                if (marginBottom.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "BottomMargin", ShapeSheetHelpers.FormatInvariant(marginBottom.Value) + " pt");
                    changes.Add($"BottomMargin={ShapeSheetHelpers.FormatInvariant(marginBottom.Value)}pt");
                }

                // wordWrap and autoSize have no single-cell Visio equivalent - text wrapping is a
                // consequence of TxtWidth and the paragraph settings rather than a switch. They
                // are reported as unsupported instead of being silently ignored, which is what
                // the PowerPoint implementation's TextFrame.WordWrap would have become.
                var unsupported = new List<string>();
                if (wordWrap.HasValue) unsupported.Add("word_wrap");
                if (autoSize.HasValue) unsupported.Add("auto_size");

                string message = changes.Count > 0
                    ? $"Updated text frame margins of shape '{shapeName}' on page {pageIndex}: {string.Join(", ", changes)}"
                    : $"No text frame margins changed on shape '{shapeName}'";

                if (unsupported.Count > 0)
                {
                    message += $". Ignored (no Visio equivalent): {string.Join(", ", unsupported)}";
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-text-frame",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult ReadFill(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                double pattern = ShapeSheetHelpers.TryGetResult(shape, "FillPattern") ?? 0d;
                string colorFormula = ShapeSheetHelpers.TryGetFormula(shape, "FillForegnd") ?? string.Empty;
                double transparency = ShapeSheetHelpers.TryGetResult(shape, "FillForegndTrans") ?? 0d;

                // FillPattern 0 = no fill, 1 = solid, anything higher is one of Visio's
                // hatch/gradient patterns.
                string patternName = pattern switch
                {
                    0d => "None",
                    1d => "Solid",
                    _ => $"Pattern {ShapeSheetHelpers.FormatInvariant(pattern)}"
                };

                string message = pattern == 0d
                    ? "Fill: None"
                    : $"Fill: {patternName}, Color: {colorFormula}, Transparency: {ShapeSheetHelpers.FormatInvariant(transparency)}";

                return new OperationResult
                {
                    Success = true,
                    Action = "read-fill",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult ReadLine(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                double pattern = ShapeSheetHelpers.TryGetResult(shape, "LinePattern") ?? 0d;
                bool visible = pattern != 0d;

                string colorFormula = ShapeSheetHelpers.TryGetFormula(shape, "LineColor") ?? string.Empty;

                // LineWeight's ResultIU is in inches; Visio's internal drawing unit. Points are
                // what the setter accepts, so report the same unit.
                double weightPoints = (ShapeSheetHelpers.TryGetResult(shape, "LineWeight") ?? 0d) * 72d;

                string message = visible
                    ? $"Visible: true, Color: {colorFormula}, Weight: {ShapeSheetHelpers.FormatInvariant(weightPoints)}pt"
                    : "Visible: false";

                return new OperationResult
                {
                    Success = true,
                    Action = "read-line",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }


    private static void EnsureWindowPage(dynamic window, dynamic page)
    {
        dynamic? currentPage = null;
        try
        {
            currentPage = window.Page;
            if (currentPage != null && Convert.ToInt32(currentPage.ID) == Convert.ToInt32(page.ID))
            {
                return;
            }

            window.Page = page;
        }
        finally
        {
            if (currentPage != null) ComUtilities.Release(ref currentPage!);
        }
    }

    private static string[] ParseShapeNames(string shapeNames)
    {
        string[] names = shapeNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
        {
            throw new ArgumentException("At least 1 shape name is required.", nameof(shapeNames));
        }

        return names;
    }

    private static void SelectPageShapes(dynamic page, dynamic window, IEnumerable<string> shapeNames, int selectAction)
    {
        foreach (string name in shapeNames)
        {
            dynamic? shape = null;
            try
            {
                shape = page.Shapes.Item(name);
                window.Select(shape, selectAction);
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }
    }

    private static List<ShapeInfo> ReadSelectionShapeInfos(dynamic selection)
    {
        int count = Convert.ToInt32(selection.Count);
        var shapes = new List<ShapeInfo>(count);
        for (int i = 1; i <= count; i++)
        {
            dynamic? shape = null;
            try
            {
                shape = selection.Item(i);
                shapes.Add(ReadVisioShapeInfo(shape));
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }

        return shapes;
    }

    private static void ExecuteMergeOperation(dynamic selection, int mergeType)
    {
        switch (mergeType)
        {
            case 1:
                selection.Union();
                break;
            case 2:
                selection.Combine();
                break;
            case 3:
                selection.Fragment();
                break;
            case 4:
                selection.Intersect();
                break;
            case 5:
                selection.Subtract();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mergeType));
        }
    }

    private static string GetMergeOperationName(int mergeType)
    {
        return mergeType switch
        {
            1 => "union",
            2 => "combine",
            3 => "fragment",
            4 => "intersect",
            5 => "subtract",
            _ => $"type({mergeType})"
        };
    }

    private static HashSet<int> ReadShapeIds(dynamic page)
    {
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            int count = Convert.ToInt32(shapes.Count);
            var ids = new HashSet<int>();
            for (int i = 1; i <= count; i++)
            {
                dynamic? shape = null;
                try
                {
                    shape = shapes.Item(i);
                    ids.Add(Convert.ToInt32(shape.ID));
                }
                finally
                {
                    if (shape != null) ComUtilities.Release(ref shape!);
                }
            }

            return ids;
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }

    private static List<string> ReadCreatedShapeNames(dynamic page, HashSet<int> beforeShapeIds)
    {
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            int count = Convert.ToInt32(shapes.Count);
            var createdShapeNames = new List<string>();
            for (int i = 1; i <= count; i++)
            {
                dynamic? shape = null;
                try
                {
                    shape = shapes.Item(i);
                    int shapeId = Convert.ToInt32(shape.ID);
                    if (!beforeShapeIds.Contains(shapeId))
                    {
                        createdShapeNames.Add(shape.Name?.ToString() ?? $"Shape{shapeId}");
                    }
                }
                finally
                {
                    if (shape != null) ComUtilities.Release(ref shape!);
                }
            }

            return createdShapeNames;
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }

    private static dynamic AddVisioShape(dynamic page, int autoShapeType, float left, float top, float width, float height)
    {
        float x1 = ToPageX(left);
        float y1 = ToPageY(top);
        float x2 = ToPageX(left + width);
        float y2 = ToPageY(top + height);

        return autoShapeType switch
        {
            9 => page.DrawOval(x1, y1, x2, y2),
            _ => page.DrawRectangle(x1, y1, x2, y2)
        };
    }

    private static string ExecuteZOrderCommand(dynamic shape, int zOrderCmd)
    {
        return zOrderCmd switch
        {
            1 => InvokeBringToFront(shape),
            2 => InvokeSendToBack(shape),
            3 => InvokeBringForward(shape),
            4 => InvokeSendBackward(shape),
            _ => throw new ArgumentOutOfRangeException(nameof(zOrderCmd))
        };
    }

    private static string InvokeBringToFront(dynamic shape)
    {
        shape.BringToFront();
        return "bring to front";
    }

    private static string InvokeSendToBack(dynamic shape)
    {
        shape.SendToBack();
        return "send to back";
    }

    private static string InvokeBringForward(dynamic shape)
    {
        shape.BringForward();
        return "bring forward";
    }

    private static string InvokeSendBackward(dynamic shape)
    {
        shape.SendBackward();
        return "send backward";
    }

    private static string TryGetDuplicateShapeName(dynamic duplicate)
    {
        try
        {
            return duplicate.Name?.ToString() ?? string.Empty;
        }
        catch
        {
            dynamic? firstItem = null;
            try
            {
                firstItem = duplicate.Item(1);
                return firstItem.Name?.ToString() ?? string.Empty;
            }
            finally
            {
                if (firstItem != null)
                {
                    ComUtilities.Release(ref firstItem!);
                }
            }
        }
    }

    private static ShapeInfo ReadVisioShapeInfo(dynamic shape, bool includeGroupItems = false)
    {
        bool isGroup = IsGroupShape(shape);
        var info = new ShapeInfo
        {
            Name = shape.Name?.ToString() ?? string.Empty,
            ShapeType = isGroup ? "Group" : "Shape",
            HasTable = false,
            HasChart = false,
            IsPlaceholder = false,
            IsGroup = isGroup,
            ZOrderPosition = 0
        };

        try { info.ShapeId = Convert.ToInt32(shape.ID); } catch { }
        try { info.Left = Convert.ToSingle(shape.CellsU["PinX"].ResultIU) * 72f; } catch { }
        try { info.Top = Convert.ToSingle(shape.CellsU["PinY"].ResultIU) * 72f; } catch { }
        try { info.Width = Convert.ToSingle(shape.CellsU["Width"].ResultIU) * 72f; } catch { }
        try { info.Height = Convert.ToSingle(shape.CellsU["Height"].ResultIU) * 72f; } catch { }
        try { info.Text = shape.Text?.ToString(); } catch { }
        info.HasTextFrame = !string.IsNullOrEmpty(info.Text);

        try
        {
            info.ShapeType = Convert.ToBoolean(shape.OneD) ? "Connector" : "Shape";
        }
        catch
        {
        }

        if (includeGroupItems && isGroup)
        {
            info.GroupItems = ReadGroupItems(shape);
        }

        return info;
    }

    private static ConnectorInfo ReadConnectorInfo(dynamic connector)
    {
        var info = new ConnectorInfo
        {
            Name = connector.Name?.ToString() ?? string.Empty
        };

        try { info.ShapeId = Convert.ToInt32(connector.ID); } catch { }

        PopulateConnectorEndpoints(connector, info);
        return info;
    }

    private static List<ShapeConnectionInfo> ReadShapeConnections(dynamic page, dynamic shape)
    {
        if (IsConnectorShape(shape))
        {
            return ReadConnectorConnections(shape);
        }

        dynamic? connects = null;
        int targetShapeId = Convert.ToInt32(shape.ID);
        var connections = new List<ShapeConnectionInfo>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            connects = page.Connects;
            int count = Convert.ToInt32(connects.Count);

            for (int i = 1; i <= count; i++)
            {
                dynamic? connect = null;
                dynamic? fromCell = null;
                dynamic? fromSheet = null;
                dynamic? toCell = null;
                dynamic? toSheet = null;
                try
                {
                    connect = connects.Item(i);
                    fromSheet = TryGetConnectObject(connect, "FromSheet");
                    toSheet = TryGetConnectObject(connect, "ToSheet");

                    if (toSheet is null || fromSheet is null)
                    {
                        continue;
                    }

                    if (Convert.ToInt32(toSheet.ID) != targetShapeId || !IsConnectorShape(fromSheet))
                    {
                        continue;
                    }

                    fromCell = TryGetConnectObject(connect, "FromCell");
                    string fromCellName = TryGetComObjectName(fromCell) ?? string.Empty;
                    string? connectorEnd = GetConnectorEndpointSide(connect, fromCellName);
                    if (connectorEnd is null)
                    {
                        continue;
                    }

                    toCell = TryGetConnectObject(connect, "ToCell");
                    string? shapeConnectionCell = GetTargetConnectionCellName(connect, toCell);
                    ConnectorInfo connectorInfo = ReadConnectorInfo(fromSheet);
                    string? connectedShapeName = connectorEnd == "start"
                        ? connectorInfo.EndShapeName
                        : connectorInfo.StartShapeName;

                    AddShapeConnection(
                        connections,
                        seenKeys,
                        new ShapeConnectionInfo
                        {
                            ConnectorShapeId = connectorInfo.ShapeId,
                            ConnectorName = connectorInfo.Name,
                            ConnectorEnd = connectorEnd,
                            ConnectorConnectionCell = connectorEnd == "start" ? "BeginX" : "EndX",
                            ShapeConnectionCell = shapeConnectionCell,
                            ConnectedShapeName = NormalizeOptionalString(connectedShapeName)
                        });
                }
                finally
                {
                    if (toSheet != null) ComUtilities.Release(ref toSheet!);
                    if (toCell != null) ComUtilities.Release(ref toCell!);
                    if (fromSheet != null) ComUtilities.Release(ref fromSheet!);
                    if (fromCell != null) ComUtilities.Release(ref fromCell!);
                    if (connect != null) ComUtilities.Release(ref connect!);
                }
            }
        }
        finally
        {
            if (connects != null) ComUtilities.Release(ref connects!);
        }

        return connections
            .OrderBy(connection => connection.ConnectorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(connection => connection.ConnectorEnd, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ShapeConnectionInfo> ReadConnectorConnections(dynamic connector)
    {
        ConnectorInfo connectorInfo = ReadConnectorInfo(connector);
        var connections = new List<ShapeConnectionInfo>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddShapeConnection(
            connections,
            seenKeys,
            new ShapeConnectionInfo
            {
                ConnectorShapeId = connectorInfo.ShapeId,
                ConnectorName = connectorInfo.Name,
                ConnectorEnd = "start",
                ConnectorConnectionCell = "BeginX",
                ShapeConnectionCell = NormalizeOptionalString(connectorInfo.StartConnectionCell),
                ConnectedShapeName = NormalizeOptionalString(connectorInfo.StartShapeName)
            });

        AddShapeConnection(
            connections,
            seenKeys,
            new ShapeConnectionInfo
            {
                ConnectorShapeId = connectorInfo.ShapeId,
                ConnectorName = connectorInfo.Name,
                ConnectorEnd = "end",
                ConnectorConnectionCell = "EndX",
                ShapeConnectionCell = NormalizeOptionalString(connectorInfo.EndConnectionCell),
                ConnectedShapeName = NormalizeOptionalString(connectorInfo.EndShapeName)
            });

        return connections
            .OrderBy(connection => connection.ConnectorEnd, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddShapeConnection(List<ShapeConnectionInfo> connections, HashSet<string> seenKeys, ShapeConnectionInfo connection)
    {
        if (string.IsNullOrWhiteSpace(connection.ConnectedShapeName)
            && string.IsNullOrWhiteSpace(connection.ShapeConnectionCell))
        {
            return;
        }

        string key = string.Join(
            "|",
            connection.ConnectorShapeId,
            connection.ConnectorEnd ?? string.Empty,
            connection.ShapeConnectionCell ?? string.Empty,
            connection.ConnectedShapeName ?? string.Empty);

        if (!seenKeys.Add(key))
        {
            return;
        }

        connections.Add(connection);
    }

    private static bool IsConnectorShape(dynamic shape)
    {
        try
        {
            return Convert.ToBoolean(shape.OneD);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsGroupShape(dynamic shape)
    {
        dynamic? childShapes = null;
        try
        {
            childShapes = shape.Shapes;
            return Convert.ToInt32(childShapes.Count) > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (childShapes != null) ComUtilities.Release(ref childShapes!);
        }
    }

    private static List<ShapeInfo> ReadGroupItems(dynamic groupShape)
    {
        dynamic? childShapes = null;
        try
        {
            childShapes = groupShape.Shapes;
            int count = Convert.ToInt32(childShapes.Count);
            var items = new List<ShapeInfo>(count);

            for (int i = 1; i <= count; i++)
            {
                dynamic child = childShapes.Item(i);
                try
                {
                    items.Add(ReadVisioShapeInfo(child, includeGroupItems: true));
                }
                finally
                {
                    ComUtilities.Release(ref child!);
                }
            }

            return items;
        }
        finally
        {
            if (childShapes != null) ComUtilities.Release(ref childShapes!);
        }
    }

    private static void PopulateConnectorEndpoints(dynamic connector, ConnectorInfo info)
    {
        dynamic? connects = null;
        dynamic? page = null;
        dynamic? pageConnects = null;
        try
        {
            connects = connector.Connects;
            ReadConnectorEndpointsFromCollection(connects, info, expectedFromShapeId: null);

            if (!HasConnectorEndpoints(info))
            {
                page = connector.ContainingPage;
                pageConnects = page.Connects;
                ReadConnectorEndpointsFromCollection(pageConnects, info, info.ShapeId);
            }
        }
        catch
        {
        }
        finally
        {
            if (pageConnects != null) ComUtilities.Release(ref pageConnects!);
            if (page != null) ComUtilities.Release(ref page!);
            if (connects != null) ComUtilities.Release(ref connects!);
        }

        if (string.IsNullOrWhiteSpace(info.StartShapeName))
        {
            info.StartShapeName = ReadConnectorMetadata(connector, StartShapeNameProperty);
        }

        if (string.IsNullOrWhiteSpace(info.EndShapeName))
        {
            info.EndShapeName = ReadConnectorMetadata(connector, EndShapeNameProperty);
        }
    }

    private static void ReadConnectorEndpointsFromCollection(dynamic connects, ConnectorInfo info, int? expectedFromShapeId)
    {
        int count = Convert.ToInt32(connects.Count);

        for (int i = 1; i <= count; i++)
        {
            dynamic? connect = null;
            dynamic? fromCell = null;
            dynamic? fromSheet = null;
            dynamic? toCell = null;
            dynamic? toSheet = null;
            try
            {
                connect = connects.Item(i);

                if (expectedFromShapeId.HasValue)
                {
                    fromSheet = TryGetConnectObject(connect, "FromSheet");
                    if (fromSheet is null)
                    {
                        continue;
                    }

                    if (Convert.ToInt32(fromSheet.ID) != expectedFromShapeId.Value)
                    {
                        continue;
                    }
                }

                fromCell = TryGetConnectObject(connect, "FromCell");
                string fromCellName = TryGetComObjectName(fromCell) ?? string.Empty;
                string? endpointSide = GetConnectorEndpointSide(connect, fromCellName);
                if (endpointSide is null)
                {
                    continue;
                }

                toCell = TryGetConnectObject(connect, "ToCell");
                toSheet = TryGetConnectObject(connect, "ToSheet");

                string? targetShapeName = toSheet?.Name?.ToString();
                string? targetCellName = GetTargetConnectionCellName(connect, toCell);

                if (string.Equals(endpointSide, "start", StringComparison.Ordinal))
                {
                    info.StartShapeName = targetShapeName;
                    info.StartConnectionCell = targetCellName;
                }
                else
                {
                    info.EndShapeName = targetShapeName;
                    info.EndConnectionCell = targetCellName;
                }

                if (HasConnectorEndpoints(info))
                {
                    return;
                }
            }
            finally
            {
                if (toSheet != null) ComUtilities.Release(ref toSheet!);
                if (toCell != null) ComUtilities.Release(ref toCell!);
                if (fromSheet != null) ComUtilities.Release(ref fromSheet!);
                if (fromCell != null) ComUtilities.Release(ref fromCell!);
                if (connect != null) ComUtilities.Release(ref connect!);
            }
        }
    }

    private static dynamic? TryGetConnectObject(dynamic connect, string propertyName)
    {
        try
        {
            return propertyName switch
            {
                "FromCell" => connect.FromCell,
                "FromSheet" => connect.FromSheet,
                "ToCell" => connect.ToCell,
                "ToSheet" => connect.ToSheet,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetConnectorEndpointSide(dynamic connect, string fromCellName)
    {
        if (string.Equals(fromCellName, "BeginX", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fromCellName, "BeginY", StringComparison.OrdinalIgnoreCase))
        {
            return "start";
        }

        if (string.Equals(fromCellName, "EndX", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fromCellName, "EndY", StringComparison.OrdinalIgnoreCase))
        {
            return "end";
        }

        try
        {
            int fromPart = Convert.ToInt32(connect.FromPart);
            return fromPart switch
            {
                7 or 8 or 9 => "start",
                10 or 11 or 12 => "end",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetTargetConnectionCellName(dynamic connect, dynamic? toCell)
    {
        string? targetCellName = TryGetComObjectName(toCell);
        if (!string.IsNullOrWhiteSpace(targetCellName))
        {
            return targetCellName;
        }

        try
        {
            int toPart = Convert.ToInt32(connect.ToPart);
            return toPart switch
            {
                3 => "WholeShape",
                >= 100 => $"ConnectionPoint{toPart - 99}",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetComObjectName(dynamic? comObject)
    {
        if (comObject is null)
        {
            return null;
        }

        try
        {
            return comObject.NameU?.ToString();
        }
        catch
        {
        }

        try
        {
            return comObject.Name?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static bool HasConnectorEndpoints(ConnectorInfo info)
    {
        return !string.IsNullOrWhiteSpace(info.StartShapeName)
            && !string.IsNullOrWhiteSpace(info.EndShapeName);
    }

    private static List<ShapePropertyRowData> ReadShapePropertyRows(object shapeObject)
    {
        dynamic shape = shapeObject;
        if (!TryGetShapeDataRowCount(shape, out int rowCount))
        {
            return [];
        }

        var properties = new List<ShapePropertyRowData>(rowCount);
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            dynamic? valueCell = null;
            dynamic? labelCell = null;
            try
            {
                valueCell = shape.CellsSRC(VisSectionProp, rowIndex, 0);
                labelCell = shape.CellsSRC(VisSectionProp, rowIndex, 2);

                string rowName = ExtractShapePropertyRowName(valueCell?.LocalName?.ToString() ?? string.Empty);
                string propertyName = ReadShapeDataCellText(labelCell);
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    propertyName = rowName;
                }

                properties.Add(new ShapePropertyRowData
                {
                    RowIndex = rowIndex,
                    RowName = rowName,
                    PropertyName = propertyName,
                    PropertyValue = ReadShapeDataCellText(valueCell)
                });
            }
            finally
            {
                if (labelCell != null) ComUtilities.Release(ref labelCell!);
                if (valueCell != null) ComUtilities.Release(ref valueCell!);
            }
        }

        return properties;
    }

    private static ShapePropertyRowData? FindShapePropertyRow(object shapeObject, string propertyName)
    {
        return ReadShapePropertyRows(shapeObject).FirstOrDefault(row =>
            string.Equals(row.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.RowName, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveTargetShapePropertyRowName(object shapeObject, string propertyName)
    {
        dynamic shape = shapeObject;
        var existingProperty = FindShapePropertyRow(shape, propertyName);
        if (existingProperty is not null)
        {
            return existingProperty.RowName;
        }

        string trimmedName = propertyName.Trim();
        if (IsValidShapeDataRowName(trimmedName))
        {
            return trimmedName;
        }

        return BuildNormalizedShapeDataRowName(trimmedName);
    }

    private static bool TryGetShapeDataRowCount(dynamic shape, out int rowCount)
    {
        try
        {
            rowCount = Convert.ToInt32(shape.RowCount(VisSectionProp));
            return true;
        }
        catch
        {
            rowCount = 0;
            return false;
        }
    }

    private static string ExtractShapePropertyRowName(string localName)
    {
        const string prefix = "Prop.";
        if (!localName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return localName;
        }

        string withoutPrefix = localName[prefix.Length..];
        int dotIndex = withoutPrefix.IndexOf('.');
        return dotIndex >= 0 ? withoutPrefix[..dotIndex] : withoutPrefix;
    }

    private static bool IsValidShapeDataRowName(string rowName)
    {
        if (string.IsNullOrWhiteSpace(rowName))
        {
            return false;
        }

        if (!(char.IsLetter(rowName[0]) || rowName[0] == '_'))
        {
            return false;
        }

        for (int index = 1; index < rowName.Length; index++)
        {
            char character = rowName[index];
            if (!(char.IsLetterOrDigit(character) || character == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildNormalizedShapeDataRowName(string propertyName)
    {
        var builder = new StringBuilder("Metadata_");
        bool previousUnderscore = true;

        foreach (char character in propertyName)
        {
            char normalized = char.IsLetterOrDigit(character) ? character : '_';
            if (normalized == '_' && previousUnderscore)
            {
                continue;
            }

            builder.Append(normalized);
            previousUnderscore = normalized == '_';
        }

        string baseName = builder.ToString().TrimEnd('_');
        if (baseName.Length == "Metadata".Length)
        {
            baseName = "Metadata_Property";
        }

        if (baseName.Length > 32)
        {
            baseName = baseName[..32];
        }

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(propertyName)))[..8].ToLowerInvariant();
        return $"{baseName}_{hash}";
    }

    private static string NormalizeConnectorEnd(string connectorEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorEnd);

        return connectorEnd.Trim().ToLowerInvariant() switch
        {
            "start" or "begin" => "start",
            "end" => "end",
            _ => throw new ArgumentOutOfRangeException(nameof(connectorEnd), "connectorEnd must be 'start' or 'end'.")
        };
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void SetConnectorEndpointMetadata(dynamic connector, string connectorEnd, string? targetShapeName)
    {
        string propertyName = connectorEnd == "start" ? StartShapeNameProperty : EndShapeNameProperty;
        WriteConnectorMetadata(connector, propertyName, targetShapeName ?? string.Empty);
    }

    private static void WriteConnectorMetadata(dynamic connector, string propertyName, string propertyValue)
    {
        dynamic? valueCell = null;
        try
        {
            EnsureShapeDataRow(connector, propertyName);
            valueCell = connector.CellsU[$"Prop.{propertyName}.Value"];
            valueCell.FormulaU = QuoteShapeDataValue(propertyValue);
        }
        finally
        {
            if (valueCell != null) ComUtilities.Release(ref valueCell!);
        }
    }

    private static string? ReadConnectorMetadata(dynamic connector, string propertyName)
    {
        dynamic? valueCell = null;
        try
        {
            valueCell = connector.CellsU[$"Prop.{propertyName}.Value"];
            return ReadShapeDataCellText(valueCell);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (valueCell != null) ComUtilities.Release(ref valueCell!);
        }
    }

    private static void EnsureShapeDataRow(dynamic shape, string rowName)
    {
        dynamic? existingCell = null;
        try
        {
            existingCell = shape.CellsU[$"Prop.{rowName}.Value"];
            return;
        }
        catch
        {
        }
        finally
        {
            if (existingCell != null) ComUtilities.Release(ref existingCell!);
        }

        try
        {
            shape.AddNamedRow(VisSectionProp, rowName, VisTagDefault);
        }
        catch
        {
            EnsureShapeDataSection(shape);
            shape.AddNamedRow(VisSectionProp, rowName, VisTagDefault);
        }
    }

    private static void EnsureShapeDataSection(dynamic shape)
    {
        try
        {
            shape.AddSection(VisSectionProp);
        }
        catch
        {
        }
    }

    private static string QuoteShapeDataValue(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string ReadShapeDataCellText(dynamic valueCell)
    {
        try
        {
            string? formula = valueCell.FormulaU?.ToString();
            if (string.IsNullOrEmpty(formula))
            {
                return string.Empty;
            }

            if (formula.Length >= 2 && formula[0] == '"' && formula[^1] == '"')
            {
                return formula[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
            }

            return formula;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class ShapePropertyRowData
    {
        public int RowIndex { get; init; }
        public string RowName { get; init; } = string.Empty;
        public string PropertyName { get; init; } = string.Empty;
        public string PropertyValue { get; init; } = string.Empty;
    }

    private static void TrySetCell(dynamic shape, string cellName, float? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        try
        {
            shape.CellsU[cellName].ResultIU = value.Value;
        }
        catch
        {
        }
    }

    private static float ReadCellResultIU(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            cell = shape.CellsU[cellName];
            return Convert.ToSingle(cell.ResultIU);
        }
        finally
        {
            if (cell != null) ComUtilities.Release(ref cell!);
        }
    }

    private static (double XPercent, double YPercent) GetGluePercentages(float startX, float startY, float endX, float endY, bool forStartShape)
    {
        float dx = endX - startX;
        float dy = endY - startY;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx >= 0)
            {
                return forStartShape ? (1d, 0.5d) : (0d, 0.5d);
            }

            return forStartShape ? (0d, 0.5d) : (1d, 0.5d);
        }

        if (dy >= 0)
        {
            return forStartShape ? (0.5d, 1d) : (0.5d, 0d);
        }

        return forStartShape ? (0.5d, 0d) : (0.5d, 1d);
    }

    private static float ToPageCoordinate(float points) => points / 72f;
    private static float ToPageX(float points) => ToPageCoordinate(points);
    private static float ToPageY(float points) => ToPageCoordinate(points);

    private static int HexToOleColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex[2..4], 16);
        int b = Convert.ToInt32(hex[4..6], 16);
        return r | (g << 8) | (b << 16);
    }

    public OperationResult SetGradientFill(IVisioBatch batch, int pageIndex, string shapeName, string color1, string color2, int gradientStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(color1);
        ArgumentException.ThrowIfNullOrWhiteSpace(color2);

        if (gradientStyle < 1 || gradientStyle > 6)
            throw new ArgumentOutOfRangeException(nameof(gradientStyle), "gradientStyle must be 1-6 (1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown, 5=FromCorner, 6=FromCenter)");

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio's gradient lives in the Fill Gradient section. FillGradientEnabled turns
                // it on; the two ends are the existing foreground and background fill colours,
                // which is how Visio's own two-colour gradients are expressed.
                ShapeSheetHelpers.SetFormula(shape, "FillForegnd", ShapeSheetHelpers.ToRgbFormula(color1, nameof(color1)));
                ShapeSheetHelpers.SetFormula(shape, "FillBkgnd", ShapeSheetHelpers.ToRgbFormula(color2, nameof(color2)));
                ShapeSheetHelpers.SetFormula(shape, "FillPattern", "1");
                ShapeSheetHelpers.SetFormula(shape, "FillGradientEnabled", "1");

                // FillGradientDir carries the direction. Visio's values do not line up with
                // PowerPoint's MsoGradientStyle, so the 1-6 input is passed through and callers
                // should treat it as a Visio direction index.
                ShapeSheetHelpers.SetFormula(shape, "FillGradientDir", gradientStyle.ToString(CultureInfo.InvariantCulture));

                return new OperationResult
                {
                    Success = true,
                    Action = "set-gradient-fill",
                    Message = $"Set gradient fill on shape '{shapeName}' from '{color1}' to '{color2}' (direction {gradientStyle}) on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetGlow(IVisioBatch batch, int pageIndex, string shapeName, float radius, string colorHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorHex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio has no Shape.Glow object; the effect lives in the GlowSize / GlowColor
                // cells. A size of 0 removes the glow.
                ShapeSheetHelpers.SetFormula(shape, "GlowSize", ShapeSheetHelpers.FormatInvariant(radius) + " pt");

                if (radius > 0)
                {
                    ShapeSheetHelpers.SetFormula(shape, "GlowColor", ShapeSheetHelpers.ToRgbFormula(colorHex, nameof(colorHex)));
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-glow",
                    Message = radius > 0
                        ? $"Set glow on shape '{shapeName}' with radius {ShapeSheetHelpers.FormatInvariant(radius)}pt and color '{colorHex}' on page {pageIndex}"
                        : $"Removed glow from shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetReflection(IVisioBatch batch, int pageIndex, string shapeName, int reflectionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (reflectionType is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reflectionType),
                reflectionType,
                "reflectionType must be 0-9 (0 = none, 1-9 = increasing reflection strength).");
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio has no preset reflection "types" like PowerPoint's msoReflectionType1-9.
                // It exposes the underlying quantities instead, so the 0-9 scale is mapped onto
                // them: size as a fraction of the shape, and a fixed blur and transparency that
                // read as a plausible reflection at each step.
                if (reflectionType == 0)
                {
                    ShapeSheetHelpers.SetFormula(shape, "ReflectionSize", "0");
                }
                else
                {
                    double fraction = reflectionType / 9d;
                    ShapeSheetHelpers.SetFormula(shape, "ReflectionSize", ShapeSheetHelpers.FormatInvariant(fraction));
                    ShapeSheetHelpers.SetFormula(shape, "ReflectionTrans", ShapeSheetHelpers.FormatInvariant(0.5));
                    ShapeSheetHelpers.SetFormula(shape, "ReflectionBlur", "0 pt");
                    ShapeSheetHelpers.SetFormula(shape, "ReflectionDist", "0 pt");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-reflection",
                    Message = reflectionType > 0
                        ? $"Set reflection strength {reflectionType} on shape '{shapeName}' on page {pageIndex}"
                        : $"Removed reflection from shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetOpacity(IVisioBatch batch, int pageIndex, string shapeName, float opacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (opacity < 0.0f || opacity > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(opacity), "opacity must be between 0.0 (transparent) and 1.0 (opaque)");

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio's FillForegndTrans is transparency (0 = opaque, 1 = fully transparent),
                // the inverse of opacity. Setting the line to match keeps the shape visually
                // coherent, which is what callers of a single "opacity" knob expect.
                string transparency = ShapeSheetHelpers.FormatInvariant(1.0f - opacity);
                ShapeSheetHelpers.SetFormula(shape, "FillForegndTrans", transparency);
                ShapeSheetHelpers.SetFormula(shape, "LineColorTrans", transparency);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-opacity",
                    Message = $"Set opacity of shape '{shapeName}' to {ShapeSheetHelpers.FormatInvariant(opacity)} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult FindByType(IVisioBatch batch, int pageIndex, int shapeType)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                int count = (int)shapes.Count;
                var matches = new List<string>();
                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        // Shape.Type reports VisShapeTypes, not MsoShapeType. See VisioShapeTypes
                        // for the values, confirmed against a live instance.
                        int type = Convert.ToInt32(shape.Type);
                        if (type == shapeType)
                        {
                            matches.Add(shape.NameU?.ToString() ?? $"Shape{i}");
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                string typeName = VisioShapeTypes.GetName(shapeType);
                string message = matches.Count > 0
                    ? $"Found {matches.Count} shape(s) of type {typeName} ({shapeType}): {string.Join(", ", matches)}"
                    : $"No shapes of type {typeName} ({shapeType}) found on page {pageIndex}";

                return new OperationResult
                {
                    Success = true,
                    Action = "find-by-type",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult CopyFormatting(IVisioBatch batch, int pageIndex, string sourceShapeName, string targetShapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceShapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetShapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic sourceShape = page.Shapes.Item(sourceShapeName);
            dynamic targetShape = page.Shapes.Item(targetShapeName);
            try
            {
                // Visio has no PickUp/Apply format painter on the object model, so the formatting
                // cells are copied explicitly. Only cells the source actually has are copied, so a
                // source without, say, a gradient does not clear the target's.
                var copied = new List<string>();

                foreach (var cellName in FormattingCellNames)
                {
                    var formula = ShapeSheetHelpers.TryGetFormula(sourceShape, cellName);
                    if (formula is null)
                    {
                        continue;
                    }

                    ShapeSheetHelpers.SetFormula(targetShape, cellName, formula);
                    copied.Add(cellName);
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "copy-formatting",
                    Message = $"Copied {copied.Count} formatting cell(s) from '{sourceShapeName}' to '{targetShapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref targetShape!);
                ComUtilities.Release(ref sourceShape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetActionSettings(IVisioBatch batch, int pageIndex, string shapeName, int actionType, string? hyperlinkAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        // Visio has no ActionSettings. PowerPoint's model - a click action that navigates between
        // slides - has no counterpart in a drawing, and the action types this method accepts
        // (NextSlide, PreviousSlide, FirstSlide, LastSlide) describe a slideshow.
        //
        // The nearest Visio equivalents are the Actions ShapeSheet section (right-click menu
        // commands) and hyperlinks, which are a different model rather than a rename. Failing with
        // a clear message beats the RuntimeBinderException this used to produce, which told the
        // caller nothing about why or what to use instead.
        _ = batch;
        _ = pageIndex;
        _ = actionType;
        _ = hyperlinkAddress;

        throw new NotSupportedException(
            "set-action-settings has no Visio equivalent. PowerPoint click actions navigate between " +
            "slides, which a drawing has no concept of. For navigation between pages use a hyperlink " +
            "with a SubAddress naming the target page (tracked in #35); for right-click commands use " +
            "the Actions ShapeSheet section.");
    }
    public OperationResult Scale(IVisioBatch batch, int pageIndex, string shapeName, float scaleX, float scaleY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio has no ScaleWidth/ScaleHeight; Width and Height are ShapeSheet cells, so
                // scaling is multiplication against the current result. The shape's pin is
                // unchanged, so it grows about its centre rather than its top-left.
                double width = ShapeSheetHelpers.TryGetResult(shape, "Width")
                    ?? throw new InvalidOperationException($"Shape '{shapeName}' has no Width cell.");
                double height = ShapeSheetHelpers.TryGetResult(shape, "Height")
                    ?? throw new InvalidOperationException($"Shape '{shapeName}' has no Height cell.");

                ShapeSheetHelpers.SetFormula(shape, "Width", ShapeSheetHelpers.FormatInvariant(width * scaleX) + " in");
                ShapeSheetHelpers.SetFormula(shape, "Height", ShapeSheetHelpers.FormatInvariant(height * scaleY) + " in");

                return new OperationResult
                {
                    Success = true,
                    Action = "scale",
                    Message = $"Scaled shape '{shapeName}' by {ShapeSheetHelpers.FormatInvariant(scaleX)}x width, {ShapeSheetHelpers.FormatInvariant(scaleY)}x height on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetLockAspectRatio(IVisioBatch batch, int pageIndex, string shapeName, bool locked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // LockAspect lives in the Protection section, which shapes do not always carry.
                // visSectionObject = 1, visRowLock = 20.
                if (ShapeSheetHelpers.TryGetResult(shape, "LockAspect") is null)
                {
                    shape.AddSection(1);
                    shape.AddRow(1, 20, 0);
                }

                ShapeSheetHelpers.SetFormula(shape, "LockAspect", locked ? "1" : "0");

                return new OperationResult
                {
                    Success = true,
                    Action = "lock-aspect-ratio",
                    Message = locked
                        ? $"Locked aspect ratio of shape '{shapeName}' on page {pageIndex}"
                        : $"Unlocked aspect ratio of shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetSoftEdge(IVisioBatch batch, int pageIndex, string shapeName, float radius)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Visio has no Shape.SoftEdge object; a single SoftEdgesSize cell carries the
                // effect, with 0 meaning none.
                ShapeSheetHelpers.SetFormula(shape, "SoftEdgesSize", ShapeSheetHelpers.FormatInvariant(radius) + " pt");

                return new OperationResult
                {
                    Success = true,
                    Action = "set-soft-edge",
                    Message = radius > 0
                        ? $"Set soft edge on shape '{shapeName}' with radius {ShapeSheetHelpers.FormatInvariant(radius)}pt on page {pageIndex}"
                        : $"Removed soft edge from shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult ReadShadow(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                double pattern = ShapeSheetHelpers.TryGetResult(shape, "ShdwPattern") ?? 0d;
                bool visible = pattern != 0d;

                string message;
                if (visible)
                {
                    // Offset cells are stored in internal units (inches); report points, which is
                    // what the setter accepts.
                    double offsetX = (ShapeSheetHelpers.TryGetResult(shape, "ShdwOffsetX") ?? 0d) * 72d;
                    double offsetY = (ShapeSheetHelpers.TryGetResult(shape, "ShdwOffsetY") ?? 0d) * 72d;
                    string colorFormula = ShapeSheetHelpers.TryGetFormula(shape, "ShdwForegnd") ?? string.Empty;

                    message = $"Visible: true, OffsetX: {ShapeSheetHelpers.FormatInvariant(offsetX)}pt, " +
                              $"OffsetY: {ShapeSheetHelpers.FormatInvariant(offsetY)}pt, Color: {colorFormula}";
                }
                else
                {
                    message = "Visible: false";
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "read-shadow",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddTextEffect(IVisioBatch batch, int pageIndex, int presetEffect, string text, string fontName, float fontSize, float left, float top)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        // WordArt is a PowerPoint/Word feature with no Visio counterpart - there is no TextEffect
        // cell and no Shapes.AddTextEffect. Failing with a clear message beats the
        // RuntimeBinderException this used to produce.
        _ = batch;
        _ = pageIndex;
        _ = presetEffect;
        _ = fontSize;
        _ = left;
        _ = top;

        throw new NotSupportedException(
            "add-text-effect has no Visio equivalent. WordArt preset effects are a PowerPoint feature. " +
            "To place styled text on a page use shape(add-textbox) followed by text(format), which " +
            "covers font, size, colour and weight.");
    }
    public OperationResult Set3D(IVisioBatch batch, int pageIndex, string shapeName, float? rotationX, float? rotationY, float? rotationZ, int? bevelType, float? bevelDepth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var changes = new List<string>();

                // Visio has no Shape.ThreeD object; the 3-D rotation and bevel cells carry it.
                if (rotationX.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "RotationXAngle", ShapeSheetHelpers.FormatInvariant(rotationX.Value) + " deg");
                    changes.Add($"RotationXAngle={ShapeSheetHelpers.FormatInvariant(rotationX.Value)}");
                }
                if (rotationY.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "RotationYAngle", ShapeSheetHelpers.FormatInvariant(rotationY.Value) + " deg");
                    changes.Add($"RotationYAngle={ShapeSheetHelpers.FormatInvariant(rotationY.Value)}");
                }
                if (rotationZ.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "RotationZAngle", ShapeSheetHelpers.FormatInvariant(rotationZ.Value) + " deg");
                    changes.Add($"RotationZAngle={ShapeSheetHelpers.FormatInvariant(rotationZ.Value)}");
                }
                if (bevelType.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "BevelTopType", bevelType.Value.ToString(CultureInfo.InvariantCulture));
                    changes.Add($"BevelTopType={bevelType.Value}");
                }
                if (bevelDepth.HasValue)
                {
                    // Visio expresses bevel depth as the top bevel's height rather than a
                    // separate depth quantity.
                    ShapeSheetHelpers.SetFormula(shape, "BevelTopHeight", ShapeSheetHelpers.FormatInvariant(bevelDepth.Value) + " pt");
                    changes.Add($"BevelTopHeight={ShapeSheetHelpers.FormatInvariant(bevelDepth.Value)}");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-3d",
                    Message = changes.Count > 0
                        ? $"Set 3D effects on shape '{shapeName}': {string.Join(", ", changes)} on page {pageIndex}"
                        : $"No 3D properties changed on shape '{shapeName}' (all parameters were null)",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
}
