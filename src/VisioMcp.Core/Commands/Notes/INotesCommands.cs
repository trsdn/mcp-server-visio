using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Notes;

/// <summary>
/// Legacy PowerPoint-only speaker notes commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("notes")]
[McpTool("notes", Title = "Legacy PowerPoint Speaker Notes", Destructive = true, Category = "notes", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: get, set, clear, or append speaker notes per slide. "
    + "Use 'read-all' to get notes from every slide at once. "
    + "'append' adds text with a newline separator to existing notes. "
    + "Useful for building presenter scripts alongside slide creation.")]
public interface INotesCommands
{
    /// <summary>Get speaker notes for a slide.</summary>
    [ServiceAction("get")]
    NotesResult GetNotes(IVisioBatch batch, int slideIndex);

    /// <summary>Set speaker notes for a slide.</summary>
    [ServiceAction("set")]
    OperationResult SetNotes(IVisioBatch batch, int slideIndex, string text);

    /// <summary>Clear speaker notes for a slide.</summary>
    [ServiceAction("clear")]
    OperationResult Clear(IVisioBatch batch, int slideIndex);

    /// <summary>Append text to existing speaker notes (adds newline separator).</summary>
    [ServiceAction("append")]
    OperationResult Append(IVisioBatch batch, int slideIndex, string text);

    /// <summary>Read speaker notes from all slides in the presentation.</summary>
    [ServiceAction("read-all")]
    OperationResult ReadAll(IVisioBatch batch);
}
