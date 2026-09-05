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
    + "AUTO-LAYOUT WARNING: layout-page, layout-selection, incremental-layout, and change-layout-direction move shapes. "
    + "Use them only when the user asked to tidy a diagram or accepted automatic placement; they can discard deliberate manual positioning. "
    + "Prefer shape(connect-shapes) when you only need dynamic connectors between existing shapes; those connectors reroute as shapes move. "
    + "layout-selection is the safer tidy-up default because it moves only the named selected shapes; layout-page relayouts the whole page. "
    + "LAYOUT STYLE VALUES: place_style 0=default, 1=top-to-bottom, 2=left-to-right, 3=radial, 4=bottom-to-top, 5=right-to-left, 6=circular, 7=compact down-right, 8=compact right-down, 9=compact right-up, 10=compact up-right, 11=compact up-left, 12=compact left-up, 13=compact left-down, 14=compact down-left, 15=parent default, 16-27=hierarchy variants. "
    + "route_style 0=default, 1=right-angle, 2=straight, 3=org chart NS, 4=org chart WE, 5=flowchart NS, 6=flowchart WE, 7=tree NS, 8=tree WE, 9=network, 10=org chart SN, 11=org chart EW, 12=flowchart SN, 13=flowchart EW, 14=tree SN, 15=tree EW, 16=center-to-center, 17=simple NS, 18=simple WE, 19=simple SN, 20=simple EW, 21=simple HV, 22=simple VH. "
    + "line_route_extension 0=default straight, 1=straight, 2=curved. place_depth 0=default, 1=medium, 2=deep, 3=shallow. "
    + "layout direction values: 0=rotate right, 1=rotate left, 2=flip vertical, 3=flip horizontal. "
    + "incremental layout values: align_or_space 1=align, 2=space, 3=align and space; align_horizontal/align_vertical 0=none, 1=default, 2=left/top, 3=center/middle, 4=right/bottom. "
    + "WORKFLOW: file(open) → page(create, name='Overview') → shape(add-shape) → text(set). "
    + "All page indices are 1-based. position=0 means append at end. "
    + "BACKGROUND PAGES: a Visio background page is a normal page marked as a background and then shown behind "
    + "other pages — shared furniture such as a title block or logo, drawn once. Two steps, in this order: "
    + "page(set-background, is_background=true) on the page holding the furniture, then "
    + "page(set-back-page, back_page_name='…') on each page that should show it. "
    + "A page that is not itself a background CANNOT be used as one. clear-back-page detaches it and keeps the page. "
    + "PAGE SIZE AND SCALE are not here: they are PageSheet cells, reached with cell(read/set-formula, "
    + "sheet_target='page', cell_name='PageWidth' | 'PageHeight' | 'DrawingScale' | 'PageScale').")]
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
    /// Destructively relayout every shape and reroute connectors on a page using the page's layout ShapeSheet settings.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("layout-page")]
    PageLayoutResult LayoutPage(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Destructively relayout only the named selected shapes using the page's layout ShapeSheet settings.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Comma-separated top-level shape names to layout as a selection</param>
    [ServiceAction("layout-selection")]
    PageLayoutResult LayoutSelection(IVisioBatch batch, int pageIndex, string shapeNames);

    /// <summary>
    /// Make small alignment and/or spacing adjustments without a full relayout.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeNames">Optional comma-separated shape names. Omitted applies to the whole page; provided applies to only those selected shapes</param>
    /// <param name="alignOrSpace">1=align, 2=space, 3=align and space</param>
    /// <param name="alignHorizontal">0=none, 1=default, 2=left, 3=center, 4=right</param>
    /// <param name="alignVertical">0=none, 1=default, 2=top, 3=middle, 4=bottom</param>
    /// <param name="spaceHorizontal">Horizontal edge-to-edge spacing in points. Must be non-negative</param>
    /// <param name="spaceVertical">Vertical edge-to-edge spacing in points. Must be non-negative</param>
    [ServiceAction("incremental-layout")]
    PageLayoutResult IncrementalLayout(IVisioBatch batch, int pageIndex, string? shapeNames = null, int alignOrSpace = 3, int alignHorizontal = 1, int alignVertical = 1, float spaceHorizontal = 36f, float spaceVertical = 36f);

    /// <summary>
    /// Rotate or flip a connected diagram's layout without rotating or flipping the individual shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="direction">0=rotate right, 1=rotate left, 2=flip vertical, 3=flip horizontal</param>
    [ServiceAction("change-layout-direction")]
    PageLayoutResult ChangeLayoutDirection(IVisioBatch batch, int pageIndex, int direction = 0);

    /// <summary>
    /// Temporarily enable or disable Visio's advanced dynamic connector routing on the page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="passive">True disables advanced routing; false enables normal dynamic routing. Default Visio behavior is false</param>
    [ServiceAction("set-passive-routing")]
    PageRoutingSettingsResult SetPassiveRouting(IVisioBatch batch, int pageIndex, bool passive = false);

    /// <summary>
    /// Set the page route style (`RouteStyle` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="routeStyle">RouteStyle cell value: 0=default, 1=right-angle, 2=straight, 3=org chart NS, 4=org chart WE, 5=flowchart NS, 6=flowchart WE, 7=tree NS, 8=tree WE, 9=network, 10=org chart SN, 11=org chart EW, 12=flowchart SN, 13=flowchart EW, 14=tree SN, 15=tree EW, 16=center-to-center, 17=simple NS, 18=simple WE, 19=simple SN, 20=simple EW, 21=simple HV, 22=simple VH</param>
    [ServiceAction("set-route-style")]
    OperationResult SetRouteStyle(IVisioBatch batch, int pageIndex, int routeStyle);

    /// <summary>
    /// Set the connector routing extension (`ConLineRouteExt` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="connectorRoutingExtension">ConLineRouteExt cell value: 0=default straight, 1=straight, 2=curved</param>
    [ServiceAction("set-connector-routing-extension")]
    OperationResult SetConnectorRoutingExtension(IVisioBatch batch, int pageIndex, int connectorRoutingExtension);

    /// <summary>
    /// Set the page line route extension (`LineRouteExt` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="lineRouteExtension">0=default straight, 1=straight, 2=curved</param>
    [ServiceAction("set-line-route-extension")]
    OperationResult SetLineRouteExtension(IVisioBatch batch, int pageIndex, int lineRouteExtension = 0);

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
    /// <param name="placeStyle">PlaceStyle cell value: 0=default, 1=top-to-bottom, 2=left-to-right, 3=radial, 4=bottom-to-top, 5=right-to-left, 6=circular, 7=compact down-right, 8=compact right-down, 9=compact right-up, 10=compact up-right, 11=compact up-left, 12=compact left-up, 13=compact left-down, 14=compact down-left, 15=parent default, 16=hierarchy top-bottom left, 17=hierarchy top-bottom center, 18=hierarchy top-bottom right, 19=hierarchy bottom-top left, 20=hierarchy bottom-top center, 21=hierarchy bottom-top right, 22=hierarchy left-right top, 23=hierarchy left-right middle, 24=hierarchy left-right bottom, 25=hierarchy right-left top, 26=hierarchy right-left middle, 27=hierarchy right-left bottom</param>
    [ServiceAction("set-place-style")]
    OperationResult SetPlaceStyle(IVisioBatch batch, int pageIndex, int placeStyle);

    /// <summary>
    /// Set the automatic layout spacing (`AvenueSizeX` and `AvenueSizeY` page sheet cells).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="avenueSizeX">Horizontal shape spacing in points</param>
    /// <param name="avenueSizeY">Vertical shape spacing in points</param>
    [ServiceAction("set-layout-spacing")]
    PageRoutingSettingsResult SetLayoutSpacing(IVisioBatch batch, int pageIndex, float avenueSizeX = 21.2598f, float avenueSizeY = 21.2598f);

    /// <summary>
    /// Set automatic layout analysis depth (`PlaceDepth` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="placeDepth">0=default, 1=medium, 2=deep, 3=shallow</param>
    [ServiceAction("set-place-depth")]
    PageRoutingSettingsResult SetPlaceDepth(IVisioBatch batch, int pageIndex, int placeDepth = 0);

    /// <summary>
    /// Set whether layout may enlarge the page to enclose the diagram (`ResizePage` page sheet cell).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="resizePage">True lets Visio enlarge the page after layout; false keeps the page size</param>
    [ServiceAction("set-resize-page")]
    PageRoutingSettingsResult SetResizePage(IVisioBatch batch, int pageIndex, bool resizePage = false);

    /// <summary>
    /// Read whether a page is a background, and which background page it shows behind itself.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("read-background")]
    PageBackgroundResult ReadBackground(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Make a page a background page, or turn it back into an ordinary one.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="isBackground">True to make it a background page, false to turn it back into a normal page</param>
    [ServiceAction("set-background")]
    PageBackgroundResult SetBackground(IVisioBatch batch, int pageIndex, bool isBackground);

    /// <summary>
    /// Show a background page behind this page. The target must already be a background page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index of the page that will show the background</param>
    /// <param name="backPageName">Name of the background page to show behind it, exactly as reported by list</param>
    [ServiceAction("set-back-page")]
    PageBackgroundResult SetBackPage(IVisioBatch batch, int pageIndex, string backPageName);

    /// <summary>
    /// Stop showing a background page behind this page. The background page itself is kept.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("clear-back-page")]
    PageBackgroundResult ClearBackPage(IVisioBatch batch, int pageIndex);
}