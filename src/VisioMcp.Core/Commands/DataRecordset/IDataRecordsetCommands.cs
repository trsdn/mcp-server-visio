using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.DataRecordset;

/// <summary>
/// Read Visio data recordsets, import ADO XML data, and link rows to shapes.
/// </summary>
[ServiceCategory("datarecordset", "DataRecordset")]
[McpTool("data_recordset", Title = "Data Recordset Operations", Destructive = true, Category = "data",
    Description = "Manage Visio document data recordsets. Data recordsets are a Visio Professional feature; if the installed edition blocks them the result explains the edition/licence limitation. "
    + "Use list/read-rows to inspect imported rows, add-from-xml for ADO classic XML data, and link-shape to link an existing page shape to a row. "
    + "add_options for add-from-xml: 0=default, 1=do not show External Data UI, 4=hide advanced configuration UI, 16=do not copy shape-data links.")]
public interface IDataRecordsetCommands
{
    /// <summary>List data recordsets in the current document.</summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list")]
    DataRecordsetListResult List(IVisioBatch batch);

    /// <summary>Add a data recordset from an ADO classic XML string.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="name">Display name for the new data recordset</param>
    /// <param name="xml">ADO classic XML rowset payload. Use xmlFile in CLI/MCP to read it from a file.</param>
    /// <param name="addOptions">0=default, 1=hide External Data UI, 4=hide advanced configuration UI, 16=do not copy shape-data links</param>
    [ServiceAction("add-from-xml")]
    DataRecordsetDetailResult AddFromXml(IVisioBatch batch, string name, [FileOrValue] string xml, int addOptions = 1);

    /// <summary>Read all rows from a data recordset.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="dataRecordsetId">ID of the data recordset returned by list or add-from-xml</param>
    [ServiceAction("read-rows")]
    DataRecordsetRowsResult ReadRows(IVisioBatch batch, int dataRecordsetId);

    /// <summary>Link an existing page shape to one data recordset row.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index containing the shape</param>
    /// <param name="shapeName">Existing shape name to link to data</param>
    /// <param name="dataRecordsetId">ID of the data recordset containing the row</param>
    /// <param name="rowId">Row ID returned by read-rows or add-from-xml</param>
    /// <param name="autoApplyDataGraphics">True to let Visio auto-apply the current data graphic; false only links the row</param>
    [ServiceAction("link-shape")]
    DataRecordsetLinkResult LinkShape(IVisioBatch batch, int pageIndex, string shapeName, int dataRecordsetId, int rowId, bool autoApplyDataGraphics = false);
}
