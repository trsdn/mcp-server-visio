using System.Globalization;
using VisioMcp.ComInterop;

namespace VisioMcp.Core.Commands;

/// <summary>
/// Shared helpers for reading and writing Visio ShapeSheet cells.
///
/// Visio models almost everything — fill, line, text, effects, protection — as ShapeSheet cells
/// rather than as COM properties, so the command classes that were ported from PowerPoint in #20
/// all need the same small set of primitives. They live here rather than being duplicated per
/// command class.
/// </summary>
internal static class ShapeSheetHelpers
{
    /// <summary>
    /// Writes a ShapeSheet formula by universal cell name, releasing the cell afterwards.
    /// </summary>
    internal static void SetFormula(dynamic shape, string cellName, string formula)
    {
        dynamic? cell = null;
        try
        {
            cell = shape.CellsU[cellName];
            cell.FormulaU = formula;
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Reads a ShapeSheet formula by universal cell name, or null when the cell does not exist.
    /// </summary>
    /// <remarks>
    /// String-valued cells such as <c>Char.Font</c> and <c>Comment</c> must be read this way:
    /// their numeric <c>ResultIU</c> is always 0.
    /// </remarks>
    internal static string? TryGetFormula(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            if (!CellExists(shape, cellName))
            {
                return null;
            }

            cell = shape.CellsU[cellName];
            return cell.FormulaU?.ToString();
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Reads a ShapeSheet cell's evaluated result in internal units, or null when the cell does
    /// not exist. Distances are inches and angles are radians.
    /// </summary>
    internal static double? TryGetResult(dynamic shape, string cellName)
    {
        dynamic? cell = null;
        try
        {
            if (!CellExists(shape, cellName))
            {
                return null;
            }

            cell = shape.CellsU[cellName];
            return Convert.ToDouble(cell.ResultIU, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (cell != null)
            {
                ComUtilities.Release(ref cell!);
            }
        }
    }

    /// <summary>
    /// Whether a shape exposes the named ShapeSheet cell.
    /// </summary>
    /// <remarks>
    /// <c>CellExistsU</c> returns a VBA-style <c>short</c> (0 or -1), not a <c>bool</c>. Casting it
    /// directly to <c>bool</c> throws <c>RuntimeBinderException: Cannot convert type 'short' to
    /// 'bool'</c>, so the comparison is done numerically.
    /// </remarks>
    internal static bool CellExists(dynamic shape, string cellName)
    {
        // visExistsAnywhere = 0: report the cell whether it is local or inherited from the master.
        return Convert.ToInt32(shape.CellExistsU[cellName, 0], CultureInfo.InvariantCulture) != 0;
    }

    /// <summary>
    /// Converts "#RRGGBB" or "RRGGBB" into a ShapeSheet colour formula.
    /// </summary>
    internal static string ToRgbFormula(string colorHex, string parameterName)
    {
        var trimmed = colorHex.Trim().TrimStart('#');

        if (trimmed.Length != 6 || !int.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
        {
            throw new ArgumentException(
                $"Colour '{colorHex}' is not a 6-digit hex value such as '#FF0000' or 'FF0000'.",
                parameterName);
        }

        int r = (packed >> 16) & 0xFF;
        int g = (packed >> 8) & 0xFF;
        int b = packed & 0xFF;

        return $"RGB({r},{g},{b})";
    }

    /// <summary>
    /// Wraps text as a quoted ShapeSheet string formula, escaping embedded quotes.
    /// </summary>
    internal static string ToStringFormula(string? value)
    {
        var text = value ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /// <summary>
    /// Formats a number for a ShapeSheet formula.
    /// </summary>
    /// <remarks>
    /// Current-culture formatting produces a comma decimal separator on many locales, which
    /// corrupts the formula. Everything written to or reported from a cell goes through here.
    /// </remarks>
    internal static string FormatInvariant(double value) =>
        value.ToString("0.############", CultureInfo.InvariantCulture);

    /// <summary>Internal distance units are inches; callers work in points.</summary>
    internal static double InchesToPoints(double inches) => inches * 72d;
}
