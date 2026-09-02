using System.Globalization;
using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Text;

public class TextCommands : ITextCommands
{
    // Char.Style is a bitfield rather than separate cells.
    private const int CharStyleBold = 1;
    private const int CharStyleItalic = 2;
    private const int CharStyleUnderline = 4;

    /// <summary>Maps the accepted alignment words onto Para.HorzAlign values.</summary>
    private static int ParseHorizontalAlignment(string alignment) => alignment.ToLowerInvariant() switch
    {
        "left" => 0,
        "center" => 1,
        "centre" => 1,
        "right" => 2,
        "justify" => 3,
        _ => throw new ArgumentException(
            $"Unknown alignment '{alignment}'. Expected left, center, right or justify.", nameof(alignment))
    };

    /// <summary>Maps the accepted vertical alignment words onto the VerticalAlign cell.</summary>
    private static int ParseVerticalAlignment(string verticalAlignment) => verticalAlignment.ToLowerInvariant() switch
    {
        "top" => 0,
        "middle" => 1,
        "center" => 1,
        "centre" => 1,
        "bottom" => 2,
        _ => throw new ArgumentException(
            $"Unknown vertical alignment '{verticalAlignment}'. Expected top, middle or bottom.", nameof(verticalAlignment))
    };

    /// <summary>Lower-cases everything, then capitalises the first letter of each sentence.</summary>
    private static string ToSentenceCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var chars = value.ToLowerInvariant().ToCharArray();
        bool startOfSentence = true;

