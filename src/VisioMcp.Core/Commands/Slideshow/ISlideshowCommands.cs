using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Slideshow;

/// <summary>
/// Legacy PowerPoint-only slideshow presentation mode commands retained from the bootstrap template.
/// </summary>
[ServiceCategory("slideshow")]
[McpTool("slideshow", Title = "Legacy PowerPoint Slideshow Operations", Destructive = false, Category = "slideshow", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "Prefer page/shape/text/cell/stencil for Visio workflows. "
    + "If you still use this legacy surface: control presentation slideshow mode: start, stop, navigate, configure. "
    + "show_type for configure: 1=Speaker (fullscreen), 2=Browsed by individual (window), 3=Kiosk (loop). "
    + "Use 'start' with start_slide (1-based, 0=beginning). "
    + "'goto-slide' navigates during active show. 'get-status' checks if show is running.")]
public interface ISlideshowCommands
{
    /// <summary>
    /// Start the slideshow from a specific slide.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="startSlide">1-based slide to start from (0 = beginning)</param>
    [ServiceAction("start")]
    OperationResult Start(IVisioBatch batch, int startSlide);

    /// <summary>
    /// Stop/end the running slideshow.
    /// </summary>
    [ServiceAction("stop")]
    OperationResult EndShow(IVisioBatch batch);

    /// <summary>
    /// Navigate to a specific slide in the running slideshow.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based target slide index</param>
    [ServiceAction("goto-slide")]
    OperationResult GotoSlide(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Get the current slideshow status.
    /// </summary>
    [ServiceAction("get-status")]
    SlideshowInfoResult GetStatus(IVisioBatch batch);

    /// <summary>
    /// Configure slideshow settings (show type, looping, animation, narration).
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="showType">1=Speaker (full screen), 2=Browsed by individual (window), 3=Browsed at kiosk (loop)</param>
    /// <param name="loopUntilStopped">Whether to loop the slideshow continuously</param>
    /// <param name="showWithAnimation">Whether to show animations during the slideshow</param>
    /// <param name="showWithNarration">Whether to play narrations during the slideshow</param>
    [ServiceAction("configure")]
    OperationResult Configure(IVisioBatch batch, int showType, bool loopUntilStopped, bool showWithAnimation, bool showWithNarration);
}
