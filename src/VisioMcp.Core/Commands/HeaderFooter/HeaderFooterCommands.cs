using System.Reflection;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.HeaderFooter;

/// <summary>
/// Headers and footers on <c>Document.Header*</c> / <c>Document.Footer*</c>.
/// </summary>
/// <remarks>
/// Visio's model is document-scoped and field-based: six independent strings, not PowerPoint's
/// per-slide "show footer / show slide number / show date" toggles. The PowerPoint signature's
/// <c>showSlideNumber</c> and <c>showDate</c> have no analogue at all — a page number is expressed
/// as the field code <c>&amp;p</c> inside whichever of the six fields the caller wants it in — so
/// they are dropped rather than accepted and ignored.
/// </remarks>
public class HeaderFooterCommands : IHeaderFooterCommands
{
    /// <summary>
    /// Unit code passed to the parameterised margin properties. <c>visNoCast</c> (0) returns the
    /// value in Visio's internal units, which are inches.
    /// </summary>
    private const int VisNoCast = 0;

    public HeaderFooterResult GetInfo(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic doc = ctx.Document;

            return new HeaderFooterResult
            {
                Success = true,
                FilePath = ctx.DocumentPath,
                HeaderLeft = doc.HeaderLeft?.ToString() ?? string.Empty,
                HeaderCenter = doc.HeaderCenter?.ToString() ?? string.Empty,
                HeaderRight = doc.HeaderRight?.ToString() ?? string.Empty,
                FooterLeft = doc.FooterLeft?.ToString() ?? string.Empty,
                FooterCenter = doc.FooterCenter?.ToString() ?? string.Empty,
                FooterRight = doc.FooterRight?.ToString() ?? string.Empty,
                HeaderMargin = Convert.ToDouble(doc.HeaderMargin(VisNoCast)),
                FooterMargin = Convert.ToDouble(doc.FooterMargin(VisNoCast))
            };
        });
    }

    public OperationResult Update(
        IVisioBatch batch,
        string? headerLeft,
        string? headerCenter,
        string? headerRight,
        string? footerLeft,
        string? footerCenter,
        string? footerRight,
        double? headerMargin,
        double? footerMargin)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic doc = ctx.Document;
            var changed = new List<string>();

            if (headerLeft is not null) { doc.HeaderLeft = headerLeft; changed.Add("header_left"); }
            if (headerCenter is not null) { doc.HeaderCenter = headerCenter; changed.Add("header_center"); }
            if (headerRight is not null) { doc.HeaderRight = headerRight; changed.Add("header_right"); }
            if (footerLeft is not null) { doc.FooterLeft = footerLeft; changed.Add("footer_left"); }
            if (footerCenter is not null) { doc.FooterCenter = footerCenter; changed.Add("footer_center"); }
            if (footerRight is not null) { doc.FooterRight = footerRight; changed.Add("footer_right"); }

            if (headerMargin.HasValue)
            {
                SetMargin(doc, "HeaderMargin", headerMargin.Value);
                changed.Add("header_margin");
            }

            if (footerMargin.HasValue)
            {
                SetMargin(doc, "FooterMargin", footerMargin.Value);
                changed.Add("footer_margin");
            }

            return new OperationResult
            {
                Success = true,
                Action = "set",
                Message = changed.Count == 0
                    ? "No header/footer settings supplied; nothing changed"
                    : $"Updated {string.Join(", ", changed)}",
                FilePath = ctx.DocumentPath
            };
        });
    }

    /// <summary>
    /// Writes one of the parameterised margin properties.
    /// </summary>
    /// <remarks>
    /// <c>HeaderMargin</c> and <c>FooterMargin</c> are parameterised properties —
    /// <c>double HeaderMargin(Variant UnitsNameOrCode)</c> — so a plain <c>dynamic</c> assignment
    /// does not bind to them. Reflection with <see cref="BindingFlags.SetProperty"/> passes the
    /// units argument alongside the value, which is the only form the IDispatch binder accepts for
    /// a property that takes an argument.
    /// </remarks>
    private static void SetMargin(object document, string propertyName, double value)
    {
        document.GetType().InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: document,
            args: [VisNoCast, value],
            culture: System.Globalization.CultureInfo.InvariantCulture);
    }
}
