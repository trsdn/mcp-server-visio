using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Page;

/// <summary>
/// Visio page lifecycle, guides, and routing commands.
/// </summary>
[ServiceCategory("page")]
[McpTool("page", Title = "Page Operations", Destructive = true, Category = "pages",
    Description = "List, inspect, create, rename, and delete Visio pages, manage page guides, and control page-level routing and line jump settings. "
    + "WORKFLOW: file(open) → page(create, name='Overview') → shape(add-shape) → text(set). "
    + "All page indices are 1-based. position=0 means append at end.")]
public interface IPageCommands
{
    /// <summary>
    /// List all pages in the active Visio document.
    /// </summary>
    [ServiceAction("list")]
    PageListResult List(IVisioBatch batch);

    /// <summary>
    /// Read one page and summarize its shapes.
    /// </summary>
    [ServiceAction("read")]
    PageDetailResult Read(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Create a new page.
    /// </summary>
    [ServiceAction("create")]
    OperationResult Create(IVisioBatch batch, int position, string name);

    /// <summary>
    /// Set the visible page name.
    /// </summary>
    [ServiceAction("set-name")]
    OperationResult SetName(IVisioBatch batch, int pageIndex, string name);

    /// <summary>
    /// Delete a page by index.
    /// </summary>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// List page guides.
    /// </summary>
    [ServiceAction("list-guides")]
    PageGuideListResult ListGuides(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Add a guide to a page. guideType values: 1=point, 2=horizontal, 3=vertical.
    /// </summary>
    [ServiceAction("add-guide")]
    OperationResult AddGuide(IVisioBatch batch, int pageIndex, int guideType, float xPosition, float yPosition);

    /// <summary>
    /// Reposition an existing guide.
    /// </summary>
    [ServiceAction("set-guide-position")]
    OperationResult SetGuidePosition(IVisioBatch batch, int pageIndex, string guideName, float xPosition, float yPosition);

    /// <summary>
    /// Delete a guide by shape name.
    /// </summary>
    [ServiceAction("delete-guide")]
    OperationResult DeleteGuide(IVisioBatch batch, int pageIndex, string guideName);

    /// <summary>
    /// Read page-level connector routing and line jump settings from the page sheet.
    /// </summary>
    [ServiceAction("get-routing-settings")]
    PageRoutingSettingsResult GetRoutingSettings(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Set the page route style (`RouteStyle` page sheet cell).
    /// </summary>
    [ServiceAction("set-route-style")]
    OperationResult SetRouteStyle(IVisioBatch batch, int pageIndex, int routeStyle);

    /// <summary>
    /// Set the connector routing extension (`ConLineRouteExt` page sheet cell).
    /// </summary>
    [ServiceAction("set-connector-routing-extension")]
    OperationResult SetConnectorRoutingExtension(IVisioBatch batch, int pageIndex, int connectorRoutingExtension);

    /// <summary>
    /// Set the line jump code (`LineJumpCode` page sheet cell).
    /// </summary>
    [ServiceAction("set-line-jump-code")]
    OperationResult SetLineJumpCode(IVisioBatch batch, int pageIndex, int lineJumpCode);

    /// <summary>
    /// Set the line jump style (`LineJumpStyle` page sheet cell).
    /// </summary>
    [ServiceAction("set-line-jump-style")]
    OperationResult SetLineJumpStyle(IVisioBatch batch, int pageIndex, int lineJumpStyle);

    /// <summary>
    /// Set the route walk preference (`WalkPreference` page sheet cell).
    /// </summary>
    [ServiceAction("set-walk-preference")]
    OperationResult SetWalkPreference(IVisioBatch batch, int pageIndex, int walkPreference);

    /// <summary>
    /// Set the shape placement style (`PlaceStyle` page sheet cell).
    /// </summary>
    [ServiceAction("set-place-style")]
    OperationResult SetPlaceStyle(IVisioBatch batch, int pageIndex, int placeStyle);
}
