using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Cell;

public class CellCommands : ICellCommands
{
    public CellResult Read(IVisioBatch batch, int pageIndex, string shapeName, string cellName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsU[cellName];
            try
            {
                return new CellResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    Cell = ReadCellInfo(cell, cellName, includeValue: true, includeFormula: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public CellResult ReadFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsU[cellName];
            try
            {
                return new CellResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    Cell = ReadCellInfo(cell, cellName, includeValue: false, includeFormula: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Write(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsU[cellName];
            try
            {
                cell.FormulaU = NormalizeValueExpression(value);

                return new OperationResult
                {
                    Success = true,
                    Action = "write",
                    Message = $"Updated cell '{cellName}' on shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string formula)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);
        ArgumentException.ThrowIfNullOrWhiteSpace(formula);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsU[cellName];
            try
            {
                cell.FormulaU = formula;

                return new OperationResult
                {
                    Success = true,
                    Action = "set-formula",
                    Message = $"Set formula on cell '{cellName}' for shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public CellListResult List(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var result = new CellListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName
                };

                foreach (string knownCellName in KnownCellNames)
                {
                    dynamic? cell = null;
                    try
                    {
                        cell = shape.CellsU[knownCellName];
                        result.Cells.Add(ReadCellInfo(cell, knownCellName, includeValue: true, includeFormula: true));
                    }
                    finally
                    {
                        if (cell != null)
                        {
                            ComUtilities.Release(ref cell!);
                        }
                    }
                }

                result.Cells.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.CellName, right.CellName));
                return result;
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

    private static CellInfo ReadCellInfo(dynamic cell, string fallbackName, bool includeValue, bool includeFormula)
    {
        var info = new CellInfo
        {
            CellName = TryGetCellName(cell) ?? fallbackName
        };

        if (includeValue)
        {
            info.Value = TryGetCellValue(cell);
        }

        if (includeFormula)
        {
            info.Formula = TryGetCellFormula(cell);
        }

        return info;
    }

    private static string NormalizeValueExpression(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
        {
            return numericValue.ToString(CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string? TryGetCellName(dynamic cell)
    {
        try
        {
            return cell.NameU?.ToString();
        }
        catch
        {
            try
            {
                return cell.Name?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    private static string? TryGetCellValue(dynamic cell)
    {
        try
        {
            return Convert.ToString(cell.ResultIU, CultureInfo.InvariantCulture);
        }
        catch
        {
            try
            {
                return cell.FormulaU?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    private static string? TryGetCellFormula(dynamic cell)
    {
        try
        {
            return cell.FormulaU?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static readonly string[] KnownCellNames =
    [
        "PinX",
        "PinY",
        "Width",
        "Height",
        "Angle",
        "LocPinX",
        "LocPinY",
        "TxtWidth",
        "TxtHeight",
        "TxtPinX",
        "TxtPinY",
        "FillForegnd",
        "LineColor",
        "LineWeight",
        "Comment"
    ];
}
