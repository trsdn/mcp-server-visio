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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="cellName">ShapeSheet cell name, for example Width, PinX, FillForegnd, Char.Size or Prop.Cost. Section cells use the Section.Row.Cell form</param>
    [ServiceAction("read")]
    CellResult Read(IVisioBatch batch, int pageIndex, string shapeName, string cellName);

    /// <summary>
    /// Read the raw formula for one cell from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="cellName">ShapeSheet cell name. Use read-formula rather than read for text-valued cells such as Comment or Char.Font, which evaluate to 0</param>
    [ServiceAction("read-formula")]
    CellResult ReadFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName);

    /// <summary>
    /// Write a literal value expression into a cell.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="cellName">ShapeSheet cell name to write</param>
    /// <param name="value">Literal value. Distance cells need explicit units such as '3 in' or '12 pt', and angles need ' deg'; a bare number is read in internal units (inches, radians)</param>
    [ServiceAction("write")]
    OperationResult Write(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string value);

    /// <summary>
    /// Set a raw ShapeSheet formula on a cell.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="cellName">ShapeSheet cell name to write</param>
    /// <param name="formula">ShapeSheet expression, for example 'Width*0.5' or 'GUARD(2 in)'. A formula recalculates when its inputs change; a literal value does not</param>
    [ServiceAction("set-formula")]
    OperationResult SetFormula(IVisioBatch batch, int pageIndex, string shapeName, string cellName, string formula);

    /// <summary>
    /// List discovered cells on a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    [ServiceAction("list")]
    CellListResult List(IVisioBatch batch, int pageIndex, string shapeName);
}
