using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Transition;

/// <summary>
/// Legacy PowerPoint-only slide transition effects retained from the bootstrap template.
/// </summary>
[ServiceCategory("transition")]
[McpTool("transition", Title = "Legacy PowerPoint Slide Transitions", Destructive = true, Category = "animation", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "Prefer page/shape/text/cell/stencil for Visio workflows. "
    + "If you still use this legacy surface: set slide transition effects (between slides, not shape animations). "
    + "transition_type (PpEntryEffect): 3844=Fade, 3849=Push, 3851=Wipe, 3850=Cover, 3855=Split, "
    + "3856=Random, 3847=Dissolve, 3852=Wheel. duration: seconds (e.g. 0.5-2.0). "
    + "advance_on_click: bool. advance_after_time: seconds (0=disabled, for kiosk mode). "
    + "'copy-to-all' applies one slide's transition to every slide.")]
public interface ITransitionCommands
{
    /// <summary>Get the transition settings for a slide.</summary>
    [ServiceAction("get")]
    TransitionResult GetTransition(IVisioBatch batch, int slideIndex);

    /// <summary>Set a transition effect on a slide.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="transitionType">PpEntryEffect enum value (e.g. 3844=Fade, 3849=Push)</param>
    /// <param name="duration">Duration in seconds</param>
    /// <param name="advanceOnClick">Whether to advance on mouse click</param>
    /// <param name="advanceAfterTime">Auto-advance after N seconds (0 = disabled)</param>
    [ServiceAction("set")]
    OperationResult SetTransition(IVisioBatch batch, int slideIndex, int transitionType, float duration, bool advanceOnClick, float advanceAfterTime);

    /// <summary>Remove transition from a slide.</summary>
    [ServiceAction("remove")]
    OperationResult Remove(IVisioBatch batch, int slideIndex);

    /// <summary>Copy the transition from one slide to all other slides.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based index of the source slide</param>
    [ServiceAction("copy-to-all")]
    OperationResult CopyToAll(IVisioBatch batch, int slideIndex);
}
