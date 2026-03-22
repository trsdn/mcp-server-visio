using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Stencil;

/// <summary>
/// Visio stencil operations for listing masters and dropping them onto pages.
/// </summary>
[ServiceCategory("stencil")]
[McpTool("stencil", Title = "Stencil Operations", Destructive = true, Category = "stencils",
    Description = "List masters from a Visio stencil file and drop a master onto a page. "
    + "WORKFLOW: stencil(list-masters, stencil_path='...BASIC_M.VSSX') → stencil(drop-master, master_name='Rectangle'). "
    + "Coordinates are in points. page_index is 1-based.")]
public interface IStencilCommands
{
    /// <summary>
    /// List masters in a stencil file.
    /// </summary>
    [ServiceAction("list-masters")]
    StencilMasterListResult ListMasters(IVisioBatch batch, string stencilPath);

    /// <summary>
    /// Drop a master from a stencil file onto a page.
    /// </summary>
    [ServiceAction("drop-master")]
    OperationResult DropMaster(IVisioBatch batch, int pageIndex, string stencilPath, string masterName, float xPosition, float yPosition);
}
