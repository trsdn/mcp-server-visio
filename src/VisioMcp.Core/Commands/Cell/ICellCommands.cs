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
    + "Value returns the evaluated internal-unit result; formula returns the raw ShapeSheet expression. "
    + "SECTIONS: named cells like Prop.Cost live in ShapeSheet sections. Use list-sections to see which a shape has, "
    + "add-row to create one (add-row with section='Prop', row_name='Cost' creates Prop.Cost), list-rows to enumerate, "
    + "and delete-row to remove. section accepts a name (Prop, User, Connections, Actions, Hyperlink, Geometry1, Char, Para) "
    + "or a numeric index. "
    + "read-src/write-src address a cell by section+row+column, which is the only way to reach a row that has no name, "
    + "such as a connection point.")]
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

    /// <summary>
    /// List the ShapeSheet sections present on a shape, with the number of rows in each.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    [ServiceAction("list-sections")]
    ShapeSheetSectionListResult ListSections(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// List the rows of one ShapeSheet section, with each row's name and first cell name.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="section">Section name such as Prop, User, Connections, Actions or Hyperlink, or a numeric section index. Use list-sections to see what a shape has</param>
    [ServiceAction("list-rows")]
    ShapeSheetRowListResult ListRows(IVisioBatch batch, int pageIndex, string shapeName, string? section = null);

    /// <summary>
    /// Add a row to a ShapeSheet section, creating the section if it does not exist.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="section">Section name such as Prop, User, Connections or Actions, or a numeric section index</param>
    /// <param name="rowName">Optional row name. Named rows become addressable cells such as Prop.Cost or User.Origin; omit it for sections whose rows are positional, such as Connections</param>
    [ServiceAction("add-row")]
    ShapeSheetRowResult AddRow(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, string? rowName = null);

    /// <summary>
    /// Delete one row from a ShapeSheet section by its 0-based index.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="section">Section name or numeric section index</param>
    /// <param name="rowIndex">0-based row index within the section. Rows below it shift up, so delete from the highest index first when removing several</param>
    [ServiceAction("delete-row")]
    OperationResult DeleteRow(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0);

    /// <summary>
    /// Read a cell addressed by section, row and column rather than by name.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="section">Section name or numeric section index</param>
    /// <param name="rowIndex">0-based row index within the section</param>
    /// <param name="columnIndex">0-based column index within the row. This is the only way to reach a row that has no name</param>
    [ServiceAction("read-src")]
    CellResult ReadSrc(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0);

    /// <summary>
    /// Write a formula to a cell addressed by section, row and column.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="section">Section name or numeric section index</param>
    /// <param name="rowIndex">0-based row index within the section</param>
    /// <param name="columnIndex">0-based column index within the row</param>
    /// <param name="formula">ShapeSheet expression to write, for example 'Width*0.5'. Distance values need explicit units such as '3 in'; a bare number is read in internal units</param>
    [ServiceAction("write-src")]
    OperationResult WriteSrc(IVisioBatch batch, int pageIndex, string shapeName, string? section = null, int rowIndex = 0, int columnIndex = 0, string formula = "");
}
