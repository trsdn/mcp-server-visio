using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Hyperlink;

/// <summary>
/// Hyperlinks attached to shapes.
/// </summary>
/// <remarks>
/// The modelling differs from PowerPoint's in a way that shapes this whole interface: in Visio a
/// shape carries a *collection* of hyperlinks, stored as rows in its Hyperlink ShapeSheet section,
/// not the single click-action PowerPoint attaches. Every action therefore identifies a hyperlink
/// by its row name.
///
/// Previously backed by PowerPoint <c>ActionSettings</c>/<c>ppActionHyperlink</c> and suppressed in
/// #19 because every action threw <c>RuntimeBinderException</c> on a Visio document. Reimplemented
/// against <c>Shape.Hyperlinks</c> in #35.
/// </remarks>
[ServiceCategory("hyperlink")]
[McpTool("hyperlink", Title = "Hyperlink Operations", Destructive = true, Category = "content",
    Description = "Manage hyperlinks on shapes. "
    + "A Visio shape can carry SEVERAL hyperlinks — they are rows in the shape's Hyperlink ShapeSheet "
    + "section — so every action identifies one by hyperlink_name: the row name such as 'Row_1', or "
    + "whatever name you pass to add. "
    + "WORKFLOW: shape(list) → hyperlink(add, shape_name='Rectangle', address='https://…') → hyperlink(list). "
    + "TARGETS: address is an external target (https://…, mailto:…, or a file path). "
    + "sub_address navigates inside the document — a page name such as 'Page-2', or 'Page-2/Rectangle' "
    + "for a specific shape. Set address empty and sub_address alone for a purely internal link. "
    + "description is the text Visio displays for the link. "
    + "Hyperlinks are shape-level only: Visio has no page or document hyperlink collection.")]
public interface IHyperlinkCommands
{
    /// <summary>
    /// List every hyperlink in the document, across all pages and shapes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list")]
    HyperlinkListResult List(IVisioBatch batch);

    /// <summary>
    /// List the hyperlinks on one shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    [ServiceAction("list-for-shape")]
    HyperlinkListResult ListForShape(IVisioBatch batch, int pageIndex, string shapeName);

    /// <summary>
    /// Read one hyperlink on a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="hyperlinkName">Hyperlink row name, exactly as reported by list</param>
    [ServiceAction("read")]
    HyperlinkResult Read(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName);

    /// <summary>
    /// Add a hyperlink to a shape. A shape may carry several.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="address">External target such as 'https://example.com', 'mailto:a@b.com' or a file path. Leave empty for a link that only navigates inside the document</param>
    /// <param name="subAddress">Target inside the document: a page name such as 'Page-2', or 'Page-2/Rectangle' for a shape</param>
    /// <param name="description">Text Visio displays for the link, shown on hover</param>
    /// <param name="hyperlinkName">Optional name for the new row, used to address it later. Visio assigns 'Row_1', 'Row_2' when omitted</param>
    /// <param name="newWindow">Open the target in a new window</param>
    [ServiceAction("add")]
    HyperlinkResult Add(IVisioBatch batch, int pageIndex, string shapeName, string? address = null, string? subAddress = null, string? description = null, string? hyperlinkName = null, bool newWindow = false);

    /// <summary>
    /// Change an existing hyperlink. Omitted values are left as they are.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="hyperlinkName">Hyperlink row name, exactly as reported by list</param>
    /// <param name="address">New external target, or omit to leave it unchanged</param>
    /// <param name="subAddress">New in-document target, or omit to leave it unchanged</param>
    /// <param name="description">New display text, or omit to leave it unchanged</param>
    /// <param name="newWindow">Open the target in a new window, or omit to leave it unchanged</param>
    [ServiceAction("update")]
    HyperlinkResult Update(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName, string? address = null, string? subAddress = null, string? description = null, bool? newWindow = null);

    /// <summary>
    /// Delete one hyperlink from a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Shape name, exactly as reported by shape(list)</param>
    /// <param name="hyperlinkName">Hyperlink row name, exactly as reported by list</param>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName);
}
