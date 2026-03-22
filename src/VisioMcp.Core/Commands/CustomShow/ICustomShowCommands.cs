using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.CustomShow;

/// <summary>
/// Legacy PowerPoint-only custom slide show management retained from the bootstrap template.
/// </summary>
[ServiceCategory("customshow")]
[McpTool("customshow", Title = "Legacy PowerPoint Custom Shows", Destructive = true, Category = "customshow", PublicSurface = false,
    Description = "Legacy PowerPoint-only surface retained during the Visio migration. Not Visio-native. "
    + "If you still use this legacy surface: create, list, delete custom slide shows (curated subsets of slides). "
    + "slide_indices: comma-separated 1-based slide numbers (e.g. '1,3,5,8'). "
    + "Use slideshow(start) to play a custom show.")]
public interface ICustomShowCommands
{
    /// <summary>List all custom shows in the presentation.</summary>
    [ServiceAction("list")]
    CustomShowListResult List(IVisioBatch batch);

    /// <summary>Create a custom show from specified slide indices.</summary>
    /// <param name="batch">Batch context</param>
    /// <param name="showName">Name for the custom show</param>
    /// <param name="slideIndices">Comma-separated 1-based slide indices (e.g. "1,3,5")</param>
    [ServiceAction("create")]
    OperationResult Create(IVisioBatch batch, string showName, string slideIndices);

    /// <summary>Delete a custom show by name.</summary>
    [ServiceAction("delete")]
    OperationResult Delete(IVisioBatch batch, string showName);
}
