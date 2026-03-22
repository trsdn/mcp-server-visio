using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.SlideTable;

/// <summary>
/// Legacy PowerPoint-only table-on-slide operations retained from the bootstrap template.
/// </summary>
[ServiceCategory("slidetable")]
[McpTool("slidetable", Title = "Legacy PowerPoint Table Operations", Destructive = true, Category = "tables", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: create and edit table shapes on slides. "
    + "WORKFLOW: slidetable(create, rows=4, columns=3) → slidetable(write-cell) or slidetable(write-row). "
    + "All row/column indices are 1-based. position=-1 means 'at end' for add-row/add-column. "
    + "format-cell: fill_color='#RRGGBB', text_align='left'/'center'/'right'. "
    + "merge-cells: specify start_row/start_column/end_row/end_column rectangle. "
    + "Positions in points (72pt = 1 inch).")]
public interface ISlideTableCommands
{
    /// <summary>
    /// Create a table shape on a slide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="rows">Number of rows</param>
    /// <param name="columns">Number of columns</param>
    /// <param name="left">Position from left in points</param>
    /// <param name="top">Position from top in points</param>
    /// <param name="width">Width in points</param>
    /// <param name="height">Height in points</param>
    [ServiceAction("create")]
    OperationResult Create(IVisioBatch batch, int slideIndex, int rows, int columns, float left, float top, float width, float height);

    /// <summary>
    /// Read all data from a table shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    [ServiceAction("read")]
    SlideTableResult Read(IVisioBatch batch, int slideIndex, string shapeName);

    /// <summary>
    /// Write a value to a specific table cell.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    /// <param name="column">1-based column index</param>
    /// <param name="value">Cell value to set</param>
    [ServiceAction("write-cell")]
    OperationResult WriteCell(IVisioBatch batch, int slideIndex, string shapeName, int row, int column, string value);

    /// <summary>
    /// Add a row to the table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="position">1-based position to insert (-1 = at end)</param>
    [ServiceAction("add-row")]
    OperationResult AddRow(IVisioBatch batch, int slideIndex, string shapeName, int position);

    /// <summary>
    /// Add a column to the table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="position">1-based position to insert (-1 = at end)</param>
    [ServiceAction("add-column")]
    OperationResult AddColumn(IVisioBatch batch, int slideIndex, string shapeName, int position);

    /// <summary>
    /// Delete a row from the table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index to delete</param>
    [ServiceAction("delete-row")]
    OperationResult DeleteRow(IVisioBatch batch, int slideIndex, string shapeName, int row);

    /// <summary>
    /// Delete a column from the table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="column">1-based column index to delete</param>
    [ServiceAction("delete-column")]
    OperationResult DeleteColumn(IVisioBatch batch, int slideIndex, string shapeName, int column);

    /// <summary>
    /// Merge a range of cells in a table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="startRow">1-based start row</param>
    /// <param name="startColumn">1-based start column</param>
    /// <param name="endRow">1-based end row</param>
    /// <param name="endColumn">1-based end column</param>
    [ServiceAction("merge-cells")]
    OperationResult MergeCells(IVisioBatch batch, int slideIndex, string shapeName, int startRow, int startColumn, int endRow, int endColumn);

    /// <summary>
    /// Read the text value of a specific table cell.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    /// <param name="column">1-based column index</param>
    [ServiceAction("read-cell")]
    OperationResult ReadCell(IVisioBatch batch, int slideIndex, string shapeName, int row, int column);

    /// <summary>
    /// Set formatting on a table cell (fill color, text alignment).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    /// <param name="column">1-based column index</param>
    /// <param name="fillColor">Hex fill color (#RRGGBB) or null to skip</param>
    /// <param name="fontBold">Set bold (null = don't change)</param>
    /// <param name="fontSize">Set font size (0 = don't change)</param>
    /// <param name="textAlign">Text alignment: left, center, right (null = don't change)</param>
    [ServiceAction("format-cell")]
    OperationResult FormatCell(IVisioBatch batch, int slideIndex, string shapeName, int row, int column, string? fillColor, bool? fontBold, float fontSize, string? textAlign);

    /// <summary>
    /// Write values to an entire row in a table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    /// <param name="values">Comma-separated values for the row</param>
    [ServiceAction("write-row")]
    OperationResult WriteRow(IVisioBatch batch, int slideIndex, string shapeName, int row, string values);

    /// <summary>
    /// Read all cell values from a specific row in a table.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    [ServiceAction("read-row")]
    OperationResult ReadRow(IVisioBatch batch, int slideIndex, string shapeName, int row);

    /// <summary>
    /// Set all four borders of a table cell to the same color and width.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the table shape</param>
    /// <param name="row">1-based row index</param>
    /// <param name="column">1-based column index</param>
    /// <param name="colorHex">Border color as hex (#RRGGBB)</param>
    /// <param name="width">Border width in points</param>
    [ServiceAction("set-cell-border")]
    OperationResult SetCellBorder(IVisioBatch batch, int slideIndex, string shapeName, int row, int column, string colorHex, float width);
}
