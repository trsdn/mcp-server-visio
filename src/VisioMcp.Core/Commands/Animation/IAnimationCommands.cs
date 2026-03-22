using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Animation;

/// <summary>
/// Legacy PowerPoint-only animation effect operations retained from the bootstrap template.
/// </summary>
[ServiceCategory("animation")]
[McpTool("animation", Title = "Legacy PowerPoint Animation Operations", Destructive = true, Category = "animations", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "Prefer page/shape/text/cell/stencil for Visio workflows. "
    + "If you still use this legacy surface: add, remove, reorder animation effects on shapes. "
    + "effect_type (MsoAnimEffect): 1=Appear, 2=Fly, 10=Fade, 16=Wipe, 22=RandomBars, 26=Dissolve, 53=GrowAndTurn. "
    + "trigger_type: 1=OnClick (default), 2=WithPrevious, 3=AfterPrevious. "
    + "WORKFLOW: shape(add-shape) → animation(add) → animation(set-timing). Use animation(list) to see current effects.")]
public interface IAnimationCommands
{
    /// <summary>
    /// List all animation effects on a slide.
    /// </summary>
    [ServiceAction("list")]
    AnimationListResult List(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Add an animation effect to a shape.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="shapeName">Name of the target shape</param>
    /// <param name="effectType">MsoAnimEffect integer (e.g., 1=Appear, 2=Fly, 10=Fade, 16=Wipe)</param>
    /// <param name="triggerType">1=OnClick (default), 2=WithPrevious, 3=AfterPrevious</param>
    [ServiceAction("add")]
    OperationResult Add(IVisioBatch batch, int slideIndex, string shapeName, int effectType, int triggerType);

    /// <summary>
    /// Remove an animation effect by its 1-based index in the animation sequence.
    /// </summary>
    [ServiceAction("remove")]
    OperationResult Remove(IVisioBatch batch, int slideIndex, int effectIndex);

    /// <summary>
    /// Remove all animation effects from a slide.
    /// </summary>
    [ServiceAction("clear")]
    OperationResult Clear(IVisioBatch batch, int slideIndex);

    /// <summary>
    /// Set timing properties for an animation effect.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="effectIndex">1-based index of the effect in the animation sequence</param>
    /// <param name="duration">Duration in seconds</param>
    /// <param name="delay">Delay before start in seconds</param>
    /// <param name="triggerType">1=OnClick, 2=WithPrevious, 3=AfterPrevious</param>
    [ServiceAction("set-timing")]
    OperationResult SetTiming(IVisioBatch batch, int slideIndex, int effectIndex, float duration, float delay, int triggerType);

    /// <summary>
    /// Reorder an animation effect by moving it to a new position in the sequence.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="slideIndex">1-based slide index</param>
    /// <param name="effectIndex">1-based index of the effect to move</param>
    /// <param name="newIndex">1-based target position in the sequence</param>
    [ServiceAction("reorder")]
    OperationResult Reorder(IVisioBatch batch, int slideIndex, int effectIndex, int newIndex);
}
