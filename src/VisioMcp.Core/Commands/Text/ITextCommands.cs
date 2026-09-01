using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Text;

/// <summary>
/// Text operations within shapes: get, set, find, replace, word count.
/// </summary>
[ServiceCategory("text")]
[McpTool("text", Title = "Text Operations", Destructive = true, Category = "text",
    Description = "Get, set, find, and replace the text of Visio shapes. "
    + "WORKFLOW: shape(add-textbox) → text(set). "
    + "'find'/'replace' work across pages (page_index=0 for all pages). "
    + "Text formatting is not part of this tool: use the 'cell' tool to write the "
    + "Character and Paragraph ShapeSheet rows (for example Char.Font, Char.Size, "
    + "Char.Style, Char.Color, Para.HorzAlign) on the shape.")]
public interface ITextCommands
{
    /// <summary>
    /// Get text content from a shape including paragraph and run details.
    /// </summary>
    [ServiceAction("get")]
    TextResult GetText(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Set the text content of a shape (replaces all existing text).
    /// </summary>
    [ServiceAction("set")]
    OperationResult SetText(IVisioBatch batch, int pageIndex, string shapeName, string text);

    /// <summary>
    /// Find text across all shapes on a page or across the entire document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="searchText">Text to find</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("find")]
    OperationResult Find(IVisioBatch batch, string searchText, int pageIndex);

    /// <summary>
    /// Replace text across all shapes on a page or across the entire document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="searchText">Text to find</param>
    /// <param name="replaceText">Replacement text</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("replace")]
    OperationResult Replace(IVisioBatch batch, string searchText, string replaceText, int pageIndex);

    /// <summary>
    /// Count words across all pages or on a specific page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">0 for all pages, or a specific 1-based page index</param>
    [ServiceAction("word-count")]
    OperationResult WordCount(IVisioBatch batch, int pageIndex);
}
