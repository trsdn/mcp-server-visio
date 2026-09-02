using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Window;

/// <summary>
/// Visio window management for drawing windows: state, zoom, viewport, navigation, visual aids, and snap settings.
/// </summary>
[ServiceCategory("window")]
[McpTool("window", Title = "Window Operations", Destructive = false, Category = "window",
    Description = "Control the active Visio drawing window: visibility, zoom, viewport bounds, page navigation, visual aids, and snap strengths. "
    + "Use get-info/get-viewport/get-zoom to inspect the current drawing window, set-zoom to change magnification, "
    + "fit-page to fit the full page, pan-to-shape to center on a shape, pan-by-offset for viewport nudging, "
    + "get-visual-aids for grid/guide/ruler visibility, and get-snap-settings plus set-*-snap-strength actions to control Visio snapping categories. "
    + "Legacy get-view/set-view actions remain for compatibility and are not Visio-native.")]
public interface IWindowCommands
{
    /// <summary>
    /// Get current window information (state, position, size).
    /// </summary>
    [ServiceAction("get-info")]
    WindowInfoResult GetInfo(IVisioBatch batch);

    /// <summary>
    /// Minimize the Visio window.
    /// </summary>
    [ServiceAction("minimize")]
    OperationResult Minimize(IVisioBatch batch);

    /// <summary>
    /// Restore the Visio window to normal size.
    /// </summary>
    [ServiceAction("restore")]
    OperationResult Restore(IVisioBatch batch);

    /// <summary>
    /// Maximize the Visio window.
    /// </summary>
    [ServiceAction("maximize")]
    OperationResult Maximize(IVisioBatch batch);

    /// <summary>
    /// Set the zoom level of the active view (percentage).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="zoomPercent">Zoom percentage (e.g. 100 for 100%)</param>
    [ServiceAction("set-zoom")]
    OperationResult SetZoom(IVisioBatch batch, int zoomPercent);

    /// <summary>
    /// Get the current zoom level of the drawing window for a page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("get-zoom")]
    WindowViewportResult GetZoom(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Get the current viewport rectangle of the drawing window for a page.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("get-viewport")]
    WindowViewportResult GetViewport(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Fit the drawing page into the active Visio window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("fit-page")]
    OperationResult FitPage(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Fit the current selection into the active Visio window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("fit-selection")]
    OperationResult FitSelection(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Center the drawing window on a specific shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape to centre in the drawing window, as reported by shape(list)</param>
    [ServiceAction("pan-to-shape")]
    OperationResult PanToShape(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Move the drawing viewport by the given offsets in points.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="offsetX">Horizontal pan distance in points. Positive moves the view right</param>
    /// <param name="offsetY">Vertical pan distance in points. Positive moves the view up</param>
    [ServiceAction("pan-by-offset")]
    OperationResult PanByOffset(IVisioBatch batch, int pageIndex, float offsetX, float offsetY);

    /// <summary>
    /// Get drawing-aid visibility for the active Visio drawing window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    [ServiceAction("get-visual-aids")]
    WindowVisualAidsResult GetVisualAids(IVisioBatch batch, int pageIndex);

    /// <summary>
    /// Show or hide the grid in the active drawing window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="visible">True to show the grid</param>
    [ServiceAction("set-grid-visible")]
    OperationResult SetGridVisible(IVisioBatch batch, int pageIndex, bool visible);

    /// <summary>
    /// Show or hide guides in the active drawing window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="visible">True to show guides</param>
    [ServiceAction("set-guides-visible")]
    OperationResult SetGuidesVisible(IVisioBatch batch, int pageIndex, bool visible);

    /// <summary>
    /// Show or hide rulers in the active drawing window.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="visible">True to show rulers</param>
    [ServiceAction("set-rulers-visible")]
    OperationResult SetRulersVisible(IVisioBatch batch, int pageIndex, bool visible);

    /// <summary>
    /// Enable or disable Visio drawing aids globally for the application.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="enabled">True to enable snap and glue</param>
    [ServiceAction("set-drawing-aids")]
    OperationResult SetDrawingAids(IVisioBatch batch, bool enabled);

    /// <summary>
    /// Get current Visio snap strengths for the application.
    /// </summary>
    [ServiceAction("get-snap-settings")]
    WindowSnapSettingsResult GetSnapSettings(IVisioBatch batch);

    /// <summary>
    /// Set grid snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to the grid from further away</param>
    [ServiceAction("set-grid-snap-strength")]
    OperationResult SetGridSnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Set guides snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to guides from further away</param>
    [ServiceAction("set-guides-snap-strength")]
    OperationResult SetGuidesSnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Set points snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to connection and vertex points from further away</param>
    [ServiceAction("set-points-snap-strength")]
    OperationResult SetPointsSnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Set ruler snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to ruler subdivisions from further away</param>
    [ServiceAction("set-ruler-snap-strength")]
    OperationResult SetRulerSnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Set geometry snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to shape geometry from further away</param>
    [ServiceAction("set-geometry-snap-strength")]
    OperationResult SetGeometrySnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Set extensions snap strength.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="strength">Snap strength from 1 to 999. Higher values pull shapes to extension lines from further away</param>
    [ServiceAction("set-extensions-snap-strength")]
    OperationResult SetExtensionsSnapStrength(IVisioBatch batch, int strength);

    /// <summary>
    /// Switch the drawing window view. Visio has a single drawing view, so this is a no-op retained for callers that set it.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="viewType">1=Normal, 2=Outline, 3=SlideSorter, 4=NotesPage, 5=SlideMaster</param>
    [ServiceAction("set-view")]
    OperationResult SetView(IVisioBatch batch, int viewType);

    /// <summary>
    /// Report the current drawing window view.
    /// </summary>
    [ServiceAction("get-view")]
    OperationResult GetView(IVisioBatch batch);
}