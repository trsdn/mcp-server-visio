using System.Globalization;
using System.Runtime.InteropServices;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.DataRecordset;

public class DataRecordsetCommands : IDataRecordsetCommands
{
    public DataRecordsetListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? dataRecordsets = null;
            try
            {
                dataRecordsets = GetDataRecordsets(ctx.Document);
                var result = new DataRecordsetListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath
                };

                for (var i = 1; i <= Convert.ToInt32(dataRecordsets.Count, CultureInfo.InvariantCulture); i++)
                {
                    dynamic? dataRecordset = null;
                    try
                    {
                        dataRecordset = dataRecordsets.Item(i);
                        result.DataRecordsets.Add(ReadDataRecordsetInfo(dataRecordset));
                    }
                    finally
                    {
                        if (dataRecordset != null) ComUtilities.Release(ref dataRecordset!);
                    }
                }

                return result;
            }
            finally
            {
                if (dataRecordsets != null) ComUtilities.Release(ref dataRecordsets!);
            }
        });
    }

    public DataRecordsetDetailResult AddFromXml(IVisioBatch batch, string name, string xml, int addOptions = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? dataRecordsets = null;
            dynamic? dataRecordset = null;
            try
            {
                dataRecordsets = GetDataRecordsets(ctx.Document);
                try
                {
                    dataRecordset = dataRecordsets.AddFromXML(xml, addOptions, name);
                }
                catch (COMException ex)
                {
                    throw CreateDataRecordsetUnavailableException("add a data recordset from XML", ex);
                }

                return new DataRecordsetDetailResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    DataRecordset = ReadDataRecordsetInfo(dataRecordset)
                };
            }
            finally
            {
                if (dataRecordset != null) ComUtilities.Release(ref dataRecordset!);
                if (dataRecordsets != null) ComUtilities.Release(ref dataRecordsets!);
            }
        });
    }

    public DataRecordsetRowsResult ReadRows(IVisioBatch batch, int dataRecordsetId)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic? dataRecordsets = null;
            dynamic? dataRecordset = null;
            try
            {
                dataRecordsets = GetDataRecordsets(ctx.Document);
                dataRecordset = dataRecordsets.ItemFromID(dataRecordsetId);

                return new DataRecordsetRowsResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    DataRecordsetId = dataRecordsetId,
                    Rows = ReadRows(dataRecordset)
                };
            }
            finally
            {
                if (dataRecordset != null) ComUtilities.Release(ref dataRecordset!);
                if (dataRecordsets != null) ComUtilities.Release(ref dataRecordsets!);
            }
        });
    }

    public DataRecordsetLinkResult LinkShape(IVisioBatch batch, int pageIndex, string shapeName, int dataRecordsetId, int rowId, bool autoApplyDataGraphics = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? pages = null;
            dynamic? page = null;
            dynamic? shapes = null;
            dynamic? shape = null;
            try
            {
                pages = ((dynamic)ctx.Document).Pages;
                page = pages.Item(pageIndex);
                shapes = page.Shapes;
                shape = shapes.ItemU(shapeName);

                try
                {
                    shape.LinkToData(dataRecordsetId, rowId, autoApplyDataGraphics);
                }
                catch (COMException ex)
                {
                    throw CreateDataRecordsetUnavailableException(
                        $"link shape '{shapeName}' to data recordset {dataRecordsetId} row {rowId}",
                        ex);
                }

                var linkedRow = Convert.ToInt32(shape.GetLinkedDataRow(dataRecordsetId), CultureInfo.InvariantCulture);

                return new DataRecordsetLinkResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    PageIndex = pageIndex,
                    ShapeName = shape.Name?.ToString() ?? shapeName,
                    ShapeId = Convert.ToInt32(shape.ID, CultureInfo.InvariantCulture),
                    DataRecordsetId = dataRecordsetId,
                    RowId = linkedRow
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (shapes != null) ComUtilities.Release(ref shapes!);
                if (page != null) ComUtilities.Release(ref page!);
                if (pages != null) ComUtilities.Release(ref pages!);
            }
        });
    }

    private static dynamic GetDataRecordsets(object document)
    {
        try
        {
            return ((dynamic)document).DataRecordsets;
        }
        catch (COMException ex)
        {
            throw CreateDataRecordsetUnavailableException("access document data recordsets", ex);
        }
    }

    private static DataRecordsetInfo ReadDataRecordsetInfo(dynamic dataRecordset)
    {
        var rowIds = ToIntList(dataRecordset.GetDataRowIDs(string.Empty));

        return new DataRecordsetInfo
        {
            Id = Convert.ToInt32(dataRecordset.ID, CultureInfo.InvariantCulture),
            Name = dataRecordset.Name?.ToString() ?? string.Empty,
            RowCount = rowIds.Count,
            RowIds = rowIds
        };
    }

    private static List<DataRecordsetRowInfo> ReadRows(dynamic dataRecordset)
    {
        var rows = new List<DataRecordsetRowInfo>();
        foreach (var rowId in ToIntList(dataRecordset.GetDataRowIDs(string.Empty)))
        {
            rows.Add(new DataRecordsetRowInfo
            {
                RowId = rowId,
                Values = ToStringList(dataRecordset.GetRowData(rowId))
            });
        }

        return rows;
    }

    private static List<int> ToIntList(object? value)
    {
        if (value is not Array values)
        {
            return [];
        }

        var result = new List<int>(values.Length);
        foreach (var item in values)
        {
            result.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
        }

        return result;
    }

    private static List<string> ToStringList(object? value)
    {
        if (value is not Array values)
        {
            return [];
        }

        var result = new List<string>(values.Length);
        foreach (var item in values)
        {
            result.Add(Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return result;
    }

    private static InvalidOperationException CreateDataRecordsetUnavailableException(string operation, COMException ex)
    {
        return new InvalidOperationException(
            "Visio data recordsets are unavailable or blocked by this Visio edition/licence while trying to "
            + $"{operation}. Data recordsets and data linking require a Visio edition that includes data features. "
            + $"Original COM error 0x{ex.HResult:X8}: {ex.Message}",
            ex);
    }
}
