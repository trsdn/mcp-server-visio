using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Style;

/// <summary>
/// Named formatting held by the document.
/// </summary>
/// <remarks>
/// A Visio style is a reusable named format covering fill, line and text, applied to shapes by
/// name. Changing the style restyles every shape using it, which is the point.
///
/// A style carries its own ShapeSheet, so its appearance is set with the same cell names used on
/// shapes — <c>FillForegnd</c>, <c>LineWeight</c>, <c>Char.Size</c>. That is why this tool has
/// <c>set-formula</c> rather than a fixed set of formatting parameters.
///
/// Themes are deliberately not here: <c>Document.Theme</c> does not exist (#22). Theme selection
/// lives in the <c>ThemeIndex</c> and <c>VariationColorIndex</c> cells on the DocumentSheet,
/// reachable with <c>cell(sheet_target='document')</c> since #36b.
/// </remarks>
[ServiceCategory("style")]
[McpTool("style", Title = "Style Operations", Destructive = true, Category = "formatting",
    Description = "Manage the document's named styles — reusable formatting applied to shapes by name. "
    + "Changing a style restyles every shape using it, which is the reason to use one. "
    + "WORKFLOW: style(create, style_name='Callout', based_on='Normal') → "
    + "style(set-formula, style_name='Callout', cell_name='FillForegnd', formula='RGB(200,30,30)') → "
    + "style(apply, page_index=1, shape_name='Rectangle', style_name='Callout'). "
    + "A style has its own ShapeSheet, so set-formula takes the same cell names used on shapes: "
    + "FillForegnd, FillBkgnd, LineColor, LineWeight, LinePattern, Char.Size, Char.Color, Char.Style. "
    + "APPLYING: apply sets fill, line and text together by default. Pass aspect='fill' | 'line' | 'text' "
    + "to apply only one, which lets a shape combine three different styles. "
    + "A style only accepts writes and applications for the aspects it carries — create a style with "
    + "includes_fill=true before setting FillForegnd on it, or the operation is rejected. "
    + "A blank document already has: No Style, Text Only, None, Normal, Guide, Theme. "
    + "DELETE IS QUIET: shapes using a deleted style silently revert to 'No Style' and lose that "
    + "formatting, so delete reports how many were affected. "
    + "THEMES ARE NOT HERE: Document.Theme does not exist in Visio. Use "
    + "cell(sheet_target='document', cell_name='ThemeIndex' | 'VariationColorIndex').")]
public interface IStyleCommands
{
    /// <summary>
    /// List the styles defined in the document.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list")]
    StyleListResult List(IVisioBatch batch);

    /// <summary>
    /// Read one style: what it is based on and which aspects it carries.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    [ServiceAction("read")]
    StyleDetailResult Read(IVisioBatch batch, string styleName);

    /// <summary>
    /// Create a style.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Name for the new style. Must not already be in use</param>
    /// <param name="basedOn">Style to inherit from, such as 'Normal'. Omit for a style that inherits nothing</param>
    /// <param name="includesFill">Whether the style carries fill formatting</param>
    /// <param name="includesLine">Whether the style carries line formatting</param>
    /// <param name="includesText">Whether the style carries text formatting</param>
    [ServiceAction("create")]
    StyleDetailResult Create(IVisioBatch batch, string styleName, string? basedOn = null, bool includesFill = true, bool includesLine = true, bool includesText = true);

    /// <summary>
    /// Rename a style. Shapes using it keep their formatting.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    /// <param name="newName">New name for the style</param>
    [ServiceAction("rename")]
    StyleDetailResult Rename(IVisioBatch batch, string styleName, string newName);

    /// <summary>
    /// Delete a style. Shapes using it revert to 'No Style' and lose that formatting.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, string styleName);

    /// <summary>
    /// Read one cell from a style's own ShapeSheet.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    /// <param name="cellName">ShapeSheet cell name, for example FillForegnd, LineWeight or Char.Size</param>
    [ServiceAction("read-formula")]
    StyleCellResult ReadFormula(IVisioBatch batch, string styleName, string cellName);

    /// <summary>
    /// Set one cell on a style's own ShapeSheet, restyling every shape that uses it.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    /// <param name="cellName">ShapeSheet cell name, for example FillForegnd, LineWeight or Char.Size</param>
    /// <param name="formula">ShapeSheet expression, for example 'RGB(200,30,30)' or '3 pt'</param>
    [ServiceAction("set-formula")]
    StyleCellResult SetFormula(IVisioBatch batch, string styleName, string cellName, string formula);

    /// <summary>
    /// Apply a style to a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="styleName">Style name, exactly as reported by list</param>
    /// <param name="aspect">Which part to apply: 'all' (default), or 'fill', 'line' or 'text' to apply only that one</param>
    [ServiceAction("apply")]
    OperationResult Apply(IVisioBatch batch, int pageIndex, string shapeName, string styleName, string? aspect = null);
}
