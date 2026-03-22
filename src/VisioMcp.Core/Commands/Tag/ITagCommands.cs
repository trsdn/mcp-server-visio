using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Tag;

/// <summary>
/// Custom tags/metadata on slides and shapes.
/// </summary>
[ServiceCategory("tag")]
[McpTool("tag", Title = "Tags & Metadata", Destructive = true, Category = "tags", PublicSurface = false,
    Description = "Set custom key-value metadata tags on slides or shapes. "
    + "shape_name: null/empty = slide-level tag. tag_name is case-insensitive. "
    + "Tags persist with the file and can be used for filtering, automation, or custom workflows.")]
public interface ITagCommands
{
    /// <summary>List all tags on a slide or shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Shape name (null/empty = slide-level tags)</param>
    [ServiceAction("list")]
    TagListResult List(IVisioBatch batch, int slideIndex, string? shapeName);

    /// <summary>Set a tag value on a slide or shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Shape name (null/empty = slide-level tag)</param>
    /// <param name="tagName">Tag name (case-insensitive)</param>
    /// <param name="tagValue">Tag value</param>
    [ServiceAction("set")]
    OperationResult SetTag(IVisioBatch batch, int slideIndex, string? shapeName, string tagName, string tagValue);

    /// <summary>Delete a tag from a slide or shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Shape name (null/empty = slide-level tag)</param>
    /// <param name="tagName">Tag name to delete</param>
    [ServiceAction("delete")]
    OperationResult DeleteTag(IVisioBatch batch, int slideIndex, string? shapeName, string tagName);
}
