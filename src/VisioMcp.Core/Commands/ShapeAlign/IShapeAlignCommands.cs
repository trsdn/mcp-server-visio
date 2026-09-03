using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.ShapeAlign;

/// <summary>
/// Shape alignment and distribution operations.
/// </summary>
[ServiceCategory("shapealign")]
[McpTool("shapealign", Title = "Shape Alignment", Destructive = true, Category = "shapealign",
    Description = "Align and distribute multiple shapes on a page. "
    + "shape_names: comma-separated shape names (e.g. 'Title 1,TextBox 3,Rectangle 2'). "
    + "align_type: 0=AlignLeft, 1=AlignCenter, 2=AlignRight, 3=AlignTop, 4=AlignMiddle, 5=AlignBottom. "
    + "distribute_type: 0=Horizontally, 1=Vertically. Requires 3+ shapes for distribute.")]
public interface IShapeAlignCommands
{
    /// <summary>
    /// Align shapes on a page.
    /// alignType: 0=AlignLeft, 1=AlignCenter, 2=AlignRight, 3=AlignTop, 4=AlignMiddle, 5=AlignBottom
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated shape names</param>
    /// <param name="alignType">Alignment type (0-5)</param>
    [ServiceAction("align")]
    OperationResult Align(IVisioBatch batch, int pageIndex, string shapeNames, int alignType);

    /// <summary>
    /// Distribute shapes evenly on a page.
    /// distributeType: 0=Horizontally, 1=Vertically
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated shape names</param>
    /// <param name="distributeType">0=Horizontally, 1=Vertically</param>
    [ServiceAction("distribute")]
    OperationResult Distribute(IVisioBatch batch, int pageIndex, string shapeNames, int distributeType);
}
