using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Image;

/// <summary>
/// Image operations: insert and adjust imported pictures on Visio pages.
/// </summary>
[ServiceCategory("image")]
[McpTool("image", Title = "Image Operations", Destructive = true, Category = "media", PublicSurface = true,
    Description = "Insert and adjust imported bitmap images on Visio pages. Positions and sizes use "
    + "Visio page coordinates in points (72 pt = 1 inch; Y increases upward); width/height 0 keeps "
    + "the original import size. Crop values are points hidden beyond each picture edge while preserving "
    + "the shape frame: left/right/top/bottom expand the underlying image, and left/bottom use negative "
    + "ImgOffsetX/Y. Brightness and contrast are Visio percentage fractions from 0.0 to 1.0; 0.5 is "
    + "neutral/unchanged. Do not pass PowerPoint-style 0 as neutral: in Visio 0 means 0% (fully dark/"
    + "minimum contrast). Visio has no color-key transparent color action.")]
public interface IImageCommands
{
    /// <summary>Insert a picture from a file path onto a Visio page.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="left">Visio page X coordinate in points (72 pt = 1 inch)</param>
    /// <param name="top">Visio page Y coordinate in points; Y increases upward</param>
    /// <param name="width">Width in points (0 = original)</param>
    /// <param name="height">Height in points (0 = original)</param>
    [ServiceAction("insert")]
    OperationResult Insert(IVisioBatch batch, int pageIndex, string imagePath, float left, float top, float width, float height);

    /// <summary>
    /// Crop an imported image shape by hiding point amounts beyond each edge while preserving the
    /// shape frame.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the picture shape</param>
    /// <param name="cropLeft">Points hidden beyond the left edge (0 = no left crop)</param>
    /// <param name="cropRight">Points hidden beyond the right edge (0 = no right crop)</param>
    /// <param name="cropTop">Points hidden beyond the top edge (0 = no top crop)</param>
    /// <param name="cropBottom">Points hidden beyond the bottom edge (0 = no bottom crop)</param>
    [ServiceAction("crop")]
    OperationResult Crop(IVisioBatch batch, int pageIndex, string shapeName, float cropLeft, float cropRight, float cropTop, float cropBottom);

    /// <summary>Set brightness and contrast on an imported image shape.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="pageIndex">1-based page index</param>
    /// <param name="shapeName">Name of the picture shape</param>
    /// <param name="brightness">Visio percentage fraction (0.0 to 1.0); 0.5 is neutral/unchanged</param>
    /// <param name="contrast">Visio percentage fraction (0.0 to 1.0); 0.5 is neutral/unchanged</param>
    [ServiceAction("set-brightness-contrast")]
    OperationResult SetBrightnessContrast(IVisioBatch batch, int pageIndex, string shapeName, float brightness, float contrast);
}
