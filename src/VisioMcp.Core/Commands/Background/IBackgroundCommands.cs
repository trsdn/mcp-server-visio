using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Background;

/// <summary>
/// Slide background: get, set solid color, set image, reset to master.
/// </summary>
[ServiceCategory("background")]
[McpTool("background", Title = "Slide Background", Destructive = true, Category = "background", PublicSurface = false,
    Description = "Get, set, or reset slide backgrounds. Supports solid color, gradient, and image backgrounds. "
    + "color_hex: '#RRGGBB' format (e.g. '#0B3D91' for navy). Use 'reset' to revert to slide master background. "
    + "gradient_style: 1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown, 5=FromCorner, 6=FromCenter.")]
public interface IBackgroundCommands
{
    /// <summary>Get the current background info for a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("get")]
    BackgroundResult GetInfo(IVisioBatch batch, int slideIndex);

    /// <summary>Set a solid color background for a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="colorHex">Hex color string (#RRGGBB)</param>
    [ServiceAction("set-color")]
    OperationResult SetColor(IVisioBatch batch, int slideIndex, string colorHex);

    /// <summary>Reset a slide background to follow the slide master.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    [ServiceAction("reset")]
    OperationResult Reset(IVisioBatch batch, int slideIndex);

    /// <summary>Set an image as slide background.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="imagePath">Path to the image file</param>
    [ServiceAction("set-image")]
    OperationResult SetImage(IVisioBatch batch, int slideIndex, string imagePath);

    /// <summary>Set a two-color gradient background for a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="color1">First gradient color as hex (#RRGGBB)</param>
    /// <param name="color2">Second gradient color as hex (#RRGGBB)</param>
    /// <param name="gradientStyle">1=Horizontal, 2=Vertical, 3=DiagonalUp, 4=DiagonalDown, 5=FromCorner, 6=FromCenter</param>
    [ServiceAction("set-gradient")]
    OperationResult SetGradient(IVisioBatch batch, int slideIndex, string color1, string color2, int gradientStyle);
}