        for (int i = 0; i < chars.Length; i++)
        {
            if (startOfSentence && char.IsLetter(chars[i]))
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                startOfSentence = false;
            }
            else if (chars[i] is '.' or '!' or '?')
            {
                startOfSentence = true;
            }
        }

        return new string(chars);
    }

    public TextResult GetText(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var result = new TextResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    ShapeId = GetShapeId(shape),
                    ShapeName = shape.Name?.ToString() ?? ""
                };

                result.Text = GetShapeText(shape);
                result.Paragraphs.Add(new TextParagraphInfo { Index = 1, Text = result.Text });
                return result;
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult SetText(IVisioBatch batch, int pageIndex, string shapeName, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                shape.Text = text;
                return new OperationResult
                {
                    Success = true,
                    Action = "set",
                    Message = $"Set text on shape '{shapeName}' (page {pageIndex})",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Find(IVisioBatch batch, string searchText, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = ctx.Document;
            var matches = new List<string>();

            void SearchSlide(dynamic s, int idx)
            {
                dynamic shapes = s.Shapes;
                try
                {
                    int count = (int)shapes.Count;
                    for (int i = 1; i <= count; i++)
                    {
                        dynamic shape = shapes.Item(i);
                        try
                        {
                            string text = GetShapeText(shape);
                            if (!string.IsNullOrEmpty(text) && text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            {
                                matches.Add($"Page {idx}, Shape '{shape.Name}': found '{searchText}'");
                            }
                        }
                        finally
                        {
                            ComUtilities.Release(ref shape!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shapes!);
                }
            }

            if (pageIndex > 0)
            {
                dynamic slide = pres.Pages.Item(pageIndex);
                try
                {
                    SearchSlide(slide, pageIndex);
                }
                finally
                {
                    ComUtilities.Release(ref slide!);
                }
            }
            else
            {
                dynamic slides = pres.Pages;
                try
                {
                    int slideCount = Convert.ToInt32(slides.Count);
                    for (int i = 1; i <= slideCount; i++)
                    {
                        dynamic slide = slides.Item(i);
                        try
                        {
                            SearchSlide(slide, i);
                        }
                        finally
                        {
                            ComUtilities.Release(ref slide!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref slides!);
                }
            }

            return new OperationResult
            {
                Success = true,
                Action = "find",
                Message = matches.Count > 0
                    ? $"Found {matches.Count} match(es):\n" + string.Join("\n", matches)
                    : $"No matches found for '{searchText}'",
                FilePath = ctx.DocumentPath
            };
        });
    }

    public OperationResult Replace(IVisioBatch batch, string searchText, string replaceText, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = ctx.Document;
            int replacements = 0;

            void ReplaceInSlide(dynamic s)
            {
                dynamic shapes = s.Shapes;
                try
                {
                    int count = (int)shapes.Count;
                    for (int i = 1; i <= count; i++)
                    {
                        dynamic shape = shapes.Item(i);
                        try
                        {
                            string text = GetShapeText(shape);
                            if (!string.IsNullOrEmpty(text) && text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            {
                                shape.Text = text.Replace(searchText, replaceText, StringComparison.OrdinalIgnoreCase);
                                replacements++;
                            }
                        }
                        finally
                        {
                            ComUtilities.Release(ref shape!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shapes!);
                }
            }

            if (pageIndex > 0)
            {
                dynamic slide = pres.Pages.Item(pageIndex);
                try
                {
                    ReplaceInSlide(slide);
                }
                finally
                {
                    ComUtilities.Release(ref slide!);
                }
            }
            else
            {
                dynamic slides = pres.Pages;
                try
                {
                    int slideCount = Convert.ToInt32(slides.Count);
                    for (int i = 1; i <= slideCount; i++)
                    {
                        dynamic slide = slides.Item(i);
                        try
                        {
                            ReplaceInSlide(slide);
                        }
                        finally
                        {
                            ComUtilities.Release(ref slide!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref slides!);
                }
            }

            return new OperationResult
            {
                Success = true,
                Action = "replace",
                Message = $"Replaced {replacements} occurrence(s) of '{searchText}' with '{replaceText}'",
                FilePath = ctx.DocumentPath
            };
        });
    }

    public OperationResult Format(IVisioBatch batch, int pageIndex, string shapeName, string? fontName, float? fontSize, bool? bold, bool? italic, string? color, string? alignment, string? verticalAlignment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var changes = new List<string>();

                if (fontName != null)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Char.Font", ShapeSheetHelpers.ToStringFormula(fontName));
                    changes.Add($"font={fontName}");
                }

                if (fontSize.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Char.Size", ShapeSheetHelpers.FormatInvariant(fontSize.Value) + " pt");
                    changes.Add($"size={ShapeSheetHelpers.FormatInvariant(fontSize.Value)}pt");
                }

                if (color != null)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Char.Color", ShapeSheetHelpers.ToRgbFormula(color, nameof(color)));
                    changes.Add($"color={color}");
                }

                // Char.Style is a bitfield (bold 1, italic 2, underline 4), so bold and italic are
                // applied by read-modify-write. Setting the cell outright would silently clear
                // whichever attribute the caller did not mention.
                if (bold.HasValue || italic.HasValue)
                {
                    int style = (int)(ShapeSheetHelpers.TryGetResult(shape, "Char.Style") ?? 0d);

                    if (bold.HasValue)
                    {
                        style = bold.Value ? style | CharStyleBold : style & ~CharStyleBold;
                        changes.Add($"bold={bold.Value}");
                    }

                    if (italic.HasValue)
                    {
                        style = italic.Value ? style | CharStyleItalic : style & ~CharStyleItalic;
                        changes.Add($"italic={italic.Value}");
                    }

                    ShapeSheetHelpers.SetFormula(shape, "Char.Style", style.ToString(CultureInfo.InvariantCulture));
                }

                if (alignment != null)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Para.HorzAlign", ParseHorizontalAlignment(alignment).ToString(CultureInfo.InvariantCulture));
                    changes.Add($"align={alignment}");
                }

                if (verticalAlignment != null)
                {
                    ShapeSheetHelpers.SetFormula(shape, "VerticalAlign", ParseVerticalAlignment(verticalAlignment).ToString(CultureInfo.InvariantCulture));
                    changes.Add($"valign={verticalAlignment}");
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "format",
                    Message = changes.Count > 0
                        ? $"Formatted text in shape '{shapeName}' on page {pageIndex}: {string.Join(", ", changes)}"
                        : $"No formatting changed on shape '{shapeName}' (all parameters were null)",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult FormatAdvanced(IVisioBatch batch, int pageIndex, string shapeName, bool? underline, bool? strikethrough, bool? subscript, bool? superscript)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var changes = new List<string>();

                if (underline.HasValue)
                {
                    int style = (int)(ShapeSheetHelpers.TryGetResult(shape, "Char.Style") ?? 0d);
                    style = underline.Value ? style | CharStyleUnderline : style & ~CharStyleUnderline;
                    ShapeSheetHelpers.SetFormula(shape, "Char.Style", style.ToString(CultureInfo.InvariantCulture));
                    changes.Add($"underline={underline.Value}");
                }

                // Visio's Character section has no strikethrough, subscript or superscript cell.
                // These are reported rather than silently dropped: a no-op that returns success is
                // how a caller comes to believe the text changed when it did not.
                var unsupported = new List<string>();
                if (strikethrough.HasValue) unsupported.Add("strikethrough");
                if (subscript.HasValue) unsupported.Add("subscript");
                if (superscript.HasValue) unsupported.Add("superscript");

                string message = changes.Count > 0
                    ? $"Applied advanced formatting to shape '{shapeName}' on page {pageIndex}: {string.Join(", ", changes)}"
                    : $"No advanced formatting applied to shape '{shapeName}'";

                if (unsupported.Count > 0)
                {
                    message += $". Ignored (no Visio equivalent): {string.Join(", ", unsupported)}";
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "format-advanced",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult WordCount(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = ctx.Document;
            int totalWords = 0;

            void CountInSlide(dynamic s)
            {
                dynamic shapes = s.Shapes;
                try
                {
                    int count = (int)shapes.Count;
                    for (int i = 1; i <= count; i++)
                    {
                        dynamic shape = shapes.Item(i);
                        try
                        {
                            string text = GetShapeText(shape);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                totalWords += text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                            }
                        }
                        finally
                        {
                            ComUtilities.Release(ref shape!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref shapes!);
                }
            }

            if (pageIndex > 0)
            {
                dynamic slide = pres.Pages.Item(pageIndex);
                try
                {
                    CountInSlide(slide);
                }
                finally
                {
                    ComUtilities.Release(ref slide!);
                }
            }
            else
            {
                dynamic slides = pres.Pages;
                try
                {
                    int slideCount = Convert.ToInt32(slides.Count);
                    for (int i = 1; i <= slideCount; i++)
                    {
                        dynamic slide = slides.Item(i);
                        try
                        {
                            CountInSlide(slide);
                        }
                        finally
                        {
                            ComUtilities.Release(ref slide!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref slides!);
                }
            }

            string scope = pageIndex > 0 ? $"page {pageIndex}" : "all pages";
            return new OperationResult
            {
                Success = true,
                Action = "word-count",
                Message = $"Total word count ({scope}): {totalWords}",
                FilePath = ctx.DocumentPath
            };
        });
    }

    public OperationResult AltTextAudit(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shapes = page.Shapes;
            try
            {
                int count = (int)shapes.Count;
                var missing = new List<string>();

                for (int i = 1; i <= count; i++)
                {
                    dynamic shape = shapes.Item(i);
                    try
                    {
                        // Visio's alt-text equivalent is the Comment cell - see
                        // ShapeCommands.SetAltText. It is a string cell, so the formula carries the
                        // text; an empty shape yields the empty-string formula "" rather than null.
                        string comment = ShapeSheetHelpers.TryGetFormula(shape, "Comment") ?? string.Empty;
                        string trimmed = comment.Trim().Trim('"').Trim();

                        if (trimmed.Length == 0)
                        {
                            missing.Add(shape.NameU?.ToString() ?? $"Shape{i}");
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref shape!);
                    }
                }

                string message = missing.Count == 0
                    ? $"All {count} shape(s) on page {pageIndex} have alt text"
                    : $"{missing.Count} of {count} shape(s) on page {pageIndex} have no alt text: {string.Join(", ", missing)}";

                return new OperationResult
                {
                    Success = true,
                    Action = "alt-text-audit",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shapes!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult EmptyPlaceholderAudit(IVisioBatch batch, int pageIndex)
    {
        // Placeholders are a PowerPoint slide-layout concept: a slide inherits typed regions from
        // its layout, and an unfilled one is a defect. Visio pages have no layout inheritance and
        // therefore no placeholders, so there is nothing equivalent to audit.
        _ = batch;
        _ = pageIndex;

        throw new NotSupportedException(
            "text(empty-placeholder-audit) has no Visio equivalent. Placeholders come from a " +
            "PowerPoint slide layout; Visio pages have no layout inheritance. To find shapes with " +
            "no text, use shape(list) followed by text(get) on each shape.");
    }
    public OperationResult SetSpacing(IVisioBatch batch, int pageIndex, string shapeName, float? lineSpacing, float? spaceBefore, float? spaceAfter, float? characterSpacing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                var changes = new List<string>();

                if (lineSpacing.HasValue)
                {
                    // A negative SpLine is a multiple of the line height, which is how a "1.5 line
                    // spacing" request is expressed; a positive value would be an absolute distance.
                    ShapeSheetHelpers.SetFormula(shape, "Para.SpLine", ShapeSheetHelpers.FormatInvariant(-lineSpacing.Value));
                    changes.Add($"lineSpacing={ShapeSheetHelpers.FormatInvariant(lineSpacing.Value)}");
                }

                if (spaceBefore.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Para.SpBefore", ShapeSheetHelpers.FormatInvariant(spaceBefore.Value) + " pt");
                    changes.Add($"spaceBefore={ShapeSheetHelpers.FormatInvariant(spaceBefore.Value)}pt");
                }

                if (spaceAfter.HasValue)
                {
                    ShapeSheetHelpers.SetFormula(shape, "Para.SpAfter", ShapeSheetHelpers.FormatInvariant(spaceAfter.Value) + " pt");
                    changes.Add($"spaceAfter={ShapeSheetHelpers.FormatInvariant(spaceAfter.Value)}pt");
                }

                string message = changes.Count > 0
                    ? $"Set spacing on shape '{shapeName}' on page {pageIndex}: {string.Join(", ", changes)}"
                    : $"No spacing changed on shape '{shapeName}'";

                // Visio has no character-spacing (tracking) cell.
                if (characterSpacing.HasValue)
                {
                    message += ". Ignored (no Visio equivalent): characterSpacing";
                }

                return new OperationResult
                {
                    Success = true,
                    Action = "set-spacing",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult SetBullets(IVisioBatch batch, int pageIndex, string shapeName, int bulletType, string? bulletCharacter, int indentLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (bulletType is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bulletType), bulletType, "bulletType must be 0 (none) or 1-7 (Visio bullet styles).");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(indentLevel);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                ShapeSheetHelpers.SetFormula(shape, "Para.Bullet", bulletType.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrEmpty(bulletCharacter))
                {
                    ShapeSheetHelpers.SetFormula(shape, "Para.BulletStr", ShapeSheetHelpers.ToStringFormula(bulletCharacter));
                }

                // Visio has no discrete bullet "levels"; indentation is a distance. A quarter inch
                // per level matches Visio's own default list indentation.
                double indentInches = indentLevel * 0.25d;
                ShapeSheetHelpers.SetFormula(shape, "Para.IndLeft", ShapeSheetHelpers.FormatInvariant(indentInches) + " in");

                return new OperationResult
                {
                    Success = true,
                    Action = "set-bullets",
                    Message = bulletType == 0
                        ? $"Removed bullets from shape '{shapeName}' on page {pageIndex}"
                        : $"Set bullet style {bulletType} on shape '{shapeName}' at indent level {indentLevel} on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static int GetShapeId(dynamic shape)
    {
        try
        {
            return Convert.ToInt32(shape.ID);
        }
        catch
        {
            return 0;
        }
    }

    private static string GetShapeText(dynamic shape)
    {
        try
        {
            return shape.Text?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void ReadParagraphs(dynamic textRange, List<TextParagraphInfo> paragraphs)
    {
        dynamic allParagraphs = textRange.Paragraphs();
        try
        {
            int paraCount = (int)allParagraphs.Count;
            for (int p = 1; p <= paraCount; p++)
            {
                dynamic para = textRange.Paragraphs(p, 1);
                try
                {
                    var paraInfo = new TextParagraphInfo
                    {
                        Index = p,
                        Text = para.Text?.ToString() ?? ""
                    };

                    try { paraInfo.Alignment = Convert.ToInt32(para.ParagraphFormat.Alignment); } catch { }

                    dynamic runs = para.Runs();
                    try
                    {
                        int runCount = (int)runs.Count;
                        for (int r = 1; r <= runCount; r++)
                        {
                            dynamic run = para.Runs(r, 1);
                            try
                            {
                                var runInfo = new TextRunInfo
                                {
                                    Text = run.Text?.ToString() ?? ""
                                };
                                try { runInfo.FontName = run.Font.Name?.ToString(); } catch { }
                                try { runInfo.FontSize = Convert.ToSingle(run.Font.Size); } catch { }
                                try { runInfo.Bold = Convert.ToInt32(run.Font.Bold) != 0; } catch { }
                                try { runInfo.Italic = Convert.ToInt32(run.Font.Italic) != 0; } catch { }
                                try
                                {
                                    int rgb = Convert.ToInt32(run.Font.Color.RGB);
                                    runInfo.Color = $"#{rgb:X6}";
                                }
                                catch { }

                                paraInfo.Runs.Add(runInfo);
                            }
                            finally
                            {
                                ComUtilities.Release(ref run!);
                            }
                        }
                    }
                    finally
                    {
                        ComUtilities.Release(ref runs!);
                    }

                    paragraphs.Add(paraInfo);
                }
                finally
                {
                    ComUtilities.Release(ref para!);
                }
            }
        }
        finally
        {
            ComUtilities.Release(ref allParagraphs!);
        }
    }

    public OperationResult InsertLink(IVisioBatch batch, int pageIndex, string shapeName, string linkText, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(linkText);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        // PowerPoint could hyperlink a text *range* inside a shape. Visio models hyperlinks as
        // rows in a shape's Hyperlink ShapeSheet section, attached to the whole shape, so there is
        // no equivalent of linking part of a shape's text.
        //
        // Parameters are still validated first so the caller gets the more specific error when
        // both a bad argument and the unsupported operation apply.
        _ = batch;
        _ = pageIndex;

        throw new NotSupportedException(
            "text(insert-link) has no Visio equivalent. Visio attaches hyperlinks to a whole shape " +
            "via its Hyperlink ShapeSheet section rather than to a range of text. Use the hyperlink " +
            "tool once it is reimplemented against Shape.Hyperlinks (tracked in #35).");
    }
    public OperationResult ChangeCase(IVisioBatch batch, int pageIndex, string shapeName, int caseType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (caseType is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(caseType), caseType, "caseType must be 0 (lower), 1 (upper), 2 (title) or 3 (sentence).");
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Deliberately not Char.Case: that is a display transform, so the stored text would
                // be unchanged and a subsequent text(get) would return the original casing. The
                // caller asked to change the text, so the text is changed.
                string original = shape.Text?.ToString() ?? string.Empty;
                string converted = caseType switch
                {
                    0 => original.ToLowerInvariant(),
                    1 => original.ToUpperInvariant(),
                    2 => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(original.ToLowerInvariant()),
                    _ => ToSentenceCase(original)
                };

                shape.Text = converted;

                return new OperationResult
                {
                    Success = true,
                    Action = "change-case",
                    Message = $"Changed case of text in shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult ReadSpacing(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // SpLine is negative when it expresses a multiple of the line height and positive
                // when it is an absolute distance, so it is reported raw rather than converted.
                double lineSpacing = ShapeSheetHelpers.TryGetResult(shape, "Para.SpLine") ?? 0d;
                double beforeInches = ShapeSheetHelpers.TryGetResult(shape, "Para.SpBefore") ?? 0d;
                double afterInches = ShapeSheetHelpers.TryGetResult(shape, "Para.SpAfter") ?? 0d;

                string message =
                    $"LineSpacing: {ShapeSheetHelpers.FormatInvariant(lineSpacing)}, " +
                    $"SpaceBefore: {ShapeSheetHelpers.FormatInvariant(ShapeSheetHelpers.InchesToPoints(beforeInches))}pt, " +
                    $"SpaceAfter: {ShapeSheetHelpers.FormatInvariant(ShapeSheetHelpers.InchesToPoints(afterInches))}pt";

                return new OperationResult
                {
                    Success = true,
                    Action = "read-spacing",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult ReadBullets(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                int bullet = (int)(ShapeSheetHelpers.TryGetResult(shape, "Para.Bullet") ?? 0d);
                string custom = ShapeSheetHelpers.TryGetFormula(shape, "Para.BulletStr") ?? string.Empty;
                double indentInches = ShapeSheetHelpers.TryGetResult(shape, "Para.IndLeft") ?? 0d;

                string message = bullet == 0
                    ? "Bullets: none"
                    : $"Bullets: style {bullet}, custom character {custom}, " +
                      $"indent {ShapeSheetHelpers.FormatInvariant(ShapeSheetHelpers.InchesToPoints(indentInches))}pt";

                return new OperationResult
                {
                    Success = true,
                    Action = "read-bullets",
                    Message = message,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult InsertSymbol(IVisioBatch batch, int pageIndex, string shapeName, string fontName, int charNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        // char.ConvertFromUtf32 throws for surrogates and out-of-range values; check first so the
        // caller gets a parameter error rather than an opaque conversion failure.
        if (charNumber < 0 || charNumber > 0x10FFFF || (charNumber >= 0xD800 && charNumber <= 0xDFFF))
        {
            throw new ArgumentOutOfRangeException(
                nameof(charNumber), charNumber, "charNumber must be a valid Unicode code point outside the surrogate range.");
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                string symbol = char.ConvertFromUtf32(charNumber);
                string existing = shape.Text?.ToString() ?? string.Empty;
                shape.Text = existing + symbol;

                // Visio applies Char.Font to the whole shape rather than to a range, so naming a
                // symbol font restyles all of the shape's text, not just the inserted character.
                ShapeSheetHelpers.SetFormula(shape, "Char.Font", ShapeSheetHelpers.ToStringFormula(fontName));

                return new OperationResult
                {
                    Success = true,
                    Action = "insert-symbol",
                    Message = $"Appended symbol U+{charNumber:X4} to shape '{shapeName}' on page {pageIndex} and set font '{fontName}' for the whole shape",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult InsertDateTime(IVisioBatch batch, int pageIndex, string shapeName, int dateTimeFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // Formatted with InvariantCulture so the result does not vary with the machine
                // locale. As with insert-slide-number this is literal text, not a live field.
                var now = DateTime.Now;
                string formatted = dateTimeFormat switch
                {
                    1 => now.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
                    2 => now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    3 => now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    4 => now.ToString("HH:mm", CultureInfo.InvariantCulture),
                    5 => now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    _ => now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                };

                string existing = shape.Text?.ToString() ?? string.Empty;
                shape.Text = existing + formatted;

                return new OperationResult
                {
                    Success = true,
                    Action = "insert-date-time",
                    Message = $"Appended '{formatted}' to shape '{shapeName}' on page {pageIndex}. Note: inserted as literal text, not a field that updates.",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
    public OperationResult InsertSlideNumber(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic shape = page.Shapes.Item(shapeName);
            try
            {
                // PowerPoint inserted a live slide-number field. Visio's equivalent would be a
                // Fields-section field, which cannot be added through a single cell write, so the
                // page's current index is appended as literal text instead. The distinction
                // matters: this does not update if pages are reordered.
                string existing = shape.Text?.ToString() ?? string.Empty;
                shape.Text = existing + pageIndex.ToString(CultureInfo.InvariantCulture);

                return new OperationResult
                {
                    Success = true,
                    Action = "insert-slide-number",
                    Message = $"Appended page number {pageIndex} to shape '{shapeName}'. Note: inserted as literal text, not a field that tracks page order.",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref shape!);
                ComUtilities.Release(ref page!);
            }
        });
    }
}
