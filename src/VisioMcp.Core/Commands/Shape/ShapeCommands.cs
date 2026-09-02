using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Commands.Slide;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Shape;

public class ShapeCommands : IShapeCommands
{
    private const int VisSectionProp = 243;
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
                    SetShapeFormula(shape, "FillPattern", "0");
                }
                else
                {
                    SetShapeFormula(shape, "FillForegnd", ToVisioRgbFormula(colorHex));
                    SetShapeFormula(shape, "FillPattern", "1");
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
                    SetShapeFormula(shape, "LinePattern", "0");
                }
                else
                {
                    SetShapeFormula(shape, "LineColor", ToVisioRgbFormula(colorHex));
                    SetShapeFormula(shape, "LinePattern", "1");

                    if (lineWidth > 0)
                    {
                        // LineWeight is a distance cell; state the unit so the value is not
                        // reinterpreted in the document's default units.
                        SetShapeFormula(shape, "LineWeight", FormatInvariant(lineWidth) + " pt");
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
                SetShapeFormula(shape, "Angle", FormatInvariant(-degrees) + " deg");

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
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            try
            {
                shape.AlternativeText = altText;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-alt-text",
                    Message = $"Set alt text of shape '{shapeName}' to '{altText}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult CopyToSlide(IVisioBatch batch, int pageIndex, string shapeName, int targetSlideIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = ctx.Presentation;
            dynamic srcSlide = pres.Slides.Item(pageIndex);
            dynamic shape = srcSlide.Shapes.Item(shapeName);
            try
            {
                shape.Copy();
                dynamic targetSlide = pres.Slides.Item(targetSlideIndex);
                dynamic pasted = targetSlide.Shapes.Paste();
                string newName = "";
                try { newName = pasted.Item(1).Name?.ToString() ?? ""; } catch { }
                ComUtilities.Release(ref pasted!);
                ComUtilities.Release(ref targetSlide!);

                return new OperationResult
                {
                    Success = true,
                    Action = "copy-to-slide",
                    Message = $"Copied shape '{shapeName}' from slide {pageIndex} to slide {targetSlideIndex}" +
                              (string.IsNullOrEmpty(newName) ? "" : $" as '{newName}'"),
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref srcSlide!);
            }
        });
    }

    public OperationResult SetShadow(IVisioBatch batch, int pageIndex, string shapeName, bool visible, float offsetX, float offsetY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            try
            {
                dynamic shadow = shape.Shadow;
                try
                {
                    shadow.Visible = visible ? -1 : 0;
                    if (visible)
                    {
                        shadow.OffsetX = offsetX;
                        shadow.OffsetY = offsetY;
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shadow!);
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-shadow",
                    Message = visible
                        ? $"Set shadow on shape '{shapeName}' (offset {offsetX},{offsetY})"
                        : $"Removed shadow from shape '{shapeName}'",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
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
                bool current = (TryGetShapeResult(shape, cellName) ?? 0d) != 0d;
                SetShapeFormula(shape, cellName, current ? "FALSE" : "TRUE");

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
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            dynamic? textFrame = null;
            try
            {
                textFrame = shape.TextFrame;
                if (marginLeft.HasValue) textFrame.MarginLeft = marginLeft.Value;
                if (marginRight.HasValue) textFrame.MarginRight = marginRight.Value;
                if (marginTop.HasValue) textFrame.MarginTop = marginTop.Value;
                if (marginBottom.HasValue) textFrame.MarginBottom = marginBottom.Value;
                if (wordWrap.HasValue) textFrame.WordWrap = wordWrap.Value ? -1 : 0; // msoTrue=-1, msoFalse=0
                if (autoSize.HasValue) textFrame.AutoSize = autoSize.Value; // ppAutoSizeNone=0, ppAutoSizeShapeToFitText=1, ppAutoSizeTextToFitShape=2

                return new OperationResult
                {
                    Success = true,
                    Action = "set-text-frame",
                    Message = $"Updated text frame properties of shape '{shapeName}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (textFrame != null) ComUtilities.Release(ref textFrame!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
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
                double pattern = TryGetShapeResult(shape, "FillPattern") ?? 0d;
                string colorFormula = TryGetShapeFormula(shape, "FillForegnd") ?? string.Empty;
                double transparency = TryGetShapeResult(shape, "FillForegndTrans") ?? 0d;

                // FillPattern 0 = no fill, 1 = solid, anything higher is one of Visio's
                // hatch/gradient patterns.
                string patternName = pattern switch
                {
                    0d => "None",
                    1d => "Solid",
                    _ => $"Pattern {FormatInvariant(pattern)}"
                };

                string message = pattern == 0d
                    ? "Fill: None"
                    : $"Fill: {patternName}, Color: {colorFormula}, Transparency: {FormatInvariant(transparency)}";

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
                double pattern = TryGetShapeResult(shape, "LinePattern") ?? 0d;
                bool visible = pattern != 0d;

                string colorFormula = TryGetShapeFormula(shape, "LineColor") ?? string.Empty;

                // LineWeight's ResultIU is in inches; Visio's internal drawing unit. Points are
                // what the setter accepts, so report the same unit.
                double weightPoints = (TryGetShapeResult(shape, "LineWeight") ?? 0d) * 72d;

                string message = visible
                    ? $"Visible: true, Color: {colorFormula}, Weight: {FormatInvariant(weightPoints)}pt"
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

    /// <summary>
    /// Writes a ShapeSheet formula by universal cell name, releasing the cell afterwards.
    /// </summary>
    private static void SetShapeFormula(dynamic shape, string cellName, string formula)
    {
        dynamic? cell = null;
        try
        {
            cell = shape.CellsU[cellName];
            cell.FormulaU = formula;
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Reads a ShapeSheet formula by universal cell name, or null when the cell does not exist.
    /// </summary>
    private static string? TryGetShapeFormula(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            if (!ShapeCellExists(shape, cellName))
            {
                return null;
            }

            cell = shape.CellsU[cellName];
            return cell.FormulaU?.ToString();
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Reads a ShapeSheet cell's evaluated result, or null when the cell does not exist.
    /// </summary>
    private static double? TryGetShapeResult(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            if (!ShapeCellExists(shape, cellName))
            {
                return null;
            }

            cell = shape.CellsU[cellName];
            return Convert.ToDouble(cell.ResultIU, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Whether a shape exposes the named ShapeSheet cell.
    /// </summary>
    /// <remarks>
    /// <c>CellExistsU</c> returns a VBA-style <c>short</c> (0 or -1), not a <c>bool</c>. Casting
    /// it directly to <c>bool</c> throws <c>RuntimeBinderException: Cannot convert type 'short'
    /// to 'bool'</c>, so the comparison is done numerically.
    /// </remarks>
    private static bool ShapeCellExists(dynamic shape, string cellName)
    {
        // visExistsAnywhere = 0: report the cell whether it is local or inherited from the master.
        return Convert.ToInt32(shape.CellExistsU[cellName, 0], CultureInfo.InvariantCulture) != 0;
    }

    /// <summary>
    /// Converts "#RRGGBB", "RRGGBB" or a Visio colour-index string into a ShapeSheet colour formula.
    /// </summary>
    private static string ToVisioRgbFormula(string colorHex)
    {
        var trimmed = colorHex.Trim().TrimStart('#');

        if (trimmed.Length != 6 || !int.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
        {
            throw new ArgumentException(
                $"Colour '{colorHex}' is not a 6-digit hex value such as '#FF0000' or 'FF0000'.",
                nameof(colorHex));
        }

        int r = (packed >> 16) & 0xFF;
        int g = (packed >> 8) & 0xFF;
        int b = packed & 0xFF;

        return $"RGB({r},{g},{b})";
    }

    private static string FormatInvariant(double value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);

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
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            try
            {
                // TwoColorGradient(style, variant) - variant 1 is default direction
                shape.Fill.TwoColorGradient(gradientStyle, 1);
                shape.Fill.ForeColor.RGB = HexToOleColor(color1);
                shape.Fill.BackColor.RGB = HexToOleColor(color2);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-gradient-fill",
                    Message = $"Set gradient fill on shape '{shapeName}' from '{color1}' to '{color2}' (style {gradientStyle}) on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult SetGlow(IVisioBatch batch, int pageIndex, string shapeName, float radius, string colorHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorHex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            try
            {
                dynamic glow = shape.Glow;
                try
                {
                    glow.Radius = radius;
                    if (radius > 0)
                    {
                        glow.Color.RGB = HexToOleColor(colorHex);
                    }
                }
                finally
                {
                    ComUtilities.Release(ref glow!);
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-glow",
                    Message = radius > 0
                        ? $"Set glow on shape '{shapeName}' with radius {radius}pt and color '{colorHex}' on slide {pageIndex}"
                        : $"Removed glow from shape '{shapeName}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult SetReflection(IVisioBatch batch, int pageIndex, string shapeName, int reflectionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (reflectionType < 0 || reflectionType > 9)
            throw new ArgumentOutOfRangeException(nameof(reflectionType), "reflectionType must be 0-9 (0=None, 1-9=msoReflectionType1 through msoReflectionType9)");

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            try
            {
                dynamic reflection = shape.Reflection;
                try
                {
                    reflection.Type = reflectionType;
                }
                finally
                {
                    ComUtilities.Release(ref reflection!);
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-reflection",
                    Message = reflectionType > 0
                        ? $"Set reflection type {reflectionType} on shape '{shapeName}' on slide {pageIndex}"
                        : $"Removed reflection from shape '{shapeName}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
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
                string transparency = FormatInvariant(1.0f - opacity);
                SetShapeFormula(shape, "FillForegndTrans", transparency);
                SetShapeFormula(shape, "LineColorTrans", transparency);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-opacity",
                    Message = $"Set opacity of shape '{shapeName}' to {FormatInvariant(opacity)} on page {pageIndex}",
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
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shapes = slide.Shapes;
            try
            {
                int count = (int)shapes.Count;
                var matches = new List<string>();
                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        int type = Convert.ToInt32(shape.Type);
                        if (type == shapeType)
                        {
                            matches.Add(shape.Name?.ToString() ?? $"Shape{i}");
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                string typeName = ShapeHelpers.GetShapeTypeName(shapeType);
                string message = matches.Count > 0
                    ? $"Found {matches.Count} shape(s) of type {typeName} ({shapeType}): {string.Join(", ", matches)}"
                    : $"No shapes of type {typeName} ({shapeType}) found on slide {pageIndex}";

                return new OperationResult
                {
                    Success = true,
                    Action = "find-by-type",
                    Message = message,
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult CopyFormatting(IVisioBatch batch, int pageIndex, string sourceShapeName, string targetShapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceShapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetShapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic sourceShape = slide.Shapes.Item(sourceShapeName);
            dynamic targetShape = slide.Shapes.Item(targetShapeName);
            try
            {
                sourceShape.PickUp();
                targetShape.Apply();

                return new OperationResult
                {
                    Success = true,
                    Action = "copy-formatting",
                    Message = $"Copied formatting from '{sourceShapeName}' to '{targetShapeName}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                ComUtilities.Release(ref targetShape!);
                ComUtilities.Release(ref sourceShape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult SetActionSettings(IVisioBatch batch, int pageIndex, string shapeName, int actionType, string? hyperlinkAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (actionType == 7 && string.IsNullOrWhiteSpace(hyperlinkAddress))
            throw new ArgumentException("hyperlinkAddress is required when actionType is 7 (Hyperlink)", nameof(hyperlinkAddress));

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            dynamic? actionSettings = null;
            dynamic? actionSetting = null;
            try
            {
                actionSettings = shape.ActionSettings;
                // Item(1) = ppMouseClick
                actionSetting = actionSettings.Item(1);
                actionSetting.Action = actionType;

                if (actionType == 7 && !string.IsNullOrWhiteSpace(hyperlinkAddress))
                {
                    dynamic hyperlink = actionSetting.Hyperlink;
                    try
                    {
                        hyperlink.Address = hyperlinkAddress;
                    }
                    finally
                    {
                        ComUtilities.Release(ref hyperlink!);
                    }
                }

                string actionDesc = actionType switch
                {
                    0 => "None",
                    1 => "NextSlide",
                    2 => "PreviousSlide",
                    3 => "FirstSlide",
                    4 => "LastSlide",
                    7 => $"Hyperlink ({hyperlinkAddress})",
                    _ => $"Action {actionType}"
                };

                return new OperationResult
                {
                    Success = true,
                    Action = "set-action-settings",
                    Message = $"Set action on shape '{shapeName}' to {actionDesc} on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (actionSetting != null) ComUtilities.Release(ref actionSetting!);
                if (actionSettings != null) ComUtilities.Release(ref actionSettings!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
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
                double width = TryGetShapeResult(shape, "Width")
                    ?? throw new InvalidOperationException($"Shape '{shapeName}' has no Width cell.");
                double height = TryGetShapeResult(shape, "Height")
                    ?? throw new InvalidOperationException($"Shape '{shapeName}' has no Height cell.");

                SetShapeFormula(shape, "Width", FormatInvariant(width * scaleX) + " in");
                SetShapeFormula(shape, "Height", FormatInvariant(height * scaleY) + " in");

                return new OperationResult
                {
                    Success = true,
                    Action = "scale",
                    Message = $"Scaled shape '{shapeName}' by {FormatInvariant(scaleX)}x width, {FormatInvariant(scaleY)}x height on page {pageIndex}",
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
                if (TryGetShapeResult(shape, "LockAspect") is null)
                {
                    shape.AddSection(1);
                    shape.AddRow(1, 20, 0);
                }

                SetShapeFormula(shape, "LockAspect", locked ? "1" : "0");

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
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            dynamic? softEdge = null;
            try
            {
                softEdge = shape.SoftEdge;
                // Type: 1 = msoSoftEdgeType1 (enabled), 0 = none
                softEdge.Type = radius > 0 ? 1 : 0;
                softEdge.Radius = radius;

                return new OperationResult
                {
                    Success = true,
                    Action = "set-soft-edge",
                    Message = radius > 0
                        ? $"Set soft edge on shape '{shapeName}' with radius {radius}pt on slide {pageIndex}"
                        : $"Removed soft edge from shape '{shapeName}' on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (softEdge != null) ComUtilities.Release(ref softEdge!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult ReadShadow(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            dynamic? shadow = null;
            try
            {
                shadow = shape.Shadow;
                bool visible = Convert.ToInt32(shadow.Visible) != 0;

                string message;
                if (visible)
                {
                    float offsetX = Convert.ToSingle(shadow.OffsetX);
                    float offsetY = Convert.ToSingle(shadow.OffsetY);
                    float blur = Convert.ToSingle(shadow.Blur);
                    int rgb = Convert.ToInt32(shadow.ForeColor.RGB);
                    int r = rgb & 0xFF;
                    int g = (rgb >> 8) & 0xFF;
                    int b = (rgb >> 16) & 0xFF;
                    string colorHex = $"#{r:X2}{g:X2}{b:X2}";
                    message = $"Visible: true, OffsetX: {offsetX:F2}, OffsetY: {offsetY:F2}, Blur: {blur:F2}, Color: {colorHex}";
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
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (shadow != null) ComUtilities.Release(ref shadow!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult AddTextEffect(IVisioBatch batch, int pageIndex, int presetEffect, string text, string fontName, float fontSize, float left, float top)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic? shape = null;
            try
            {
                // AddTextEffect(PresetTextEffect, Text, FontName, FontSize, FontBold, FontItalic, Left, Top)
                // FontBold=0 (msoFalse), FontItalic=0 (msoFalse)
                shape = slide.Shapes.AddTextEffect(presetEffect, text, fontName, fontSize, 0, 0, left, top);
                string name = shape.Name?.ToString() ?? "";

                return new OperationResult
                {
                    Success = true,
                    Action = "add-text-effect",
                    Message = $"Added text effect '{name}' with preset {presetEffect} on slide {pageIndex}",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }

    public OperationResult Set3D(IVisioBatch batch, int pageIndex, string shapeName, float? rotationX, float? rotationY, float? rotationZ, int? bevelType, float? bevelDepth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic slide = ((dynamic)ctx.Presentation).Slides.Item(pageIndex);
            dynamic shape = slide.Shapes.Item(shapeName);
            dynamic? threeD = null;
            try
            {
                threeD = shape.ThreeD;
                var changes = new List<string>();

                if (rotationX.HasValue)
                {
                    threeD.RotationX = rotationX.Value;
                    changes.Add($"RotationX={rotationX.Value}");
                }
                if (rotationY.HasValue)
                {
                    threeD.RotationY = rotationY.Value;
                    changes.Add($"RotationY={rotationY.Value}");
                }
                if (rotationZ.HasValue)
                {
                    threeD.RotationZ = rotationZ.Value;
                    changes.Add($"RotationZ={rotationZ.Value}");
                }
                if (bevelType.HasValue)
                {
                    threeD.BevelTopType = bevelType.Value;
                    changes.Add($"BevelTopType={bevelType.Value}");
                }
                if (bevelDepth.HasValue)
                {
                    threeD.BevelTopDepth = bevelDepth.Value;
                    changes.Add($"BevelTopDepth={bevelDepth.Value}");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-3d",
                    Message = changes.Count > 0
                        ? $"Set 3D effects on shape '{shapeName}': {string.Join(", ", changes)} on slide {pageIndex}"
                        : $"No 3D properties changed on shape '{shapeName}' (all parameters were null)",
                    FilePath = ctx.PresentationPath
                };
            }
            finally
            {
                if (threeD != null) ComUtilities.Release(ref threeD!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref slide!);
            }
        });
    }
}
