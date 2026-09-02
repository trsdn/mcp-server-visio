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
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("read")]
    PageDetailResult Read(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Create a new page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="position">1-based insertion position. Pass 0 to append the page at the end</param>
    /// <param name="name">Page name. Must be unique within the document</param>
    [ServiceAction("create")]
    OperationResult Create(IVisioBatch batch, int position, string name);

    /// <summary>
    /// Set the visible page name.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="name">New page name. Must be unique within the document</param>
    [ServiceAction("set-name")]
    OperationResult SetName(IVisioBatch batch, int pageIndex, string name);

    /// <summary>
    /// Delete a page by index.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// List page guides.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("list-guides")]
    PageGuideListResult ListGuides(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Add a guide to a page. guideType values: 1=point, 2=horizontal, 3=vertical.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="guideType">1 = point, 2 = horizontal guide line, 3 = vertical guide line</param>
    /// <param name="xPosition">Guide X position in points. Ignored for a horizontal guide</param>
    /// <param name="yPosition">Guide Y position in points. Ignored for a vertical guide. Visio measures Y upward from the bottom of the page</param>
    [ServiceAction("add-guide")]
    OperationResult AddGuide(IVisioBatch batch, int pageIndex, int guideType, float xPosition, float yPosition);

    /// <summary>
    /// Reposition an existing guide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="guideName">Guide shape name, as reported by list-guides</param>
    /// <param name="xPosition">New X position in points</param>
    /// <param name="yPosition">New Y position in points, measured upward from the bottom of the page</param>
    [ServiceAction("set-guide-position")]
    OperationResult SetGuidePosition(IVisioBatch batch, int pageIndex, string guideName, float xPosition, float yPosition);

    /// <summary>
    /// Delete a guide by shape name.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="guideName">Guide shape name, as reported by list-guides</param>
    [ServiceAction("delete-guide")]
    OperationResult DeleteGuide(IVisioBatch batch, int pageIndex, string guideName);

    /// <summary>
    /// Read page-level connector routing and line jump settings from the page sheet.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("get-routing-settings")]
    PageRoutingSettingsResult GetRoutingSettings(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Set the page route style (`RouteStyle` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="routeStyle">Connector routing style, written to the page RouteStyle cell. Controls whether connectors run at right angles, straight, or along a tree or flowchart layout. Read the current value with get-routing-settings before changing it</param>
    [ServiceAction("set-route-style")]
    OperationResult SetRouteStyle(IVisioBatch batch, int pageIndex, int routeStyle);

    /// <summary>
    /// Set the connector routing extension (`ConLineRouteExt` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="connectorRoutingExtension">Routing extension, written to the page ConLineRouteExt cell. Selects whether connectors are drawn with straight or curved segments</param>
    [ServiceAction("set-connector-routing-extension")]
    OperationResult SetConnectorRoutingExtension(IVisioBatch batch, int pageIndex, int connectorRoutingExtension);

    /// <summary>
    /// Set the line jump code (`LineJumpCode` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="lineJumpCode">Which connectors draw a jump where lines cross, written to the page LineJumpCode cell</param>
    [ServiceAction("set-line-jump-code")]
    OperationResult SetLineJumpCode(IVisioBatch batch, int pageIndex, int lineJumpCode);

    /// <summary>
    /// Set the line jump style (`LineJumpStyle` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="lineJumpStyle">Shape of the jump drawn where connectors cross, written to the page LineJumpStyle cell</param>
    [ServiceAction("set-line-jump-style")]
    OperationResult SetLineJumpStyle(IVisioBatch batch, int pageIndex, int lineJumpStyle);

    /// <summary>
    /// Set the route walk preference (`WalkPreference` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="walkPreference">Which side of a shape a connector prefers to leave from, written to the page WalkPreference cell</param>
    [ServiceAction("set-walk-preference")]
    OperationResult SetWalkPreference(IVisioBatch batch, int pageIndex, int walkPreference);

    /// <summary>
    /// Set the shape placement style (`PlaceStyle` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="placeStyle">Automatic layout style used when shapes are placed, written to the page PlaceStyle cell</param>
    [ServiceAction("set-place-style")]
    OperationResult SetPlaceStyle(IVisioBatch batch, int pageIndex, int placeStyle);
}