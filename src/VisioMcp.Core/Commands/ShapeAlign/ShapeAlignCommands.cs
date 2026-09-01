using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.ShapeAlign;

public class ShapeAlignCommands : IShapeAlignCommands
{
    public OperationResult Align(IVisioBatch batch, int pageIndex, string shapeNames, int alignType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                var names = ParseShapeNames(shapeNames);
                var bounds = ReadShapeBounds((object)page, names);

                if (bounds.Count < 2)
                {
                    throw new InvalidOperationException("Align requires at least two shapes.");
                }

                switch (alignType)
                {
                    case 0:
                        {
                            float targetLeft = bounds.Min(item => item.Left);
                            foreach (var shape in bounds)
                            {
                                SetPinX((object)page, shape.Name, targetLeft + (shape.Width / 2f));
                            }

                            break;
                        }

                    case 1:
                        {
                            float targetCenterX = (bounds.Min(item => item.Left) + bounds.Max(item => item.Right)) / 2f;
                            foreach (var shape in bounds)
                            {
                                SetPinX((object)page, shape.Name, targetCenterX);
                            }

                            break;
                        }

                    case 2:
                        {
                            float targetRight = bounds.Max(item => item.Right);
                            foreach (var shape in bounds)
                            {
                                SetPinX((object)page, shape.Name, targetRight - (shape.Width / 2f));
                            }

                            break;
                        }

                    case 3:
                        {
                            float targetTop = bounds.Max(item => item.Top);
                            foreach (var shape in bounds)
                            {
                                SetPinY((object)page, shape.Name, targetTop - (shape.Height / 2f));
                            }

                            break;
                        }

                    case 4:
                        {
                            float targetCenterY = (bounds.Min(item => item.Bottom) + bounds.Max(item => item.Top)) / 2f;
                            foreach (var shape in bounds)
                            {
                                SetPinY((object)page, shape.Name, targetCenterY);
                            }

                            break;
                        }

                    case 5:
                        {
                            float targetBottom = bounds.Min(item => item.Bottom);
                            foreach (var shape in bounds)
                            {
                                SetPinY((object)page, shape.Name, targetBottom + (shape.Height / 2f));
                            }

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(alignType), alignType, "alignType must be between 0 and 5.");
                }

                string alignName = GetAlignTypeName(alignType);
                return new OperationResult
                {
                    Success = true,
                    Action = "align",
                    Message = $"Aligned {bounds.Count} shape(s) {alignName} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Distribute(IVisioBatch batch, int pageIndex, string shapeNames, int distributeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeNames);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                var names = ParseShapeNames(shapeNames);
                var bounds = ReadShapeBounds((object)page, names);

                if (bounds.Count < 3)
                {
                    throw new InvalidOperationException("Distribute requires at least three shapes.");
                }

                switch (distributeType)
                {
                    case 0:
                        {
                            var ordered = bounds.OrderBy(item => item.Left).ToList();
                            float span = ordered[^1].Right - ordered[0].Left;
                            float totalWidth = ordered.Sum(item => item.Width);
                            float gap = (span - totalWidth) / (ordered.Count - 1);
                            float currentLeft = ordered[0].Left;

                            foreach (var shape in ordered)
                            {
                                SetPinX((object)page, shape.Name, currentLeft + (shape.Width / 2f));
                                currentLeft += shape.Width + gap;
                            }

                            break;
                        }

                    case 1:
                        {
                            var ordered = bounds.OrderBy(item => item.Bottom).ToList();
                            float span = ordered[^1].Top - ordered[0].Bottom;
                            float totalHeight = ordered.Sum(item => item.Height);
                            float gap = (span - totalHeight) / (ordered.Count - 1);
                            float currentBottom = ordered[0].Bottom;

                            foreach (var shape in ordered)
                            {
                                SetPinY((object)page, shape.Name, currentBottom + (shape.Height / 2f));
                                currentBottom += shape.Height + gap;
                            }

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(distributeType), distributeType, "distributeType must be 0 or 1.");
                }

                string distName = distributeType == 0 ? "horizontally" : "vertically";
                return new OperationResult
                {
                    Success = true,
                    Action = "distribute",
                    Message = $"Distributed {bounds.Count} shape(s) {distName} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ctx.Document.Pages.Item(pageIndex);
    }

    private static List<string> ParseShapeNames(string shapeNames)
    {
        return shapeNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ShapeBounds> ReadShapeBounds(object pageObject, List<string> shapeNames)
    {
        dynamic page = pageObject;
        var results = new List<ShapeBounds>(shapeNames.Count);
        foreach (string name in shapeNames)
        {
            dynamic? shape = null;
            try
            {
                shape = page.Shapes.Item(name);
                float pinX = Convert.ToSingle(shape.CellsU["PinX"].ResultIU);
                float pinY = Convert.ToSingle(shape.CellsU["PinY"].ResultIU);
                float width = Convert.ToSingle(shape.CellsU["Width"].ResultIU);
                float height = Convert.ToSingle(shape.CellsU["Height"].ResultIU);

                results.Add(new ShapeBounds(name, pinX, pinY, width, height));
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
            }
        }

        return results;
    }

    private static void SetPinX(object pageObject, string shapeName, float pinX)
    {
        dynamic page = pageObject;
        dynamic? shape = null;
        dynamic? pinXCell = null;
        try
        {
            shape = page.Shapes.Item(shapeName);
            pinXCell = shape.CellsU["PinX"];
            pinXCell.ResultIU = pinX;
        }
        finally
        {
            if (pinXCell != null) ComUtilities.Release(ref pinXCell!);
            if (shape != null) ComUtilities.Release(ref shape!);
        }
    }

    private static void SetPinY(object pageObject, string shapeName, float pinY)
    {
        dynamic page = pageObject;
        dynamic? shape = null;
        dynamic? pinYCell = null;
        try
        {
            shape = page.Shapes.Item(shapeName);
            pinYCell = shape.CellsU["PinY"];
            pinYCell.ResultIU = pinY;
        }
        finally
        {
            if (pinYCell != null) ComUtilities.Release(ref pinYCell!);
            if (shape != null) ComUtilities.Release(ref shape!);
        }
    }

    private static string GetAlignTypeName(int alignType) => alignType switch
    {
        0 => "left",
        1 => "center",
        2 => "right",
        3 => "top",
        4 => "middle",
        5 => "bottom",
        _ => $"type({alignType})"
    };

    private sealed record ShapeBounds(string Name, float PinX, float PinY, float Width, float Height)
    {
        public float Left => PinX - (Width / 2f);
        public float Right => PinX + (Width / 2f);
        public float Bottom => PinY - (Height / 2f);
        public float Top => PinY + (Height / 2f);
    }
}
