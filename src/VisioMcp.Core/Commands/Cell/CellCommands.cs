using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Cell;

public class CellCommands : ICellCommands
{
    public CellResult Read(IVisioBatch batch, int pageIndex, string? shapeName, string cellName, string? sheetTarget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
            dynamic cell = shape.CellsU[cellName];
            try
            {
                return new CellResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = sheetRef.Label,
                    Cell = ReadCellInfo(cell, cellName, includeValue: true, includeFormula: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                sheetRef.Release();
            }
        });
    }

    public CellResult ReadFormula(IVisioBatch batch, int pageIndex, string? shapeName, string cellName, string? sheetTarget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
            dynamic cell = shape.CellsU[cellName];
            try
            {
                return new CellResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = sheetRef.Label,
                    Cell = ReadCellInfo(cell, cellName, includeValue: false, includeFormula: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                sheetRef.Release();
            }
        });
    }

    public OperationResult Write(IVisioBatch batch, int pageIndex, string? shapeName, string cellName, string value, string? sheetTarget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                sheetRef.Release();
            }
        });
    }

    public OperationResult SetFormula(IVisioBatch batch, int pageIndex, string? shapeName, string cellName, string formula, string? sheetTarget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);
        ArgumentException.ThrowIfNullOrWhiteSpace(formula);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                sheetRef.Release();
            }
        });
    }

    public CellListResult List(IVisioBatch batch, int pageIndex, string? shapeName, string? sheetTarget = null)
    {

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
            try
            {
                var result = new CellListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = sheetRef.Label
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
                sheetRef.Release();
            }
        });
    }

    /// <summary>
    /// A resolved ShapeSheet target, together with the COM objects that must be released.
    /// </summary>
    /// <remarks>
    /// Visio exposes the same section, row and cell API on a shape, on <c>Page.PageSheet</c> and on
    /// <c>Document.DocumentSheet</c> — confirmed against a live instance in #33. This lets one set
    /// of cell actions address all three rather than duplicating them per target.
    /// </remarks>
    private sealed class SheetRef
    {
        internal dynamic Sheet { get; init; } = null!;
        internal dynamic? Page { get; init; }

        /// <summary>Name for result payloads and messages, for example a shape name or "PageSheet".</summary>
        internal string Label { get; init; } = string.Empty;

        internal void Release()
        {
            dynamic? sheet = Sheet;
            if (sheet != null)
            {
                ComUtilities.Release(ref sheet!);
            }

            dynamic? page = Page;
            if (page != null)
            {
                ComUtilities.Release(ref page!);
            }
        }
    }

    /// <summary>
    /// Resolves <c>sheetTarget</c> to the sheet the caller means.
    /// </summary>
    /// <exception cref="ArgumentException">The target is not one of shape, page or document.</exception>
    private static SheetRef ResolveSheet(VisioContext ctx, string? sheetTarget, int pageIndex, string? shapeName)
    {
        var target = string.IsNullOrWhiteSpace(sheetTarget) ? "shape" : sheetTarget.Trim();

        if (string.Equals(target, "document", StringComparison.OrdinalIgnoreCase))
        {
            return new SheetRef
            {
                Sheet = ((dynamic)ctx.Document).DocumentSheet,
                Page = null,
                Label = "DocumentSheet"
            };
        }

        if (string.Equals(target, "page", StringComparison.OrdinalIgnoreCase))
        {
            dynamic page = GetPage(ctx, pageIndex);
            return new SheetRef
            {
                Sheet = page.PageSheet,
                Page = page,
                Label = "PageSheet"
            };
        }

        if (!string.Equals(target, "shape", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Unknown sheet target '{sheetTarget}'. Use 'shape' (the default), 'page' for the page's own "
                + "ShapeSheet, or 'document' for the document's.",
                nameof(sheetTarget));
        }

        if (string.IsNullOrWhiteSpace(shapeName))
        {
            throw new ArgumentException(
                "shapeName is required when sheet_target is 'shape'. Pass sheet_target='page' or 'document' "
                + "to address a page's or the document's own ShapeSheet instead.",
                nameof(shapeName));
        }

        dynamic shapePage = GetPage(ctx, pageIndex);
        dynamic shape = shapePage.Shapes.Item(shapeName);

        return new SheetRef
        {
            Sheet = shape,
            Page = shapePage,
            Label = SafeSheetString(() => shape.Name) ?? shapeName
        };
    }

    /// <summary>Reads a COM string property, returning null rather than throwing.</summary>
    private static string? SafeSheetString(Func<object?> read)
    {
        try
        {
            return read()?.ToString();
        }
        catch
        {
            return null;
        }
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

    public ShapeSheetSectionListResult ListSections(IVisioBatch batch, int pageIndex, string? shapeName, string? sheetTarget = null)
    {

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                    ShapeName = sheetRef.Label,
                    Sections = sections
                };
            }
            finally
            {
                sheetRef.Release();
            }
        });
    }

    public ShapeSheetRowListResult ListRows(IVisioBatch batch, int pageIndex, string? shapeName, string? section = null, string? sheetTarget = null)
    {

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                    ShapeName = sheetRef.Label,
                    SectionName = ShapeSheetSections.GetName(sectionIndex),
                    SectionIndex = sectionIndex,
                    Rows = rows
                };
            }
            finally
            {
                sheetRef.Release();
            }
        });
    }

    public ShapeSheetRowResult AddRow(IVisioBatch batch, int pageIndex, string? shapeName, string? section = null, string? rowName = null, string? sheetTarget = null)
    {

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                    ShapeName = sheetRef.Label,
                    SectionName = ShapeSheetSections.GetName(sectionIndex),
                    SectionIndex = sectionIndex,
                    Row = ReadRowInfo(shape, sectionIndex, rowIndex)
                };
            }
            finally
            {
                sheetRef.Release();
            }
        });
    }

    public OperationResult DeleteRow(IVisioBatch batch, int pageIndex, string? shapeName, string? section = null, int rowIndex = 0, string? sheetTarget = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                sheetRef.Release();
            }
        });
    }

    public CellResult ReadSrc(IVisioBatch batch, int pageIndex, string? shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0, string? sheetTarget = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                    ShapeName = sheetRef.Label,
                    Cell = ReadCellInfo(cell, cellName, includeValue: true, includeFormula: true)
                };
            }
            finally
            {
                ComUtilities.Release(ref cell!);
                sheetRef.Release();
            }
        });
    }

    public OperationResult WriteSrc(IVisioBatch batch, int pageIndex, string? shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0, string formula = "", string? sheetTarget = null)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);

        int sectionIndex = ShapeSheetSections.Resolve(section!);

        return batch.Execute((ctx, ct) =>
        {
            var sheetRef = ResolveSheet(ctx, sheetTarget, pageIndex, shapeName);
            dynamic shape = sheetRef.Sheet;
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
                sheetRef.Release();
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
