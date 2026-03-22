using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Comment;

/// <summary>
/// Legacy PowerPoint-only slide comments commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("comment")]
[McpTool("comment", Title = "Legacy PowerPoint Slide Comments", Destructive = true, Category = "comments", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: add, list, delete comments on slides. "
    + "slide_index: 1-based (0 = all slides for list/clear). "
    + "comment_index: 1-based (from list results) for delete. "
    + "left/top: position in points (0 = top-left corner).")]
public interface ICommentCommands
{
    /// <summary>List all comments on a slide (0 = all slides).</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index, or 0 for all slides</param>
    [ServiceAction("list")]
    CommentListResult List(IVisioBatch batch, int slideIndex);

    /// <summary>Add a comment to a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="text">Comment text</param>
    /// <param name="author">Author name</param>
    /// <param name="left">Horizontal position in points (0 = top-left)</param>
    /// <param name="top">Vertical position in points (0 = top-left)</param>
    [ServiceAction("add")]
    OperationResult Add(IVisioBatch batch, int slideIndex, string text, string author, float left, float top);

    /// <summary>Delete a comment by index on a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="commentIndex">1-based comment index</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int slideIndex, int commentIndex);

    /// <summary>Delete all comments on a slide (0 = all slides).</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index, or 0 for all slides</param>
    [ServiceAction("clear")]
    OperationResult Clear(IVisioBatch batch, int slideIndex);
}
