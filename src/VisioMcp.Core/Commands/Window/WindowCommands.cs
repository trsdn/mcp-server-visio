using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Window;

public class WindowCommands : IWindowCommands
{
    private const int VisDrawingWindowType = 1;
    private const int VisPageWindowSubType = 128;
    private const int VisFitNone = 0;
    private const int VisFitPage = 1;
    private const float PointsPerInch = 72f;
    private const int MinimumSnapStrength = 1;
    private const int MaximumSnapStrength = 999;

    public WindowInfoResult GetInfo(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            try
            {
                int state = Convert.ToInt32(app.WindowState);
                return new WindowInfoResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    WindowState = state,
                    WindowStateName = GetWindowStateName(state),
                    Left = Convert.ToSingle(app.Left),
                    Top = Convert.ToSingle(app.Top),
                    Width = Convert.ToSingle(app.Width),
                    Height = Convert.ToSingle(app.Height)
                };
            }
            finally
            {
                ComUtilities.Release(ref app!);
            }
        });
    }

    public OperationResult Minimize(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            try
            {
                app.WindowState = 2;
                return new OperationResult
                {
                    Success = true,
                    Action = "minimize",
                    Message = "Visio window minimized",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref app!);
            }
        });
    }

    public OperationResult Restore(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            try
            {
                app.WindowState = 1;
                return new OperationResult
                {
                    Success = true,
                    Action = "restore",
                    Message = "Visio window restored",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref app!);
            }
        });
    }

    public OperationResult Maximize(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            try
            {
                app.WindowState = 3;
                return new OperationResult
                {
                    Success = true,
                    Action = "maximize",
                    Message = "Visio window maximized",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref app!);
            }
        });
    }

    public OperationResult SetZoom(IVisioBatch batch, int zoomPercent)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(zoomPercent, 5);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(zoomPercent, 999);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, 1);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                window.Zoom = zoomPercent / 100d;

                return new OperationResult
                {
                    Success = true,
                    Action = "set-zoom",
                    Message = $"Set zoom to {zoomPercent}%",
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

    public WindowViewportResult GetZoom(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                return ReadViewport(window, page, ctx.DocumentPath, pageIndex, includeRect: false);
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public WindowViewportResult GetViewport(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                return ReadViewport(window, page, ctx.DocumentPath, pageIndex, includeRect: true);
            }
            finally
            {
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult FitPage(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                window.ViewFit = VisFitPage;

                return new OperationResult
                {
                    Success = true,
                    Action = "fit-page",
                    Message = $"Fitted page {pageIndex} to the active window",
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

    public OperationResult FitSelection(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            dynamic? selection = null;
            try
            {
                EnsureDrawingWindowPage(window, page);
                selection = window.Selection;
                var bounds = ReadSelectionBounds(selection);
                window.SetViewRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                window.ViewFit = VisFitNone;

                return new OperationResult
                {
                    Success = true,
                    Action = "fit-selection",
                    Message = $"Fitted the current selection on page {pageIndex} to the active window",
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

    public OperationResult PanToShape(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                EnsureDrawingWindowPage(window, page);
                float pinX = ReadCellResultIU(shape, "PinX");
                float pinY = ReadCellResultIU(shape, "PinY");
                window.ScrollViewTo(pinX, pinY);
                window.ViewFit = VisFitNone;

                return new OperationResult
                {
                    Success = true,
                    Action = "pan-to-shape",
                    Message = $"Centered window on shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult PanByOffset(IVisioBatch batch, int pageIndex, float offsetX, float offsetY)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                var rect = ReadViewRect(window);
                double offsetXInches = offsetX / PointsPerInch;
                double offsetYInches = offsetY / PointsPerInch;
                window.SetViewRect(
                    rect.Left + offsetXInches,
                    rect.Top + offsetYInches,
                    rect.Width,
                    rect.Height);
                window.ViewFit = VisFitNone;

                return new OperationResult
                {
                    Success = true,
                    Action = "pan-by-offset",
                    Message = $"Moved viewport by ({offsetX}, {offsetY}) points on page {pageIndex}",
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

    public WindowVisualAidsResult GetVisualAids(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            dynamic settings = ctx.Application.Settings;
            try
            {
                EnsureDrawingWindowPage(window, page);
                return new WindowVisualAidsResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    PageName = page.Name?.ToString() ?? string.Empty,
                    GridVisible = Convert.ToBoolean(window.ShowGrid),
                    GuidesVisible = Convert.ToBoolean(window.ShowGuides),
                    RulersVisible = Convert.ToBoolean(window.ShowRulers),
                    DrawingAidsEnabled = Convert.ToBoolean(settings.DrawingAids)
                };
            }
            finally
            {
                ComUtilities.Release(ref settings!);
                ComUtilities.Release(ref window!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetGridVisible(IVisioBatch batch, int pageIndex, bool visible)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                window.ShowGrid = visible;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-grid-visible",
                    Message = $"{(visible ? "Enabled" : "Disabled")} grid display on page {pageIndex}",
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

    public OperationResult SetGuidesVisible(IVisioBatch batch, int pageIndex, bool visible)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                window.ShowGuides = visible;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-guides-visible",
                    Message = $"{(visible ? "Enabled" : "Disabled")} guide display on page {pageIndex}",
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

    public OperationResult SetRulersVisible(IVisioBatch batch, int pageIndex, bool visible)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic window = GetDrawingWindow(ctx);
            try
            {
                EnsureDrawingWindowPage(window, page);
                window.ShowRulers = visible;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-rulers-visible",
                    Message = $"{(visible ? "Enabled" : "Disabled")} ruler display on page {pageIndex}",
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

    public OperationResult SetDrawingAids(IVisioBatch batch, bool enabled)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic settings = ctx.Application.Settings;
            try
            {
                settings.DrawingAids = enabled;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-drawing-aids",
                    Message = $"{(enabled ? "Enabled" : "Disabled")} Visio drawing aids",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref settings!);
            }
        });
    }

    public WindowSnapSettingsResult GetSnapSettings(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic settings = ctx.Application.Settings;
            try
            {
                return new WindowSnapSettingsResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    DrawingAidsEnabled = Convert.ToBoolean(settings.DrawingAids),
                    GridSnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthGridX", "SnapStrengthGridY"),
                    GuidesSnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthGuidesX", "SnapStrengthGuidesY"),
                    PointsSnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthPointsX", "SnapStrengthPointsY"),
                    RulerSnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthRulerX", "SnapStrengthRulerY"),
                    GeometrySnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthGeometryX", "SnapStrengthGeometryY"),
                    ExtensionsSnapStrength = ReadPairedSnapStrength(settings, "SnapStrengthExtensionsX", "SnapStrengthExtensionsY")
                };
            }
            finally
            {
                ComUtilities.Release(ref settings!);
            }
        });
    }

    public OperationResult SetGridSnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-grid-snap-strength", "grid", "SnapStrengthGridX", "SnapStrengthGridY");

    public OperationResult SetGuidesSnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-guides-snap-strength", "guides", "SnapStrengthGuidesX", "SnapStrengthGuidesY");

    public OperationResult SetPointsSnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-points-snap-strength", "points", "SnapStrengthPointsX", "SnapStrengthPointsY");

    public OperationResult SetRulerSnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-ruler-snap-strength", "ruler", "SnapStrengthRulerX", "SnapStrengthRulerY");

    public OperationResult SetGeometrySnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-geometry-snap-strength", "geometry", "SnapStrengthGeometryX", "SnapStrengthGeometryY");

    public OperationResult SetExtensionsSnapStrength(IVisioBatch batch, int strength)
        => SetSnapStrength(batch, strength, "set-extensions-snap-strength", "extensions", "SnapStrengthExtensionsX", "SnapStrengthExtensionsY");

    public OperationResult SetView(IVisioBatch batch, int viewType)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            dynamic? window = null;
            try
            {
                window = app.ActiveWindow;
                window.ViewType = viewType;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-view",
                    Message = $"Set legacy view to {GetViewTypeName(viewType)}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (window != null) ComUtilities.Release(ref window!);
                ComUtilities.Release(ref app!);
            }
        });
    }

    public OperationResult GetView(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic app = ctx.Application;
            dynamic? window = null;
            try
            {
                window = app.ActiveWindow;
                int viewType = Convert.ToInt32(window.ViewType);
                return new OperationResult
                {
                    Success = true,
                    Action = "get-view",
                    Message = $"Current legacy view: {GetViewTypeName(viewType)} ({viewType})",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (window != null) ComUtilities.Release(ref window!);
                ComUtilities.Release(ref app!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static OperationResult SetSnapStrength(
        IVisioBatch batch,
        int strength,
        string action,
        string categoryName,
        string xPropertyName,
        string yPropertyName)
    {
        ValidateSnapStrength(strength);

        return batch.Execute((ctx, ct) =>
        {
            dynamic settings = ctx.Application.Settings;
            try
            {
                WritePairedSnapStrength(settings, xPropertyName, yPropertyName, strength);
                return new OperationResult
                {
                    Success = true,
                    Action = action,
                    Message = $"Snap strength for {categoryName} set to {strength}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref settings!);
            }
        });
    }

    private static void ValidateSnapStrength(int strength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(strength, MinimumSnapStrength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(strength, MaximumSnapStrength);
    }

    private static dynamic GetDrawingWindow(VisioContext ctx)
    {
        dynamic window = ctx.Application.ActiveWindow;
        EnsureDrawingWindow(window);
        return window;
    }

    private static void EnsureDrawingWindow(dynamic window)
    {
        int windowType = Convert.ToInt32(window.Type);
        if (windowType != VisDrawingWindowType)
        {
            throw new InvalidOperationException($"Active Visio window is not a drawing window (type {windowType}).");
        }
    }

    private static void EnsureDrawingWindowPage(dynamic window, dynamic page)
    {
        EnsureDrawingWindow(window);

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

    private static int ReadPairedSnapStrength(dynamic settings, string xPropertyName, string yPropertyName)
    {
        int xValue = ReadIntProperty(settings, xPropertyName);
        int yValue = ReadIntProperty(settings, yPropertyName);
        return Math.Max(xValue, yValue);
    }

    private static void WritePairedSnapStrength(dynamic settings, string xPropertyName, string yPropertyName, int strength)
    {
        WriteIntProperty(settings, xPropertyName, strength);
        WriteIntProperty(settings, yPropertyName, strength);
    }

    private static int ReadIntProperty(dynamic target, string propertyName)
    {
        return propertyName switch
        {
            "SnapStrengthGridX" => Convert.ToInt32(target.SnapStrengthGridX),
            "SnapStrengthGridY" => Convert.ToInt32(target.SnapStrengthGridY),
            "SnapStrengthGuidesX" => Convert.ToInt32(target.SnapStrengthGuidesX),
            "SnapStrengthGuidesY" => Convert.ToInt32(target.SnapStrengthGuidesY),
            "SnapStrengthPointsX" => Convert.ToInt32(target.SnapStrengthPointsX),
            "SnapStrengthPointsY" => Convert.ToInt32(target.SnapStrengthPointsY),
            "SnapStrengthRulerX" => Convert.ToInt32(target.SnapStrengthRulerX),
            "SnapStrengthRulerY" => Convert.ToInt32(target.SnapStrengthRulerY),
            "SnapStrengthGeometryX" => Convert.ToInt32(target.SnapStrengthGeometryX),
            "SnapStrengthGeometryY" => Convert.ToInt32(target.SnapStrengthGeometryY),
            "SnapStrengthExtensionsX" => Convert.ToInt32(target.SnapStrengthExtensionsX),
            "SnapStrengthExtensionsY" => Convert.ToInt32(target.SnapStrengthExtensionsY),
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unknown snap strength property.")
        };
    }

    private static void WriteIntProperty(dynamic target, string propertyName, int value)
    {
        switch (propertyName)
        {
            case "SnapStrengthGridX":
                target.SnapStrengthGridX = value;
                break;
            case "SnapStrengthGridY":
                target.SnapStrengthGridY = value;
                break;
            case "SnapStrengthGuidesX":
                target.SnapStrengthGuidesX = value;
                break;
            case "SnapStrengthGuidesY":
                target.SnapStrengthGuidesY = value;
                break;
            case "SnapStrengthPointsX":
                target.SnapStrengthPointsX = value;
                break;
            case "SnapStrengthPointsY":
                target.SnapStrengthPointsY = value;
                break;
            case "SnapStrengthRulerX":
                target.SnapStrengthRulerX = value;
                break;
            case "SnapStrengthRulerY":
                target.SnapStrengthRulerY = value;
                break;
            case "SnapStrengthGeometryX":
                target.SnapStrengthGeometryX = value;
                break;
            case "SnapStrengthGeometryY":
                target.SnapStrengthGeometryY = value;
                break;
            case "SnapStrengthExtensionsX":
                target.SnapStrengthExtensionsX = value;
                break;
            case "SnapStrengthExtensionsY":
                target.SnapStrengthExtensionsY = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unknown snap strength property.");
        }
    }

    private static WindowViewportResult ReadViewport(dynamic window, dynamic page, string filePath, int pageIndex, bool includeRect)
    {
        var rect = includeRect ? ReadViewRect(window) : null;
        int windowType = Convert.ToInt32(window.Type);
        int windowSubType = Convert.ToInt32(window.SubType);
        int viewFit = Convert.ToInt32(window.ViewFit);
        double zoom = Convert.ToDouble(window.Zoom) * 100d;

        return new WindowViewportResult
        {
            Success = true,
            FilePath = filePath,
            PageIndex = pageIndex,
            PageName = page.Name?.ToString() ?? string.Empty,
            WindowType = windowType,
            WindowTypeName = GetWindowTypeName(windowType),
            WindowSubType = windowSubType,
            WindowSubTypeName = GetWindowSubTypeName(windowSubType),
            ViewFit = viewFit,
            ViewFitName = GetViewFitName(viewFit),
            ZoomPercent = Convert.ToSingle(zoom),
            Left = rect is null ? 0f : ToPoints(rect.Left),
            Top = rect is null ? 0f : ToPoints(rect.Top),
            Width = rect is null ? 0f : ToPoints(rect.Width),
            Height = rect is null ? 0f : ToPoints(rect.Height),
            CenterX = rect is null ? 0f : ToPoints(rect.Left + (rect.Width / 2d)),
            CenterY = rect is null ? 0f : ToPoints(rect.Top - (rect.Height / 2d))
        };
    }

    private static ViewRect ReadViewRect(dynamic window)
    {
        object left = 0d;
        object top = 0d;
        object width = 0d;
        object height = 0d;
        window.GetViewRect(ref left, ref top, ref width, ref height);
        return new ViewRect(
            Convert.ToDouble(left, CultureInfo.InvariantCulture),
            Convert.ToDouble(top, CultureInfo.InvariantCulture),
            Convert.ToDouble(width, CultureInfo.InvariantCulture),
            Convert.ToDouble(height, CultureInfo.InvariantCulture));
    }

    private static SelectionBounds ReadSelectionBounds(dynamic selection)
    {
        int count = Convert.ToInt32(selection.Count);
        if (count <= 0)
        {
            throw new InvalidOperationException("No shapes are currently selected.");
        }

        double? minLeft = null;
        double? maxRight = null;
        double? maxTop = null;
        double? minBottom = null;

        for (int i = 1; i <= count; i++)
        {
            dynamic? shape = null;
            try
            {
                shape = selection.Item(i);
                double pinX = ReadCellResultIU(shape, "PinX");
                double pinY = ReadCellResultIU(shape, "PinY");
                double width = ReadCellResultIU(shape, "Width");
                double height = ReadCellResultIU(shape, "Height");

                double left = pinX - (width / 2d);
                double right = pinX + (width / 2d);
                double bottom = pinY - (height / 2d);
                double top = pinY + (height / 2d);

                minLeft = minLeft.HasValue ? Math.Min(minLeft.Value, left) : left;
                maxRight = maxRight.HasValue ? Math.Max(maxRight.Value, right) : right;
                maxTop = maxTop.HasValue ? Math.Max(maxTop.Value, top) : top;
                minBottom = minBottom.HasValue ? Math.Min(minBottom.Value, bottom) : bottom;
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }

        double margin = 0.25d;
        double leftWithMargin = minLeft!.Value - margin;
        double topWithMargin = maxTop!.Value + margin;
        double widthWithMargin = Math.Max((maxRight!.Value - minLeft.Value) + (margin * 2d), 0.5d);
        double heightWithMargin = Math.Max((maxTop.Value - minBottom!.Value) + (margin * 2d), 0.5d);

        return new SelectionBounds(leftWithMargin, topWithMargin, widthWithMargin, heightWithMargin);
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

    private static float ToPoints(double pageUnits) => Convert.ToSingle(pageUnits * PointsPerInch);

    private static string GetWindowStateName(int state) => state switch
    {
        1 => "Normal",
        2 => "Minimized",
        3 => "Maximized",
        _ => $"Unknown({state})"
    };

    private static string GetViewTypeName(int viewType) => viewType switch
    {
        1 => "Normal",
        2 => "Outline",
        3 => "SlideSorter",
        4 => "NotesPage",
        5 => "SlideMaster",
        _ => $"Unknown({viewType})"
    };

    private static string GetWindowTypeName(int windowType) => windowType switch
    {
        1 => "Drawing",
        2 => "Stencil",
        3 => "ShapeSheet",
        4 => "Icon",
        5 => "Application",
        6 => "BuiltInAnchorBar",
        7 => "BuiltInDockedStencil",
        8 => "AddonDrawing",
        9 => "AddonStencil",
        10 => "AddonAnchorBar",
        11 => "AddonDockedStencil",
        _ => $"Unknown({windowType})"
    };

    private static string GetWindowSubTypeName(int windowSubType) => windowSubType switch
    {
        VisPageWindowSubType => "Page",
        160 => "PageGroup",
        64 => "Master",
        96 => "MasterGroup",
        _ => GetWindowTypeName(windowSubType)
    };

    private static string GetViewFitName(int viewFit) => viewFit switch
    {
        VisFitNone => "None",
        VisFitPage => "Page",
        2 => "Width",
        _ => $"Unknown({viewFit})"
    };

    private sealed record ViewRect(double Left, double Top, double Width, double Height);

    private sealed record SelectionBounds(double Left, double Top, double Width, double Height);
}
