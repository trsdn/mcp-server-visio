using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Page;

public class PageCommands : IPageCommands
{
    private const int VisTypeGuide = 5;

    /// <summary>VBA True. Visio normalises any non-zero written to Background into this.</summary>
    private const short VisTrue = -1;

    private const int VisGuidePoint = 1;
    private const int VisGuideHorizontal = 2;
    private const int VisGuideVertical = 3;
    private const short VisSelTypeEmpty = 0;
    private const short VisSelModeSkipSuper = 0;
    private const short VisSelect = 2;
    private const short VisPoints = 50;
    private const float PointsPerInch = 72f;
    private const float MovementTolerancePoints = 0.01f;
    private const string RouteStyleCell = "RouteStyle";
    private const string ConnectorRoutingExtensionCell = "ConLineRouteExt";
    private const string LineJumpCodeCell = "LineJumpCode";
    private const string LineJumpStyleCell = "LineJumpStyle";
    private const string WalkPreferenceCell = "WalkPreference";
    private const string PlaceStyleCell = "PlaceStyle";
    private const string LineRouteExtensionCell = "LineRouteExt";
    private const string PlaceDepthCell = "PlaceDepth";
    private const string ResizePageCell = "ResizePage";
    private const string LineJumpFactorXCell = "LineJumpFactorX";
    private const string LineJumpFactorYCell = "LineJumpFactorY";
    private const string LineToLineXCell = "LineToLineX";
    private const string LineToLineYCell = "LineToLineY";
    private const string AvenueSizeXCell = "AvenueSizeX";
    private const string AvenueSizeYCell = "AvenueSizeY";
    private const string BlockSizeXCell = "BlockSizeX";
    private const string BlockSizeYCell = "BlockSizeY";
    private const string PageLineJumpDirXCell = "PageLineJumpDirX";
    private const string PageLineJumpDirYCell = "PageLineJumpDirY";

    public PageListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;
            dynamic pages = document.Pages;
            try
            {
                var result = new PageListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath
                };

                int count = Convert.ToInt32(pages.Count);
                for (int i = 1; i <= count; i++)
                {
                    dynamic page = pages.Item(i);
                    try
                    {
                        result.Pages.Add(ReadPageInfo(page, i));
                    }
                    finally
                    {
                        ComUtilities.Release(ref page!);
                    }
                }

