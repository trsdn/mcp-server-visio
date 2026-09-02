using System.Globalization;

namespace VisioMcp.Core.Commands;

/// <summary>
/// Visio ShapeSheet section indices and the cell-name prefix each one produces.
/// </summary>
/// <remarks>
/// Every value here was read back from a live Visio 16.0 instance rather than taken from
/// documentation: a named row was added to each candidate index and the resulting
/// <c>Cell.Name</c> recorded. That mattered — the widely-repeated mapping of Actions to 238 and
/// Character to 4 is wrong; they are 240 and 3.
///
/// Before #33 these constants were private and duplicated. <c>VisSectionProp = 243</c> was declared
/// separately in <c>ShapeCommands</c> and <c>DocumentPropertyCommands</c>, <c>LayerCommands</c>
/// carried its own <c>VisSectionLayer</c>, and <c>ShapeCommands</c> wrote <c>AddRow(1, 20, 0)</c>
/// with no constant at all.
/// </remarks>
internal static class ShapeSheetSections
{
    /// <summary>Shape Data rows — <c>Prop.&lt;name&gt;</c>.</summary>
    internal const int Property = 243;

    /// <summary>User-defined cells — <c>User.&lt;name&gt;</c>.</summary>
    internal const int User = 242;

    /// <summary>Connection points — <c>Connections.&lt;name&gt;</c>.</summary>
    internal const int ConnectionPoints = 7;

    /// <summary>Layer membership — <c>Layers.&lt;cell&gt;</c>.</summary>
    internal const int Layer = 241;

    /// <summary>Right-click actions — <c>Actions.&lt;name&gt;</c>.</summary>
    internal const int Action = 240;

    /// <summary>Shape protection and object behaviour — <c>Sect1.&lt;cell&gt;</c>.</summary>
    internal const int Object = 1;

    /// <summary>Row index meaning "append after the last row" (<c>visRowLast</c>).</summary>
    internal const int RowLast = -2;

    /// <summary>Default row tag (<c>visTagDefault</c>).</summary>
    internal const int TagDefault = 0;

    /// <summary>
    /// Section names an agent may use, mapped to their index. The name matches the prefix Visio
    /// puts on the resulting cell, so <c>Prop</c> produces <c>Prop.Cost</c>.
    /// </summary>
    private static readonly Dictionary<string, int> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Object"] = 1,
        ["Members"] = 2,
        ["Char"] = 3,
        ["Character"] = 3,
        ["Para"] = 4,
        ["Paragraph"] = 4,
        ["Tabs"] = 5,
        ["Scratch"] = 6,
        ["Connections"] = 7,
        ["Fields"] = 8,
        ["Controls"] = 9,
        ["Geometry1"] = 10,
        ["Geometry2"] = 11,
        ["Actions"] = 240,
        ["Layers"] = 241,
        ["User"] = 242,
        ["Prop"] = 243,
        ["Property"] = 243,
        ["Hyperlink"] = 244,
        ["Reviewer"] = 245,
        ["Annotation"] = 246,
        ["SmartTags"] = 247
    };

    /// <summary>Section names in a stable order, for listing and error messages.</summary>
    internal static IReadOnlyList<(string Name, int Index)> Known { get; } =
    [
        ("Object", 1),
        ("Members", 2),
        ("Char", 3),
        ("Para", 4),
        ("Tabs", 5),
        ("Scratch", 6),
        ("Connections", 7),
        ("Fields", 8),
        ("Controls", 9),
        ("Geometry1", 10),
        ("Geometry2", 11),
        ("Actions", 240),
        ("Layers", 241),
        ("User", 242),
        ("Prop", 243),
        ("Hyperlink", 244),
        ("Reviewer", 245),
        ("Annotation", 246),
        ("SmartTags", 247)
    ];

    /// <summary>
    /// Resolves a section name such as <c>Prop</c>, or a raw index such as <c>243</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The value is neither a known name nor an integer.</exception>
    internal static int Resolve(string section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var trimmed = section.Trim();

        if (ByName.TryGetValue(trimmed, out var index))
        {
            return index;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw))
        {
            return raw;
        }

        throw new ArgumentException(
            $"Unknown ShapeSheet section '{section}'. Use a name ({string.Join(", ", Known.Select(k => k.Name))}) "
            + "or a numeric section index.",
            nameof(section));
    }

    /// <summary>Returns the display name for a section index, or the index itself when unknown.</summary>
    internal static string GetName(int index)
    {
        foreach (var (name, value) in Known)
        {
            if (value == index)
            {
                return name;
            }
        }

        return index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Whether a section is present on a sheet.
    /// </summary>
    /// <remarks>
    /// <c>SectionExists</c> returns a VBA <c>short</c> (0 or -1), not a <c>bool</c>. Casting it
    /// directly throws <c>RuntimeBinderException: Cannot convert type 'short' to 'bool'</c> — the
    /// same trap <c>CellExistsU</c> sets, which broke five actions in #20.
    /// </remarks>
    internal static bool SectionExists(dynamic sheet, int section)
    {
        try
        {
            return Convert.ToInt32(sheet.SectionExists(section, 0)) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Row count for a section, or 0 when the section is absent.</summary>
    internal static int RowCount(dynamic sheet, int section)
    {
        try
        {
            return Convert.ToInt32(sheet.RowCount(section));
        }
        catch
        {
            return 0;
        }
    }
}
