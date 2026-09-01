using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Hyperlink;

/// <summary>
/// Legacy PowerPoint hyperlink operations.
///
/// SUPPRESSED (#19): every action is implemented against PowerPoint COM (<c>Slides</c>,
/// <c>ActionSettings</c>, <c>ppActionHyperlink</c>) and throws <c>RuntimeBinderException</c> on a
/// Visio Document. Visio models hyperlinks as rows in a shape's Hyperlink ShapeSheet section, and
/// one shape can carry several. Kept off the public surface until reimplemented against
/// <c>Shape.Hyperlinks</c> (#35).
/// </summary>
[ServiceCategory("hyperlink")]
[McpTool("hyperlink", Title = "Hyperlink Operations", Destructive = true, Category = "content",
    PublicSurface = false,
    Description = "Legacy PowerPoint hyperlink operations. Not exposed: throws on Visio documents. "
    + "Pending reimplementation against Shape.Hyperlinks (#35).")]
public interface IHyperlinkCommands
{
    /// <summary>
    /// Add a hyperlink to a shape (click on shape navigates to URL or slide).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the shape to add hyperlink to</param>
    /// <param name="address">URL (https://...) or empty for slide link</param>
    /// <param name="subAddress">Slide number for internal links (e.g. '3' to jump to slide 3), or empty</param>
    /// <param name="screenTip">Optional tooltip text shown on hover</param>
    [ServiceAction("add")]
    OperationResult Add(IVisioBatch batch, int slideIndex, string shapeName, string address, string? subAddress = null, string? screenTip = null);

    /// <summary>
    /// Get the hyperlink on a shape.
    /// </summary>
    [ServiceAction("get")]
    HyperlinkResult Read(IVisioBatch batch, int slideIndex, string shapeName);

    /// <summary>
    /// Remove the hyperlink from a shape.
    /// </summary>
    [ServiceAction("remove")]
    OperationResult Remove(IVisioBatch batch, int slideIndex, string shapeName);

    /// <summary>
    /// List all hyperlinks in the presentation.
    /// </summary>
    [ServiceAction("list")]
    HyperlinkListResult List(IVisioBatch batch);

    /// <summary>
    /// Validate all hyperlinks in the presentation and report broken or empty ones.
    /// Checks every slide and shape for hyperlinks, classifying each as valid, broken, empty, external, or internal.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("validate")]
    OperationResult Validate(IVisioBatch batch);
}
