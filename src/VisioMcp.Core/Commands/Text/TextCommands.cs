using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Text;

public class TextCommands : ITextCommands
{
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

            void SearchPage(dynamic s, int idx)
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
                dynamic page = pres.Pages.Item(pageIndex);
                try
                {
                    SearchPage(page, pageIndex);
                }
                finally
                {
                    ComUtilities.Release(ref page!);
                }
            }
            else
            {
                dynamic pages = pres.Pages;
                try
                {
                    int pageCount = Convert.ToInt32(pages.Count);
                    for (int i = 1; i <= pageCount; i++)
                    {
                        dynamic page = pages.Item(i);
                        try
                        {
                            SearchPage(page, i);
                        }
                        finally
                        {
                            ComUtilities.Release(ref page!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref pages!);
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

            void ReplaceInPage(dynamic s)
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
                dynamic page = pres.Pages.Item(pageIndex);
                try
                {
                    ReplaceInPage(page);
                }
                finally
                {
                    ComUtilities.Release(ref page!);
                }
            }
            else
            {
                dynamic pages = pres.Pages;
                try
                {
                    int pageCount = Convert.ToInt32(pages.Count);
                    for (int i = 1; i <= pageCount; i++)
                    {
                        dynamic page = pages.Item(i);
                        try
                        {
                            ReplaceInPage(page);
                        }
                        finally
                        {
                            ComUtilities.Release(ref page!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref pages!);
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

    public OperationResult WordCount(IVisioBatch batch, int pageIndex)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pres = ctx.Document;
            int totalWords = 0;

            void CountInPage(dynamic s)
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
                dynamic page = pres.Pages.Item(pageIndex);
                try
                {
                    CountInPage(page);
                }
                finally
                {
                    ComUtilities.Release(ref page!);
                }
            }
            else
            {
                dynamic pages = pres.Pages;
                try
                {
                    int pageCount = Convert.ToInt32(pages.Count);
                    for (int i = 1; i <= pageCount; i++)
                    {
                        dynamic page = pages.Item(i);
                        try
                        {
                            CountInPage(page);
                        }
                        finally
                        {
                            ComUtilities.Release(ref page!);
                        }
                    }
                }
                finally
                {
                    ComUtilities.Release(ref pages!);
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

    /// <summary>
    /// Read paragraph and run details from a COM TextRange into the provided list.
    /// </summary>
    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ctx.Document.Pages.Item(pageIndex);
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
}
