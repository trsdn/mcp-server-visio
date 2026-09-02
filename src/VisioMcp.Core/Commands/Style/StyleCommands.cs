using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Style;

/// <summary>
/// Named formatting held by the document, backed by <c>Document.Styles</c> (#36d).
/// </summary>
public class StyleCommands : IStyleCommands
{
    /// <summary>VBA True, which is how Visio reports the Includes* flags.</summary>
    private const short VisTrue = -1;

    /// <summary>What Visio reports for a style that inherits nothing, and what shapes fall back to.</summary>
    private const string NoStyle = "No Style";

    public StyleListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic styles = ctx.Document.Styles;
            try
            {
                var found = new List<StyleInfo>();
                int count = (int)styles.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic? style = null;
                    try
                    {
                        style = styles[i];
                        found.Add(Describe(style));
                    }
                    finally
                    {
                        if (style != null) ComUtilities.Release(ref style!);
                    }
                }

                return new StyleListResult
                {
                    Success = true,
                    Styles = found,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref styles!);
            }
        });
    }

    public StyleDetailResult Read(IVisioBatch batch, string styleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? style = null;
            try
            {
                style = GetStyle(ctx, styleName);
                return new StyleDetailResult
                {
                    Success = true,
                    Style = Describe(style),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        });
    }

    public StyleDetailResult Create(IVisioBatch batch, string styleName, string? basedOn = null, bool includesFill = true, bool includesLine = true, bool includesText = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);

        if (!includesFill && !includesLine && !includesText)
        {
            // Visio accepts this and produces a style that can never change a shape's appearance.
            throw new ArgumentException(
                "A style must carry at least one of fill, line or text. Creating one that carries "
                + "none produces a style that cannot change any shape.",
                nameof(includesFill));
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic styles = ctx.Document.Styles;
            dynamic? created = null;
            try
            {
                if (StyleExists(styles, styleName))
                {
                    // Visio's own message is "The name '%s' is already in use." — with the
                    // placeholder unexpanded, so it never says which name.
                    throw new ArgumentException(
                        $"A style named '{styleName}' already exists. Pick another name, or change the "
                        + "existing one with style(set-formula).",
                        nameof(styleName));
                }

                created = styles.Add(
                    styleName,
                    basedOn ?? string.Empty,
                    includesText ? 1 : 0,
                    includesLine ? 1 : 0,
                    includesFill ? 1 : 0);

                var info = Describe(created);

                // Guards the trap above: if the flags come back other than asked for, the style is
                // silently crippled and every later write and apply would no-op.
                if (info.IncludesFill != includesFill || info.IncludesLine != includesLine || info.IncludesText != includesText)
                {
                    throw new InvalidOperationException(
                        $"Visio created style '{styleName}' with fill={info.IncludesFill}, line={info.IncludesLine}, "
                        + $"text={info.IncludesText} rather than the requested fill={includesFill}, line={includesLine}, "
                        + $"text={includesText}. Styles.Add takes its flags in the order text, line, fill.");
                }

                return new StyleDetailResult
                {
                    Success = true,
                    Style = info,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (created != null) ComUtilities.Release(ref created!);
                ComUtilities.Release(ref styles!);
            }
        });
    }

    public StyleDetailResult Rename(IVisioBatch batch, string styleName, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? style = null;
            try
            {
                style = GetStyle(ctx, styleName);
                style.Name = newName;

                return new StyleDetailResult
                {
                    Success = true,
                    Style = Describe(style),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, string styleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? style = null;
            try
            {
                style = GetStyle(ctx, styleName);
                string actual = ComUtilities.SafeGetString(style, "Name");

                // Count before deleting: afterwards those shapes report 'No Style' and there is no
                // way to tell which ones were affected.
                int users = CountUsers(ctx, actual);

                style.Delete();

                string impact = users == 0
                    ? "No shapes were using it."
                    : $"{users} shape(s) were using it and have reverted to '{NoStyle}', losing that formatting.";

                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted style '{actual}'. {impact}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        });
    }

    public StyleCellResult ReadFormula(IVisioBatch batch, string styleName, string cellName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? style = null;
            try
            {
                style = GetStyle(ctx, styleName);

                if (!ShapeSheetHelpers.CellExists(style, cellName))
                {
                    throw new ArgumentException(
                        $"Style '{styleName}' has no cell named '{cellName}'. A style uses the same cell "
                        + "names as a shape, for example FillForegnd, LineWeight or Char.Size.",
                        nameof(cellName));
                }

                return new StyleCellResult
                {
                    Success = true,
                    StyleName = ComUtilities.SafeGetString(style, "Name"),
                    CellName = cellName,
                    Formula = ReadCellFormula(style, cellName),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        });
    }

    public StyleCellResult SetFormula(IVisioBatch batch, string styleName, string cellName, string formula)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cellName);
        ArgumentException.ThrowIfNullOrWhiteSpace(formula);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? style = null;
            try
            {
                style = GetStyle(ctx, styleName);
                ShapeSheetHelpers.SetFormula(style, cellName, formula);

                string written = ReadCellFormula(style, cellName);

                // A style silently ignores writes to an aspect it does not carry: the cell keeps
                // its old value and Visio raises nothing, so the caller would be told it worked.
                if (!FormulaMatches(written, formula))
                {
                    var info = Describe(style);
                    throw new InvalidOperationException(
                        $"Setting '{cellName}' on style '{styleName}' had no effect — it still reads "
                        + $"'{written}'. The style carries fill={info.IncludesFill}, line={info.IncludesLine}, "
                        + $"text={info.IncludesText}; a style ignores writes to an aspect it does not carry.");
                }

                return new StyleCellResult
                {
                    Success = true,
                    StyleName = ComUtilities.SafeGetString(style, "Name"),
                    CellName = cellName,
                    Formula = written,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        });
    }

    public OperationResult Apply(IVisioBatch batch, int pageIndex, string shapeName, string styleName, string? aspect = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(styleName);

        string which = string.IsNullOrWhiteSpace(aspect) ? "all" : aspect.Trim().ToLowerInvariant();
        if (which is not ("all" or "fill" or "line" or "text"))
        {
            throw new ArgumentException(
                $"Unknown aspect '{aspect}'. Use 'all' (the default), or 'fill', 'line' or 'text' to "
                + "apply only that part of the style.",
                nameof(aspect));
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? style = null;
            try
            {
                // Resolve the style first so an unknown name fails before the shape is touched.
                style = GetStyle(ctx, styleName);
                string actual = ComUtilities.SafeGetString(style, "Name");

                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);

                switch (which)
                {
                    case "fill": shape.FillStyle = actual; break;
                    case "line": shape.LineStyle = actual; break;
                    case "text": shape.TextStyle = actual; break;
                    default: shape.Style = actual; break;
                }

                // Visio refuses to apply a style aspect the style does not carry, and does so
                // silently: the shape keeps whatever it had, with no error.
                string landed = ReadAppliedStyle(shape, which);
                if (!string.Equals(landed, actual, StringComparison.OrdinalIgnoreCase))
                {
                    var info = Describe(style);
                    throw new InvalidOperationException(
                        $"Applying style '{actual}' to '{shapeName}' had no effect — the shape's {which} "
                        + $"style is still '{landed}'. The style carries fill={info.IncludesFill}, "
                        + $"line={info.IncludesLine}, text={info.IncludesText}; Visio ignores an aspect "
                        + "the style does not carry.");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "apply",
                    Message = which == "all"
                        ? $"Applied style '{actual}' to '{shapeName}' on page {pageIndex}"
                        : $"Applied the {which} part of style '{actual}' to '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    /// <summary>
    /// Counts shapes across the document whose fill, line or text style is the named one.
    /// </summary>
    private static int CountUsers(VisioContext ctx, string styleName)
    {
        dynamic? pages = null;
        try
        {
            pages = ctx.Document.Pages;
            int users = 0;
            int pageCount = (int)pages.Count;

            for (int p = 1; p <= pageCount; p++)
            {
                dynamic? page = null;
                try
                {
                    page = pages[p];
                    int shapeCount = (int)page.Shapes.Count;

                    for (int s = 1; s <= shapeCount; s++)
                    {
                        dynamic? shape = null;
                        try
                        {
                            shape = page.Shapes[s];
                            if (UsesStyle(shape, styleName))
                            {
                                users++;
                            }
                        }
                        finally
                        {
                            if (shape != null) ComUtilities.Release(ref shape!);
                        }
                    }
                }
                finally
                {
                    if (page != null) ComUtilities.Release(ref page!);
                }
            }

            return users;
        }
        finally
        {
            if (pages != null) ComUtilities.Release(ref pages!);
        }
    }

    private static bool UsesStyle(dynamic shape, string styleName)
    {
        foreach (var property in new[] { "Style", "FillStyle", "LineStyle", "TextStyle" })
        {
            try
            {
                var value = (string?)shape.GetType().InvokeMember(
                    property, System.Reflection.BindingFlags.GetProperty, null, shape, null);

                if (string.Equals(value, styleName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Guides and some foreign objects do not carry every style property.
            }
        }

        return false;
    }

    private static bool StyleExists(dynamic styles, string styleName)
    {
        int count = (int)styles.Count;
        for (int i = 1; i <= count; i++)
        {
            dynamic? style = null;
            try
            {
                style = styles[i];
                if (string.Equals(ComUtilities.SafeGetString(style, "Name"), styleName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            finally
            {
                if (style != null) ComUtilities.Release(ref style!);
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves a style by name, replacing Visio's bare "Object name not found".
    /// </summary>
    private static dynamic GetStyle(VisioContext ctx, string styleName)
    {
        dynamic styles = ctx.Document.Styles;
        try
        {
            try
            {
                return styles[styleName];
            }
            catch (Exception)
            {
                var available = new List<string>();
                int count = (int)styles.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic? style = null;
                    try
                    {
                        style = styles[i];
                        available.Add(ComUtilities.SafeGetString(style, "Name"));
                    }
                    finally
                    {
                        if (style != null) ComUtilities.Release(ref style!);
                    }
                }

                throw new ArgumentException(
                    $"Style '{styleName}' not found. This document has: {string.Join(", ", available)}.",
                    nameof(styleName));
            }
        }
        finally
        {
            ComUtilities.Release(ref styles!);
        }
    }

    private static string ReadCellFormula(dynamic style, string cellName)
    {
        dynamic? cell = null;
        try
        {
            cell = style.CellsU[cellName];
            return (string)cell.FormulaU ?? string.Empty;
        }
        finally
        {
            if (cell != null) ComUtilities.Release(ref cell!);
        }
    }

    /// <summary>
    /// Reads back the style that actually landed on a shape for one aspect.
    /// </summary>
    private static string ReadAppliedStyle(dynamic shape, string aspect) => aspect switch
    {
        "fill" => (string)shape.FillStyle ?? string.Empty,
        "line" => (string)shape.LineStyle ?? string.Empty,
        "text" => (string)shape.TextStyle ?? string.Empty,
        _ => (string)shape.Style ?? string.Empty
    };

    /// <summary>
    /// Whether a formula read back is the one that was written. Visio normalises spacing and may
    /// echo a written value in a canonical form, so this compares loosely rather than by equality.
    /// </summary>
    private static bool FormulaMatches(string readBack, string written)
    {
        static string Normalise(string value) => value.Replace(" ", string.Empty).Trim('=');

        return string.Equals(Normalise(readBack), Normalise(written), StringComparison.OrdinalIgnoreCase);
    }

    private static StyleInfo Describe(dynamic style)
    {
        string basedOn = string.Empty;
        try { basedOn = (string)style.BasedOn ?? string.Empty; } catch (Exception) { }

        return new StyleInfo
        {
            Name = ComUtilities.SafeGetString(style, "Name"),
            Index = (int)style.Index,
            Id = (int)style.ID,
            BasedOn = string.IsNullOrWhiteSpace(basedOn) ? null : basedOn,

            // Visio returns VBA shorts here, so a direct bool cast throws RuntimeBinderException.
            IncludesFill = (short)style.IncludesFill == VisTrue,
            IncludesLine = (short)style.IncludesLine == VisTrue,
            IncludesText = (short)style.IncludesText == VisTrue,
            Hidden = (short)style.Hidden != 0
        };
    }
}
