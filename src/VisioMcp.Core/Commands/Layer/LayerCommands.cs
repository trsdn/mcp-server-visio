using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Layer;

public class LayerCommands : ILayerCommands
{
    private const int VisSectionLayer = 241;
    private const int VisLayerColor = 2;
    private const int VisLayerVisible = 4;
    private const int VisLayerPrint = 5;
    private const int VisLayerLock = 7;
    private const int VisLayerSnap = 8;
    private const int VisLayerGlue = 9;

    public LayerListResult List(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layers = page.Layers;
            try
            {
                var result = new LayerListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex
                };

                int count = Convert.ToInt32(layers.Count);
                for (int i = 1; i <= count; i++)
                {
                    dynamic layer = layers.Item(i);
                    try
                    {
                        result.Layers.Add(ReadLayerInfo(page, layer, pageIndex, includeShapeNames: false));
                    }
                    finally
                    {
                        ComUtilities.Release(ref layer!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref layers!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public LayerDetailResult Read(IVisioBatch batch, int pageIndex, string layerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            try
            {
                return new LayerDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    Layer = ReadLayerInfo(page, layer, pageIndex, includeShapeNames: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Create(
        IVisioBatch batch,
        int pageIndex,
        string layerName,
        int? colorIndex = null,
        bool? visible = null,
        bool? printable = null,
        bool? locked = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layers = page.Layers;
            dynamic? layer = null;
            try
            {
                layer = layers.Add(layerName);

                if (colorIndex.HasValue)
                {
                    WriteLayerInteger(layer, VisLayerColor, colorIndex.Value);
                }

                if (visible.HasValue)
                {
                    WriteLayerBoolean(layer, VisLayerVisible, visible.Value);
                }

                if (printable.HasValue)
                {
                    WriteLayerBoolean(layer, VisLayerPrint, printable.Value);
                }

                if (locked.HasValue)
                {
                    WriteLayerBoolean(layer, VisLayerLock, locked.Value);
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "create",
                    Message = $"Created layer '{layerName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (layer != null)
                {
                    ComUtilities.Release(ref layer!);
                }

                ComUtilities.Release(ref layers!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, int pageIndex, string layerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            dynamic? pageSheet = null;
            try
            {
                string resolvedName = ReadLayerName(layer);
                int row = Convert.ToInt32(layer.Row);
                RemoveAllShapesFromLayer(page, layer);
                pageSheet = page.PageSheet;
                pageSheet.DeleteRow(VisSectionLayer, row);

                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted layer '{resolvedName}' from page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (pageSheet != null)
                {
                    ComUtilities.Release(ref pageSheet!);
                }

                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult AddShape(IVisioBatch batch, int pageIndex, string layerName, string shapeName, bool preserveMembership = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                layer.Add(shape, preserveMembership ? 1 : 0);

                return new OperationResult
                {
                    Success = true,
                    Action = "add-shape",
                    Message = $"Added shape '{shapeName}' to layer '{ReadLayerName(layer)}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult RemoveShape(IVisioBatch batch, int pageIndex, string layerName, string shapeName, bool preserveMembership = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                layer.Remove(shape, preserveMembership ? 1 : 0);

                return new OperationResult
                {
                    Success = true,
                    Action = "remove-shape",
                    Message = $"Removed shape '{shapeName}' from layer '{ReadLayerName(layer)}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetVisibility(IVisioBatch batch, int pageIndex, string layerName, bool visible)
    {
        return SetLayerBoolean(batch, pageIndex, layerName, "set-visibility", "visibility", VisLayerVisible, visible);
    }

    public OperationResult SetPrint(IVisioBatch batch, int pageIndex, string layerName, bool printable)
    {
        return SetLayerBoolean(batch, pageIndex, layerName, "set-print", "printability", VisLayerPrint, printable);
    }

    public OperationResult SetLock(IVisioBatch batch, int pageIndex, string layerName, bool locked)
    {
        return SetLayerBoolean(batch, pageIndex, layerName, "set-lock", "lock state", VisLayerLock, locked);
    }

    public OperationResult SetColor(IVisioBatch batch, int pageIndex, string layerName, int colorIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);
        ArgumentOutOfRangeException.ThrowIfNegative(colorIndex);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            try
            {
                WriteLayerInteger(layer, VisLayerColor, colorIndex);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-color",
                    Message = $"Set color index of layer '{ReadLayerName(layer)}' to {colorIndex} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static OperationResult SetLayerBoolean(
        IVisioBatch batch,
        int pageIndex,
        string layerName,
        string action,
        string label,
        int cellIndex,
        bool value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic layer = FindLayer(page, layerName);
            try
            {
                WriteLayerBoolean(layer, cellIndex, value);

                return new OperationResult
                {
                    Success = true,
                    Action = action,
                    Message = $"Set {label} of layer '{ReadLayerName(layer)}' to {value} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref layer!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static dynamic FindLayer(dynamic page, string layerName)
    {
        dynamic layers = page.Layers;
        try
        {
            int count = Convert.ToInt32(layers.Count);
            for (int i = 1; i <= count; i++)
            {
                dynamic layer = layers.Item(i);
                try
                {
                    if (LayerNameMatches(layer, layerName))
                    {
                        return layer;
                    }
                }
                catch
                {
                    ComUtilities.Release(ref layer!);
                    throw;
                }

                ComUtilities.Release(ref layer!);
            }
        }
        finally
        {
            ComUtilities.Release(ref layers!);
        }

        throw new InvalidOperationException($"Layer '{layerName}' was not found.");
    }

    private static bool LayerNameMatches(dynamic layer, string layerName)
    {
        string name = ReadLayerName(layer);
        if (string.Equals(name, layerName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string nameU = ReadLayerUniversalName(layer);
        return string.Equals(nameU, layerName, StringComparison.OrdinalIgnoreCase);
    }

    private static LayerInfo ReadLayerInfo(dynamic page, dynamic layer, int pageIndex, bool includeShapeNames)
    {
        var info = new LayerInfo
        {
            PageIndex = pageIndex,
            Name = ReadLayerName(layer),
            NameU = ReadLayerUniversalName(layer),
            ColorIndex = ReadLayerInteger(layer, VisLayerColor),
            Visible = ReadLayerBoolean(layer, VisLayerVisible),
            Printable = ReadLayerBoolean(layer, VisLayerPrint),
            Locked = ReadLayerBoolean(layer, VisLayerLock),
            Snap = ReadLayerBoolean(layer, VisLayerSnap),
            Glue = ReadLayerBoolean(layer, VisLayerGlue)
        };

        var shapeNames = GetLayerShapeNames(page, info.Name, info.NameU);
        info.MemberCount = shapeNames.Count;

        if (includeShapeNames)
        {
            info.ShapeNames = shapeNames;
        }

        return info;
    }

    private static List<string> GetLayerShapeNames(dynamic page, string layerName, string layerNameU)
    {
        dynamic shapes = page.Shapes;
        try
        {
            int count = Convert.ToInt32(shapes.Count);
            var results = new List<string>();
            for (int i = 1; i <= count; i++)
            {
                dynamic shape = shapes.Item(i);
                try
                {
                    if (ShapeBelongsToLayer(shape, layerName, layerNameU))
                    {
                        string name = shape.Name?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            results.Add(name);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shape!);
                }
            }

            return results;
        }
        finally
        {
            ComUtilities.Release(ref shapes!);
        }
    }

    private static void RemoveAllShapesFromLayer(dynamic page, dynamic layer)
    {
        string layerName = ReadLayerName(layer);
        string layerNameU = ReadLayerUniversalName(layer);
        dynamic shapes = page.Shapes;
        try
        {
            int count = Convert.ToInt32(shapes.Count);
            for (int i = 1; i <= count; i++)
            {
                dynamic shape = shapes.Item(i);
                try
                {
                    if (ShapeBelongsToLayer(shape, layerName, layerNameU))
                    {
                        layer.Remove(shape, 1);
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shape!);
                }
            }
        }
        finally
        {
            ComUtilities.Release(ref shapes!);
        }
    }

    private static bool ShapeBelongsToLayer(dynamic shape, string layerName, string layerNameU)
    {
        int layerCount;
        try
        {
            layerCount = Convert.ToInt32(shape.LayerCount);
        }
        catch
        {
            return false;
        }

        for (int i = 1; i <= layerCount; i++)
        {
            dynamic assignedLayer = shape.Layer(i);
            try
            {
                string assignedName = ReadLayerName(assignedLayer);
                if (string.Equals(assignedName, layerName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                string assignedNameU = ReadLayerUniversalName(assignedLayer);
                if (string.Equals(assignedNameU, layerNameU, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            finally
            {
                ComUtilities.Release(ref assignedLayer!);
            }
        }

        return false;
    }

    private static string ReadLayerName(dynamic layer)
    {
        try
        {
            return layer.Name?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadLayerUniversalName(dynamic layer)
    {
        try
        {
            return layer.NameU?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool ReadLayerBoolean(dynamic layer, int cellIndex)
    {
        dynamic? cell = null;
        try
        {
            cell = layer.CellsC(cellIndex);
            return Math.Abs(Convert.ToDouble(cell.ResultIU, CultureInfo.InvariantCulture)) > double.Epsilon;
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    private static int ReadLayerInteger(dynamic layer, int cellIndex)
    {
        dynamic? cell = null;
        try
        {
            cell = layer.CellsC(cellIndex);
            return Convert.ToInt32(cell.ResultIU, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    private static void WriteLayerBoolean(dynamic layer, int cellIndex, bool value)
    {
        dynamic? cell = null;
        try
        {
            cell = layer.CellsC(cellIndex);
            cell.FormulaU = value ? "1" : "0";
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    private static void WriteLayerInteger(dynamic layer, int cellIndex, int value)
    {
        dynamic? cell = null;
        try
        {
            cell = layer.CellsC(cellIndex);
            cell.FormulaU = value.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }
}
