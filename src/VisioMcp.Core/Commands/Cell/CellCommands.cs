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

    public ShapeSheetSectionListResult ListSections(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var sections = new List<ShapeSheetSectionInfo>();

                foreach (var (name, index) in ShapeSheetSections.Known)
                {
                    if (!ShapeSheetSections.SectionExists(shape, index))
                    {
                        continue;
                    }

                    sections.Add(new ShapeSheetSectionInfo
                    {
                        SectionName = name,
                        SectionIndex = index,
                        RowCount = ShapeSheetSections.RowCount(shape, index)
                    });
                }

                return new ShapeSheetSectionListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    Sections = sections
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeSheetRowListResult ListRows(IVisioBatch batch, int pageIndex, string shapeName, string? section = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var rows = new List<ShapeSheetRowInfo>();
                int rowCount = ShapeSheetSections.RowCount(shape, sectionIndex);

                for (int row = 0; row < rowCount; row++)
                {
                    rows.Add(ReadRowInfo(shape, sectionIndex, row));
                }

                return new ShapeSheetRowListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    SectionName = ShapeSheetSections.GetName(sectionIndex),
                    SectionIndex = sectionIndex,
                    Rows = rows
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public ShapeSheetRowResult AddRow(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, string? rowName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                if (!ShapeSheetSections.SectionExists(shape, sectionIndex))
                {
                    shape.AddSection(sectionIndex);
                }

                int rowIndex = string.IsNullOrWhiteSpace(rowName)
                    ? Convert.ToInt32(shape.AddRow(sectionIndex, ShapeSheetSections.RowLast, ShapeSheetSections.TagDefault))
                    : Convert.ToInt32(shape.AddNamedRow(sectionIndex, rowName, ShapeSheetSections.TagDefault));

                return new ShapeSheetRowResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    SectionName = ShapeSheetSections.GetName(sectionIndex),
                    SectionIndex = sectionIndex,
                    Row = ReadRowInfo(shape, sectionIndex, rowIndex)
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult DeleteRow(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                int rowCount = ShapeSheetSections.RowCount(shape, sectionIndex);

                if (rowIndex >= rowCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rowIndex),
                        rowIndex,
                        $"Section '{ShapeSheetSections.GetName(sectionIndex)}' has {rowCount} row(s) on shape '{shapeName}'.");
                }

                shape.DeleteRow(sectionIndex, rowIndex);

                return new OperationResult
                {
                    Success = true,
                    Action = "delete-row",
                    Message = $"Deleted row {rowIndex} from section '{ShapeSheetSections.GetName(sectionIndex)}' on shape '{shapeName}'. Rows below it have shifted up.",
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

    public CellResult ReadSrc(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsSRC(sectionIndex, rowIndex, columnIndex);
            try
            {
                string cellName = TryGetCellName(cell)
                    ?? $"{ShapeSheetSections.GetName(sectionIndex)}[{rowIndex},{columnIndex}]";

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

    public OperationResult WriteSrc(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0, string formula = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            dynamic cell = shape.CellsSRC(sectionIndex, rowIndex, columnIndex);
            try
            {
                cell.FormulaU = formula;

                string cellName = TryGetCellName(cell)
                    ?? $"{ShapeSheetSections.GetName(sectionIndex)}[{rowIndex},{columnIndex}]";

                return new OperationResult
                {
                    Success = true,
                    Action = "write-src",
                    Message = $"Set {cellName} to '{formula}' on shape '{shapeName}'.",
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

    /// <summary>
    /// Reads a row's identity from its first cell. A positional row such as a connection point has
    /// no <c>RowName</c>, but its cell still carries a usable name like <c>Connections.X1</c>.
    /// </summary>
    private static ShapeSheetRowInfo ReadRowInfo(dynamic shape, int sectionIndex, int rowIndex)
    {
        dynamic? cell = null;
        try
        {
            cell = shape.CellsSRC(sectionIndex, rowIndex, 0);

            return new ShapeSheetRowInfo
            {
                RowIndex = rowIndex,
                RowName = TryGetRowName(cell) ?? string.Empty,
                CellName = TryGetCellName(cell) ?? string.Empty
            };
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    private static string? TryGetRowName(dynamic cell)
    {
        try
        {
            return cell.RowName?.ToString();
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
