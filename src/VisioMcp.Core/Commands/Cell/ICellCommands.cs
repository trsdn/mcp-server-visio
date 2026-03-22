using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Cell;

/// <summary>
/// Visio ShapeSheet cell operations for reading and writing shape-level cells.
/// </summary>
[ServiceCategory("cell")]
[McpTool("cell", Title = "Cell Operations", Destructive = true, Category = "shapesheet",
    Description = "Read, list, and update Visio ShapeSheet cells on a shape. "
    + "WORKFLOW: shape(add-shape) → cell(read, cell_name='Width') → cell(set-formula, cell_name='Width', formula='3 in'). "
    + "All operations are shape-scoped and use page_index + shape_name. "
    + "Value returns the evaluated internal-unit result; formula returns the raw ShapeSheet expression.")]
public interface ICellCommands
{
    /// <summary>
    /// Read one cell from a shape.
    /// </summary>
    [ServiceAction("read")]
    CellResult Read(IVisioBatch batch, int pageIndex, string shapeName, string cellName);

    /// <summary>
    /// Read the raw formula for one cell from a shape.
    /// </summary>
    [ServiceAction("read-formula")]
    CellResult ReadFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName);

    /// <summary>
    /// Write a literal value expression into a cell.
    /// </summary>
    [ServiceAction("write")]
    OperationResult Write(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string value);

    /// <summary>
    /// Set a raw ShapeSheet formula on a cell.
    /// </summary>
    [ServiceAction("set-formula")]
    OperationResult SetFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string formula);

    /// <summary>
    /// List discovered cells on a shape.
    /// </summary>
    [ServiceAction("list")]
    CellListResult List(IVisioBatch batch, int pageIndex, string shapeName);
}