                return result;
            }
            finally
            {
                ComUtilities.Release(ref pages!);
            }
        });
    }

    public PageDetailResult Read(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                var result = new PageDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    Page = ReadPageInfo(page, pageIndex)
                };

                dynamic shapes = page.Shapes;
                try
                {
                    int shapeCount = Convert.ToInt32(shapes.Count);
                    for (int i = 1; i <= shapeCount; i++)
                    {
                        dynamic shape = shapes.Item(i);
                        try
                        {
                            result.Shapes.Add(ReadShapeInfo(shape));
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

                return result;
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Create(IVisioBatch batch, int position, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;
            dynamic pages = document.Pages;
            dynamic? newPage = null;
            try
            {
                newPage = pages.Add();
                newPage.Name = name;
                int newIndex = Convert.ToInt32(newPage.Index);

                if (position > 0 && newIndex != position)
                {
                    try
                    {
                        newPage.Index = position;
                        newIndex = Convert.ToInt32(newPage.Index);
                    }
                    catch
                    {
                    }
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "create",
                    Message = $"Created page '{name}' at position {newIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (newPage != null)
                {
                    ComUtilities.Release(ref newPage!);
                }

                ComUtilities.Release(ref pages!);
            }
        });
    }

    public OperationResult SetName(IVisioBatch batch, int pageIndex, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                string oldName = page.Name?.ToString() ?? string.Empty;
                page.Name = name;
                return new OperationResult
                {
                    Success = true,
                    Action = "set-name",
                    Message = $"Renamed page '{oldName}' to '{name}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                string pageName = page.Name?.ToString() ?? $"Page {pageIndex}";
                page.Delete(0);
                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted page '{pageName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageGuideListResult ListGuides(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                var result = new PageGuideListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    PageName = page.Name?.ToString() ?? string.Empty
                };

                int count = Convert.ToInt32(shapes.Count);
                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        if (Convert.ToInt32(shape.Type) == VisTypeGuide)
                        {
                            result.Guides.Add(ReadGuideInfo(shape));
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

    public OperationResult AddGuide(IVisioBatch batch, int pageIndex, int guideType, float xPosition, float yPosition)
    {
        ValidateGuideType(guideType);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? guide = null;
            try
            {
                guide = page.AddGuide(guideType, xPosition / PointsPerInch, yPosition / PointsPerInch);
                string guideName = guide.Name?.ToString() ?? string.Empty;

                return new OperationResult
                {
                    Success = true,
                    Action = "add-guide",
                    Message = $"Added {GetGuideTypeName(guideType)} guide '{guideName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (guide != null)
                {
                    ComUtilities.Release(ref guide!);
                }

                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetGuidePosition(IVisioBatch batch, int pageIndex, string guideName, float xPosition, float yPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guideName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic guide = page.Shapes.Item(guideName);
            try
            {
                EnsureGuideShape(guide, guideName);
                SetCellResultIU(guide, "PinX", xPosition / PointsPerInch);
                SetCellResultIU(guide, "PinY", yPosition / PointsPerInch);

                return new OperationResult
                {
                    Success = true,
                    Action = "set-guide-position",
                    Message = $"Moved guide '{guideName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref guide!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult DeleteGuide(IVisioBatch batch, int pageIndex, string guideName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guideName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic guide = page.Shapes.Item(guideName);
            try
            {
                EnsureGuideShape(guide, guideName);
                guide.Delete();

                return new OperationResult
                {
                    Success = true,
                    Action = "delete-guide",
                    Message = $"Deleted guide '{guideName}' from page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref guide!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageRoutingSettingsResult GetRoutingSettings(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                return DescribeRoutingSettings(ctx, page, pageSheet, pageIndex);
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetRouteStyle(IVisioBatch batch, int pageIndex, int routeStyle)
        => SetRoutingIntValue(batch, pageIndex, RouteStyleCell, routeStyle, "set-route-style");

    public OperationResult SetConnectorRoutingExtension(IVisioBatch batch, int pageIndex, int connectorRoutingExtension)
        => SetRoutingIntValue(batch, pageIndex, ConnectorRoutingExtensionCell, connectorRoutingExtension, "set-connector-routing-extension");

    public OperationResult SetLineJumpCode(IVisioBatch batch, int pageIndex, int lineJumpCode)
        => SetRoutingIntValue(batch, pageIndex, LineJumpCodeCell, lineJumpCode, "set-line-jump-code");

    public OperationResult SetLineJumpStyle(IVisioBatch batch, int pageIndex, int lineJumpStyle)
        => SetRoutingIntValue(batch, pageIndex, LineJumpStyleCell, lineJumpStyle, "set-line-jump-style");

    public OperationResult SetWalkPreference(IVisioBatch batch, int pageIndex, int walkPreference)
        => SetRoutingIntValue(batch, pageIndex, WalkPreferenceCell, walkPreference, "set-walk-preference");

    public OperationResult SetPlaceStyle(IVisioBatch batch, int pageIndex, int placeStyle)
        => SetRoutingIntValue(batch, pageIndex, PlaceStyleCell, placeStyle, "set-place-style");

    public PageLayoutResult LayoutPage(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                return ApplyLayout(ctx, (object)page, pageIndex, "page", null, (Action)(() => page.Layout()));
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageLayoutResult LayoutSelection(IVisioBatch batch, int pageIndex, string shapeNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);
        string[] names = ParseShapeNames(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? selection = null;
            try
            {
                selection = CreateSelection(page, names);
                return ApplyLayout(ctx, (object)page, pageIndex, "selection", string.Join(",", names), (Action)(() => selection.Layout()), names);
            }
            finally
            {
                if (selection != null) ComUtilities.Release(ref selection!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageLayoutResult IncrementalLayout(
        IVisioBatch batch,
        int pageIndex,
        string? shapeNames = null,
        int alignOrSpace = 3,
        int alignHorizontal = 1,
        int alignVertical = 1,
        float spaceHorizontal = 36f,
        float spaceVertical = 36f)
    {
        ValidateIncrementalLayoutArguments(alignOrSpace, alignHorizontal, alignVertical, spaceHorizontal, spaceVertical);
        string[]? names = string.IsNullOrWhiteSpace(shapeNames) ? null : ParseShapeNames(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? selection = null;
            try
            {
                if (names is null)
                {
                    return ApplyLayout(ctx, (object)page, pageIndex, "page", null,
                        (Action)(() => page.LayoutIncremental(alignOrSpace, alignHorizontal, alignVertical, spaceHorizontal, spaceVertical, VisPoints)));
                }

                selection = CreateSelection(page, names);
                return ApplyLayout(ctx, (object)page, pageIndex, "selection", string.Join(",", names),
                    (Action)(() => selection.LayoutIncremental(alignOrSpace, alignHorizontal, alignVertical, spaceHorizontal, spaceVertical, VisPoints)), names);
            }
            finally
            {
                if (selection != null) ComUtilities.Release(ref selection!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageLayoutResult ChangeLayoutDirection(IVisioBatch batch, int pageIndex, int direction = 0)
    {
        ValidateRange(direction, 0, 3, nameof(direction), "Layout direction must be 0 (rotate right), 1 (rotate left), 2 (flip vertical), or 3 (flip horizontal).");

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                return ApplyLayout(ctx, (object)page, pageIndex, "page", null, (Action)(() => page.LayoutChangeDirection(direction)));
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageRoutingSettingsResult SetPassiveRouting(IVisioBatch batch, int pageIndex, bool passive = false)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                page.LayoutRoutePassive = passive;
                return DescribeRoutingSettings(ctx, page, pageSheet, pageIndex);
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetLineRouteExtension(IVisioBatch batch, int pageIndex, int lineRouteExtension = 0)
    {
        ValidateRange(lineRouteExtension, 0, 2, nameof(lineRouteExtension), "Line route extension must be 0 (default straight), 1 (straight), or 2 (curved).");
        return SetRoutingIntValue(batch, pageIndex, LineRouteExtensionCell, lineRouteExtension, "set-line-route-extension");
    }

    public PageRoutingSettingsResult SetLayoutSpacing(IVisioBatch batch, int pageIndex, float avenueSizeX = 21.2598f, float avenueSizeY = 21.2598f)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(avenueSizeX);
        ArgumentOutOfRangeException.ThrowIfNegative(avenueSizeY);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                SetPageSheetPointCell(pageSheet, AvenueSizeXCell, avenueSizeX);
                SetPageSheetPointCell(pageSheet, AvenueSizeYCell, avenueSizeY);
                return DescribeRoutingSettings(ctx, page, pageSheet, pageIndex);
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageRoutingSettingsResult SetPlaceDepth(IVisioBatch batch, int pageIndex, int placeDepth = 0)
    {
        ValidateRange(placeDepth, 0, 3, nameof(placeDepth), "Place depth must be 0 (default), 1 (medium), 2 (deep), or 3 (shallow).");

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                SetPageSheetIntCell(pageSheet, PlaceDepthCell, placeDepth);
                return DescribeRoutingSettings(ctx, page, pageSheet, pageIndex);
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageRoutingSettingsResult SetResizePage(IVisioBatch batch, int pageIndex, bool resizePage = false)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                SetPageSheetBoolCell(pageSheet, ResizePageCell, resizePage);
                return DescribeRoutingSettings(ctx, page, pageSheet, pageIndex);
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static OperationResult SetRoutingIntValue(IVisioBatch batch, int pageIndex, string cellName, int value, string actionName)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic pageSheet = page.PageSheet;
            try
            {
                SetPageSheetIntCell(pageSheet, cellName, value);
                return new OperationResult
                {
                    Success = true,
                    Action = actionName,
                    Message = $"Set {cellName} to {value} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref pageSheet!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static PageRoutingSettingsResult DescribeRoutingSettings(VisioContext ctx, dynamic page, dynamic pageSheet, int pageIndex)
    {
        return new PageRoutingSettingsResult
        {
            Success = true,
            FilePath = ctx.DocumentPath,
            PageIndex = pageIndex,
            PageName = page.Name?.ToString() ?? string.Empty,
            RouteStyle = ReadPageSheetIntCell(pageSheet, RouteStyleCell),
            ConnectorRoutingExtension = ReadPageSheetIntCell(pageSheet, ConnectorRoutingExtensionCell),
            LineRouteExtension = ReadPageSheetIntCell(pageSheet, LineRouteExtensionCell),
            LineJumpCode = ReadPageSheetIntCell(pageSheet, LineJumpCodeCell),
            LineJumpStyle = ReadPageSheetIntCell(pageSheet, LineJumpStyleCell),
            WalkPreference = ReadPageSheetIntCell(pageSheet, WalkPreferenceCell),
            PlaceStyle = ReadPageSheetIntCell(pageSheet, PlaceStyleCell),
            PlaceDepth = ReadPageSheetIntCell(pageSheet, PlaceDepthCell),
            ResizePage = ReadPageSheetBoolCell(pageSheet, ResizePageCell),
            LayoutRoutePassive = Convert.ToBoolean(page.LayoutRoutePassive),
            LineJumpFactorX = ReadPageSheetFloatCell(pageSheet, LineJumpFactorXCell),
            LineJumpFactorY = ReadPageSheetFloatCell(pageSheet, LineJumpFactorYCell),
            LineToLineX = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, LineToLineXCell)),
            LineToLineY = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, LineToLineYCell)),
            AvenueSizeX = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, AvenueSizeXCell)),
            AvenueSizeY = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, AvenueSizeYCell)),
            BlockSizeX = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, BlockSizeXCell)),
            BlockSizeY = ConvertToPoints(ReadPageSheetFloatCell(pageSheet, BlockSizeYCell)),
            PageLineJumpDirX = ReadPageSheetIntCell(pageSheet, PageLineJumpDirXCell),
            PageLineJumpDirY = ReadPageSheetIntCell(pageSheet, PageLineJumpDirYCell)
        };
    }

    private static PageLayoutResult ApplyLayout(
        VisioContext ctx,
        object pageObject,
        int pageIndex,
        string scope,
        string? shapeNames,
        Action layoutAction,
        IReadOnlyCollection<string>? names = null)
    {
        dynamic page = pageObject;
        IReadOnlyDictionary<string, PageLayoutShapeChange> before = ReadShapeSnapshots(page, names);
        layoutAction();
        IReadOnlyDictionary<string, PageLayoutShapeChange> after = ReadShapeSnapshots(page, names);

        var result = new PageLayoutResult
        {
            Success = true,
            FilePath = ctx.DocumentPath,
            PageIndex = pageIndex,
            PageName = page.Name?.ToString() ?? string.Empty,
            Scope = scope,
            ShapeNames = shapeNames
        };

        foreach ((string name, PageLayoutShapeChange prior) in before)
        {
            if (!after.TryGetValue(name, out PageLayoutShapeChange? current))
            {
                continue;
            }

            result.Shapes.Add(new PageLayoutShapeChange
            {
                ShapeId = current.ShapeId,
                Name = name,
                BeforeLeft = prior.BeforeLeft,
                BeforeTop = prior.BeforeTop,
                AfterLeft = current.AfterLeft,
                AfterTop = current.AfterTop,
                Width = current.Width,
                Height = current.Height,
                Moved = HasMoved(prior.BeforeLeft, prior.BeforeTop, current.AfterLeft, current.AfterTop)
            });
        }

        int moved = result.Shapes.Count(s => s.Moved);
        result.Message = moved == 0
            ? "Visio completed the layout call but made no visible shape movement for the requested scope."
            : $"Visio moved {moved} of {result.Shapes.Count} tracked shapes. Auto-layout is destructive and can discard manual placement.";
        return result;
    }

    private static Dictionary<string, PageLayoutShapeChange> ReadShapeSnapshots(dynamic page, IReadOnlyCollection<string>? names)
    {
        var snapshots = new Dictionary<string, PageLayoutShapeChange>(StringComparer.OrdinalIgnoreCase);
        if (names is null)
        {
            dynamic shapes = page.Shapes;
            try
            {
                int count = Convert.ToInt32(shapes.Count);
                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        AddShapeSnapshot(snapshots, shape);
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

            return snapshots;
        }

        dynamic namedShapes = page.Shapes;
        try
        {
            foreach (string name in names)
            {
                dynamic shape = namedShapes.Item(name);
                try
                {
                    AddShapeSnapshot(snapshots, shape);
                }
                finally
                {
                    ComUtilities.Release(ref shape!);
                }
            }
        }
        finally
        {
            ComUtilities.Release(ref namedShapes!);
        }

        return snapshots;
    }

    private static void AddShapeSnapshot(Dictionary<string, PageLayoutShapeChange> snapshots, dynamic shape)
    {
        string name = shape.Name?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || snapshots.ContainsKey(name))
        {
            return;
        }

        float left = ConvertToPoints(ReadCellResultIU(shape, "PinX"));
        float top = ConvertToPoints(ReadCellResultIU(shape, "PinY"));
        snapshots.Add(name, new PageLayoutShapeChange
        {
            ShapeId = Convert.ToInt32(shape.ID),
            Name = name,
            BeforeLeft = left,
            BeforeTop = top,
            AfterLeft = left,
            AfterTop = top,
            Width = ConvertToPoints(ReadCellResultIU(shape, "Width")),
            Height = ConvertToPoints(ReadCellResultIU(shape, "Height"))
        });
    }

    private static dynamic CreateSelection(dynamic page, IReadOnlyCollection<string> names)
    {
        dynamic selection = page.CreateSelection(VisSelTypeEmpty, VisSelModeSkipSuper);
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            foreach (string name in names)
            {
                dynamic? shape = null;
                try
                {
                    shape = shapes.Item(name);
                    selection.Select(shape, VisSelect);
                }
                finally
                {
                    if (shape != null) ComUtilities.Release(ref shape!);
                }
            }

            return selection;
        }
        catch
        {
            ComUtilities.Release(ref selection!);
            throw;
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }

    private static string[] ParseShapeNames(string shapeNames)
    {
        string[] names = shapeNames
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
        {
            throw new ArgumentException("At least one shape name is required.", nameof(shapeNames));
        }

        return names;
    }

    private static void ValidateIncrementalLayoutArguments(
        int alignOrSpace,
        int alignHorizontal,
        int alignVertical,
        float spaceHorizontal,
        float spaceVertical)
    {
        ValidateRange(alignOrSpace, 1, 3, nameof(alignOrSpace), "align_or_space must be 1 (align), 2 (space), or 3 (align and space).");
        ValidateRange(alignHorizontal, 0, 4, nameof(alignHorizontal), "align_horizontal must be 0 (none), 1 (default), 2 (left), 3 (center), or 4 (right).");
        ValidateRange(alignVertical, 0, 4, nameof(alignVertical), "align_vertical must be 0 (none), 1 (default), 2 (top), 3 (middle), or 4 (bottom).");
        ArgumentOutOfRangeException.ThrowIfNegative(spaceHorizontal);
        ArgumentOutOfRangeException.ThrowIfNegative(spaceVertical);

        if (alignOrSpace == 1 && alignHorizontal == 0 && alignVertical == 0)
        {
            throw new ArgumentException("Incremental alignment needs align_horizontal or align_vertical to be non-zero.");
        }

        if (alignOrSpace is 2 or 3 && (alignHorizontal == 0 || alignVertical == 0))
        {
            throw new ArgumentException("Incremental spacing needs both align_horizontal and align_vertical to be non-zero.");
        }
    }

    private static void ValidateRange(int value, int min, int max, string paramName, string message)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, message);
        }
    }

    private static bool HasMoved(float beforeLeft, float beforeTop, float afterLeft, float afterTop)
        => Math.Abs(beforeLeft - afterLeft) > MovementTolerancePoints
           || Math.Abs(beforeTop - afterTop) > MovementTolerancePoints;

    private static void ValidateGuideType(int guideType)
    {
        if (guideType is not (VisGuidePoint or VisGuideHorizontal or VisGuideVertical))
        {
            throw new ArgumentOutOfRangeException(nameof(guideType), guideType, "Guide type must be 1 (point), 2 (horizontal), or 3 (vertical).");
        }
    }

    private static void EnsureGuideShape(dynamic guide, string guideName)
    {
        if (Convert.ToInt32(guide.Type) != VisTypeGuide)
        {
            throw new InvalidOperationException($"Shape '{guideName}' is not a guide.");
        }
    }

    private static PageInfo ReadPageInfo(dynamic page, int pageIndex)
    {
        var info = new PageInfo
        {
            PageIndex = pageIndex,
            Name = page.Name?.ToString() ?? string.Empty,
            ShapeCount = GetShapeCount(page),
            IsBackground = GetBackgroundFlag(page),
            BackPageName = GetBackPageName(page)
        };

        try { info.PageId = page.UniqueID[(short)0]?.ToString() ?? string.Empty; } catch { }
        if (string.IsNullOrEmpty(info.PageId))
        {
            try { info.PageId = page.ID.ToString(); } catch { }
        }

        return info;
    }

    private static int GetShapeCount(dynamic page)
    {
        dynamic? shapes = null;
        try
        {
            shapes = page.Shapes;
            return Convert.ToInt32(shapes.Count);
        }
        catch
        {
            return 0;
        }
        finally
        {
            if (shapes != null)
            {
                ComUtilities.Release(ref shapes!);
            }
        }
    }

    private static bool GetBackgroundFlag(dynamic page)
    {
        try
        {
            return Convert.ToBoolean(page.Background);
        }
        catch
        {
            return false;
        }
    }

    private static ShapeInfo ReadShapeInfo(dynamic shape)
    {
        var info = new ShapeInfo
        {
            Name = shape.Name?.ToString() ?? string.Empty
        };

        try { info.ShapeId = Convert.ToInt32(shape.ID); } catch { }
        try { info.Left = Convert.ToSingle(shape.CellsU["PinX"].ResultIU); } catch { }
        try { info.Top = Convert.ToSingle(shape.CellsU["PinY"].ResultIU); } catch { }
        try { info.Width = Convert.ToSingle(shape.CellsU["Width"].ResultIU); } catch { }
        try { info.Height = Convert.ToSingle(shape.CellsU["Height"].ResultIU); } catch { }
        try { info.Text = shape.Text?.ToString(); } catch { }
        try { info.AlternativeText = shape.CellsU["Comment"].FormulaU?.ToString(); } catch { }
        try { info.ShapeType = Convert.ToBoolean(shape.OneD) ? "Connector" : "Shape"; } catch { info.ShapeType = "Shape"; }

        info.HasTextFrame = !string.IsNullOrEmpty(info.Text);
        info.HasTable = false;
        info.HasChart = false;
        info.IsGroup = false;
        info.IsPlaceholder = false;
        info.ZOrderPosition = 0;

        return info;
    }

    private static PageGuideInfo ReadGuideInfo(dynamic guide)
    {
        int guideType = ReadGuideType(guide);
        return new PageGuideInfo
        {
            Name = guide.Name?.ToString() ?? string.Empty,
            ShapeType = Convert.ToInt32(guide.Type),
            GuideType = guideType,
            GuideTypeName = GetGuideTypeName(guideType),
            X = Convert.ToSingle(ReadCellResultIU(guide, "PinX") * PointsPerInch),
            Y = Convert.ToSingle(ReadCellResultIU(guide, "PinY") * PointsPerInch)
        };
    }

    private static int ReadGuideType(dynamic guide)
    {
        float width = ReadCellResultIU(guide, "Width");
        if (Math.Abs(width) < 0.001f)
        {
            return VisGuidePoint;
        }

        float angle = ReadCellResultIU(guide, "Angle");
        return Math.Abs(angle - (MathF.PI / 2f)) < 0.001f ? VisGuideVertical : VisGuideHorizontal;
    }

    private static string GetGuideTypeName(int guideType)
    {
        return guideType switch
        {
            VisGuidePoint => "point",
            VisGuideHorizontal => "horizontal",
            VisGuideVertical => "vertical",
            _ => "unknown"
        };
    }

    private static float ReadCellResultIU(dynamic shape, string cellName)
    {
        dynamic cell = shape.CellsU(cellName);
        try
        {
            return Convert.ToSingle(cell.ResultIU);
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static int ReadPageSheetIntCell(dynamic pageSheet, string cellName)
        => Convert.ToInt32(ReadPageSheetFloatCell(pageSheet, cellName));

    private static float ReadPageSheetFloatCell(dynamic pageSheet, string cellName)
    {
        dynamic cell = pageSheet.CellsU(cellName);
        try
        {
            return Convert.ToSingle(cell.ResultIU);
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static bool ReadPageSheetBoolCell(dynamic pageSheet, string cellName)
        => Math.Abs(ReadPageSheetFloatCell(pageSheet, cellName)) > float.Epsilon;

    private static void SetPageSheetIntCell(dynamic pageSheet, string cellName, int value)
    {
        dynamic cell = pageSheet.CellsU(cellName);
        try
        {
            cell.ResultIU = value;
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static void SetPageSheetPointCell(dynamic pageSheet, string cellName, float value)
    {
        dynamic cell = pageSheet.CellsU(cellName);
        try
        {
            cell.ResultIU = value / PointsPerInch;
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static void SetPageSheetBoolCell(dynamic pageSheet, string cellName, bool value)
    {
        dynamic cell = pageSheet.CellsU(cellName);
        try
        {
            cell.FormulaU = value ? "TRUE" : "FALSE";
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    private static float ConvertToPoints(float inches)
        => inches * PointsPerInch;

    private static void SetCellResultIU(dynamic shape, string cellName, float value)
    {
        dynamic cell = shape.CellsU(cellName);
        try
        {
            cell.ResultIU = value;
        }
        finally
        {
            ComUtilities.Release(ref cell!);
        }
    }

    // ── Background pages (#36c) ───────────────────────────────

    public PageBackgroundResult ReadBackground(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                return DescribeBackground(ctx, page, pageIndex, null);
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageBackgroundResult SetBackground(IVisioBatch batch, int pageIndex, bool isBackground)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                // Visio normalises any non-zero to -1 (VBA True), so write the canonical value.
                page.Background = isBackground ? VisTrue : 0;

                // Marking a page as a background MOVES it in the Pages collection: Visio orders
                // backgrounds after foregrounds. The index the caller passed is therefore stale,
                // and returning it would send the next call to a different page.
                int currentIndex = FindPageIndex(ctx, page, pageIndex);

                string message = isBackground
                    ? "Page is now a background. Attach it to a page with page(set-back-page). "
                      + "Note that page_index changed: Visio orders background pages after normal ones."
                    : "Page is no longer a background. Pages that showed it keep the attachment until cleared. "
                      + "Note that page_index changed: Visio orders background pages after normal ones.";

                return DescribeBackground(ctx, page, currentIndex, message);
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageBackgroundResult SetBackPage(IVisioBatch batch, int pageIndex, string backPageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backPageName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? target = null;
            try
            {
                target = ResolvePageByName(ctx, backPageName);

                // Visio rejects a non-background target with "Inappropriate target object for this
                // action", which does not say which object or why. Check first so the message can.
                if (!GetBackgroundFlag(target))
                {
                    throw new ArgumentException(
                        $"Page '{backPageName}' is not a background page, so it cannot be shown behind "
                        + "another page. Call page(set-background, is_background=true) on it first.",
                        nameof(backPageName));
                }

                if (string.Equals(page.Name?.ToString(), backPageName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"A page cannot show itself as its own background ('{backPageName}').",
                        nameof(backPageName));
                }

                page.BackPage = target;

                return DescribeBackground(ctx, page, pageIndex,
                    $"Page now shows background page '{backPageName}'.");
            }
            finally
            {
                if (target != null) ComUtilities.Release(ref target!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public PageBackgroundResult ClearBackPage(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                // Assigning null throws COMException "Invalid parameter"; an empty string is how
                // Visio detaches a background page.
                page.BackPage = string.Empty;

                return DescribeBackground(ctx, page, pageIndex,
                    "Background page detached. The background page itself still exists.");
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic ResolvePageByName(VisioContext ctx, string pageName)
    {
        dynamic pages = ctx.Document.Pages;
        try
        {
            try
            {
                return pages[pageName];
            }
            catch (Exception)
            {
                var available = new List<string>();
                int count = (int)pages.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic? p = null;
                    try
                    {
                        p = pages[i];
                        available.Add(GetBackgroundFlag(p)
                            ? $"{ComUtilities.SafeGetString(p, "Name")} (background)"
                            : ComUtilities.SafeGetString(p, "Name"));
                    }
                    finally
                    {
                        if (p != null) ComUtilities.Release(ref p!);
                    }
                }

                throw new ArgumentException(
                    $"Page '{pageName}' not found. This document has: {string.Join(", ", available)}.",
                    nameof(pageName));
            }
        }
        finally
        {
            ComUtilities.Release(ref pages!);
        }
    }

    /// <summary>
    /// Finds a page's current 1-based index by identity.
    /// </summary>
    /// <remarks>
    /// Needed because <c>Page.Background</c> reorders the collection: Visio keeps background pages
    /// after normal ones, so an index obtained before the flag was changed points elsewhere after.
    /// </remarks>
    private static int FindPageIndex(VisioContext ctx, dynamic page, int fallbackIndex)
    {
        dynamic? pages = null;
        try
        {
            int targetId = (int)page.ID;
            pages = ctx.Document.Pages;
            int count = (int)pages.Count;

            for (int i = 1; i <= count; i++)
            {
                dynamic? candidate = null;
                try
                {
                    candidate = pages[i];
                    if ((int)candidate.ID == targetId)
                    {
                        return i;
                    }
                }
                finally
                {
                    if (candidate != null) ComUtilities.Release(ref candidate!);
                }
            }

            return fallbackIndex;
        }
        finally
        {
            if (pages != null) ComUtilities.Release(ref pages!);
        }
    }

    private static PageBackgroundResult DescribeBackground(VisioContext ctx, dynamic page, int pageIndex, string? message)
    {
        return new PageBackgroundResult
        {
            Success = true,
            PageIndex = pageIndex,
            PageName = ComUtilities.SafeGetString(page, "Name"),
            IsBackground = GetBackgroundFlag(page),
            BackPageName = GetBackPageName(page),
            Message = message,
            FilePath = ctx.DocumentPath
        };
    }

    /// <summary>
    /// Name of the background page shown behind this one, or null when none is attached.
    /// </summary>
    private static string? GetBackPageName(dynamic page)
    {
        dynamic? backPage = null;
        try
        {
            backPage = page.BackPage;
            return backPage == null ? null : ComUtilities.SafeGetString(backPage, "Name");
        }
        catch (Exception)
        {
            // Older or restricted documents may not expose BackPage at all.
            return null;
        }
        finally
        {
            if (backPage != null) ComUtilities.Release(ref backPage!);
        }
    }
}
