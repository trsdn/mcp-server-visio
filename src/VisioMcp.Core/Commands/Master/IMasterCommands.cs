using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Master;

/// <summary>
/// Masters held inside the working document.
/// </summary>
/// <remarks>
/// A Visio master is a reusable shape definition. Every shape dropped from a stencil is an
/// *instance* of one, sharing its geometry, so a drawing with two hundred instances of one master
/// stores that geometry once. This is what governs both reuse and file size.
///
/// Deliberately distinct from the <c>stencil</c> tool: <c>stencil</c> reads masters out of an
/// external <c>.vssx</c> file and drops them, whereas this operates on the masters the working
/// document already owns.
///
/// Previously backed by PowerPoint <c>SlideMasters</c>/<c>CustomLayouts</c> and suppressed in #19
/// because every action threw <c>RuntimeBinderException</c> on a Visio document. Reimplemented
/// against <c>Document.Masters</c> in #34.
/// </remarks>
[ServiceCategory("master")]
[McpTool("master", Title = "Document Master Operations", Destructive = true, Category = "stencils",
    Description = "Manage the master shape definitions stored inside the open document. "
    + "A master is a reusable shape definition; every shape dropped from a stencil is an instance of one, "
    + "which is why a drawing with many identical shapes stays small. "
    + "WORKFLOW: master(list) → master(list-instances, master_name='Rectangle') to find what uses it. "
    + "DISTINCT FROM stencil: the stencil tool reads masters out of an external .vssx file and drops them; "
    + "this tool operates on masters the document already owns. A blank document has none — masters appear "
    + "when a stencil shape is dropped, or via create-from-shape. "
    + "create-from-shape turns an existing page shape into a reusable master, which is the only way to define "
    + "one without a stencil file. "
    + "delete removes the definition but LEAVES existing instances on their pages intact.")]
public interface IMasterCommands
{
    /// <summary>
    /// List the masters stored in the document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list")]
    MasterListResult List(IVisioBatch batch);

    /// <summary>
    /// Read one master's identity and contents.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterName">Master name, exactly as reported by list</param>
    [ServiceAction("read")]
    MasterDetailResult Read(IVisioBatch batch, string masterName);

    /// <summary>
    /// Turn an existing shape on a page into a reusable master.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index holding the shape to promote</param>
    /// <param name="shapeName">Shape to copy into the document's masters. The shape itself is left where it is</param>
    /// <param name="masterName">Optional name for the new master. Visio assigns one such as 'Master.4' when omitted</param>
    [ServiceAction("create-from-shape")]
    MasterDetailResult CreateFromShape(IVisioBatch batch, int pageIndex, string shapeName, string? masterName = null);

    /// <summary>
    /// Rename a master.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterName">Master name, exactly as reported by list</param>
    /// <param name="newName">New name for the master</param>
    [ServiceAction("rename")]
    MasterDetailResult Rename(IVisioBatch batch, string masterName, string newName);

    /// <summary>
    /// Delete a master definition from the document. Existing instances on pages are left intact.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterName">Master name, exactly as reported by list</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, string masterName);

    /// <summary>
    /// Find every shape across the document that is an instance of a master.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="masterName">Master name, exactly as reported by list</param>
    [ServiceAction("list-instances")]
    MasterInstanceListResult ListInstances(IVisioBatch batch, string masterName);
}
