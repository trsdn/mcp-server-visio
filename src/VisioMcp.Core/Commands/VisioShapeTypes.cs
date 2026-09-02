namespace VisioMcp.Core.Commands;

/// <summary>
/// Visio's <c>Shape.Type</c> values and their names.
/// </summary>
/// <remarks>
/// This replaces the PowerPoint-era <c>ShapeHelpers.GetShapeTypeName</c>, which mapped
/// <c>MsoShapeType</c> and was therefore wrong for every Visio shape it was handed. The failure
/// was silent and plausible rather than loud: a drawn rectangle reports <c>3</c>, which the old
/// table named <c>"Chart"</c>; a group reports <c>2</c>, named <c>"Callout"</c>; an imported
/// image reports <c>4</c>, named <c>"Comment"</c>. Callers got a confident, wrong answer.
///
/// Values confirmed against a live Visio 16.0 instance in #20 and #22.
/// </remarks>
internal static class VisioShapeTypes
{
    /// <summary>Not a valid shape.</summary>
    internal const int Invalid = 0;

    /// <summary>A page (returned by <c>Page.PageSheet</c>).</summary>
    internal const int Page = 1;

    /// <summary>A grouped shape.</summary>
    internal const int Group = 2;

    /// <summary>An ordinary drawn or dropped shape.</summary>
    internal const int Shape = 3;

    /// <summary>An embedded or linked OLE object, including imported images.</summary>
    internal const int ForeignObject = 4;

    /// <summary>A guide line or guide point.</summary>
    internal const int Guide = 5;

    /// <summary>A document (returned by <c>Document.DocumentSheet</c>).</summary>
    internal const int Document = 6;

    /// <summary>
    /// Maps a Visio <c>Shape.Type</c> value to its name.
    /// </summary>
    /// <param name="shapeType">A <c>VisShapeTypes</c> value, as returned by <c>Shape.Type</c>.</param>
    /// <returns>The type name, or <c>Unknown(n)</c> for a value Visio does not document.</returns>
    internal static string GetName(int shapeType) => shapeType switch
    {
        Invalid => "Invalid",
        Page => "Page",
        Group => "Group",
        Shape => "Shape",
        ForeignObject => "ForeignObject",
        Guide => "Guide",
        Document => "Document",
        _ => $"Unknown({shapeType})"
    };
}
