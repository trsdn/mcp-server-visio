using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Comment;

/// <summary>
/// Visio reviewer comments attached to pages or shapes.
/// </summary>
[ServiceCategory("comment")]
[McpTool("comment", Title = "Reviewer Comments", Destructive = true, Category = "comments", PublicSurface = true,
    Description = "Add, list, delete, and clear Visio reviewer comments on a page. "
    + "Pass shape_name to add a shape-attached reviewer comment or to filter list results to one shape. "
    + "Visio reviewer comments are separate from the ShapeSheet Comment cell used for accessibility alt text; "
    + "use shape(set-alt-text/read-alt-text) for that field. "
    + "Document.Comments is deliberately not exposed here; this tool is page-scoped so indexes match page-level list results.")]
public interface ICommentCommands
{
    /// <summary>List reviewer comments on a page, optionally filtered to one shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Optional shape name used to return only comments attached to that shape</param>
    [ServiceAction("list")]
    CommentListResult List(IVisioBatch batch, int pageIndex, string? shapeName = null);

    /// <summary>Add a reviewer comment to a page or shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="text">Comment text</param>
    /// <param name="shapeName">Optional shape name; omitted means add a page-level comment</param>
    [ServiceAction("add")]
    OperationResult Add(IVisioBatch batch, int pageIndex, string text, string? shapeName = null);

    /// <summary>Delete a reviewer comment by its 1-based page comment index.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="commentIndex">1-based index from page-level list results</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex, int commentIndex);

    /// <summary>Delete all reviewer comments on a page.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("clear")]
    OperationResult Clear(IVisioBatch batch, int pageIndex);
}
