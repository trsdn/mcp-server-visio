using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Attributes;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Design;

/// <summary>
/// Diagram design guidance: which diagram to draw, which stencil and masters to drop, and the
/// Visio techniques that separate a usable drawing from a plausible-looking one.
/// </summary>
/// <remarks>
/// Every action is a catalog lookup and needs no open document, so guidance can be fetched before
/// a session exists.
///
/// The catalog previously described PowerPoint slide design — archetypes such as
/// <c>big-number</c> and <c>title-slide</c>, text-density profiles and deck narrative sequences.
/// None of that has a diagram meaning. It was rebuilt for Visio in #98, and every stencil and
/// master it names was verified present on a stock install rather than assumed.
/// </remarks>
[ServiceCategory("design")]
[McpTool("design", Title = "Diagram Design Guidance", Destructive = false, Category = "design",
    Description = "Diagram design guidance, queried on demand instead of reading full documents. "
    + "No open document is needed — call this BEFORE building to choose the right diagram and the "
    + "right stencil. "
    + "WORKFLOW: design(list-archetypes) → design(get-archetype, archetype_id='flowchart') → "
    + "stencil(drop-master, stencil_path='BASFLO_M.VSSX', master_name='Decision') → "
    + "shape(connect-shapes). "
    + "list-archetypes/get-archetype: nine diagram families — flowchart, cross-functional-flowchart, "
    + "bpmn-process, org-chart, network-diagram, system-context, block-diagram, fault-tree, "
    + "annotated-diagram — each naming the stencil, the masters to drop, layout spacing and the "
    + "mistakes that make a diagram look right while being wrong. "
    + "get-stencil-catalog: every stencil and master verified present on a stock Visio install, plus "
    + "the ones that are NOT installed by default and need a fallback. "
    + "get-diagram-patterns: layers, background pages, shape data, masters, styles and routing. "
    + "list-palettes/get-palette: colour palettes with hex values for use in FillForegnd formulas. "
    + "THEMES ARE NOT HERE: Document.Theme does not exist in Visio. Use "
    + "cell(sheet_target='document', cell_name='ThemeIndex').")]
public interface IDesignCommands
{
    /// <summary>
    /// List the diagram archetypes, with the stencil and masters each one uses.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list-archetypes")]
    ArchetypeListResult ListArchetypes(IVisioBatch batch);

    /// <summary>
    /// Get one archetype: its stencil, masters, layout spacing, build order and anti-patterns.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="archetypeId">Archetype id, exactly as reported by list-archetypes</param>
    [ServiceAction("get-archetype")]
    ArchetypeDetailResult GetArchetype(IVisioBatch batch, string archetypeId);

    /// <summary>
    /// Get the catalog of stencils and masters verified present on a stock Visio install.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("get-stencil-catalog")]
    DesignReferenceResult GetStencilCatalog(IVisioBatch batch);

    /// <summary>
    /// Get the Visio techniques that apply across archetypes: layers, background pages, shape data,
    /// masters, styles, routing and verification.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("get-diagram-patterns")]
    DesignReferenceResult GetDiagramPatterns(IVisioBatch batch);

    /// <summary>
    /// List the colour palettes.
    /// </summary>
    /// <param name="batch">Batch context</param>
    [ServiceAction("list-palettes")]
    PaletteListResult ListPalettes(IVisioBatch batch);

    /// <summary>
    /// Get one colour palette with its hex values.
    /// </summary>
    /// <param name="batch">Batch context</param>
    /// <param name="paletteId">Palette id, exactly as reported by list-palettes</param>
    [ServiceAction("get-palette")]
    PaletteDetailResult GetPalette(IVisioBatch batch, string paletteId);
}
