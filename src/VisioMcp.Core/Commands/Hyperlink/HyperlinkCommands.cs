using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Hyperlink;

/// <summary>
/// Hyperlinks attached to shapes, backed by <c>Shape.Hyperlinks</c> (#35).
/// </summary>
public class HyperlinkCommands : IHyperlinkCommands
{
    public HyperlinkListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic pages = ctx.Document.Pages;
            try
            {
                var found = new List<HyperlinkInfo>();
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
                                string shapeName = ComUtilities.SafeGetString(shape, "Name");

                                foreach (var info in ReadAll(shape))
                                {
                                    info.PageIndex = p;
                                    info.ShapeName = shapeName;
                                    found.Add(info);
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

                return new HyperlinkListResult
                {
                    Success = true,
                    Hyperlinks = found,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref pages!);
            }
        });
    }

    public HyperlinkListResult ListForShape(IVisioBatch batch, int pageIndex, string shapeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);

                var found = ReadAll(shape);
                foreach (var info in found)
                {
                    info.PageIndex = pageIndex;
                    info.ShapeName = shapeName;
                }

                return new HyperlinkListResult
                {
                    Success = true,
                    Hyperlinks = found,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public HyperlinkResult Read(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hyperlinkName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? hyperlink = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);
                hyperlink = GetHyperlink(shape, shapeName, hyperlinkName);

                return Describe(ctx, pageIndex, shapeName, hyperlink);
            }
            finally
            {
                if (hyperlink != null) ComUtilities.Release(ref hyperlink!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public HyperlinkResult Add(IVisioBatch batch, int pageIndex, string shapeName, string? address = null, string? subAddress = null, string? description = null, string? hyperlinkName = null, bool newWindow = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        if (string.IsNullOrWhiteSpace(address) && string.IsNullOrWhiteSpace(subAddress))
        {
            // A hyperlink with neither target is a row that does nothing, and Visio accepts it
            // silently — so it would look like the link had been created.
            throw new ArgumentException(
                "A hyperlink needs a target: pass address for an external one such as "
                + "'https://example.com', or sub_address to navigate inside the document such as "
                + "'Page-2'. Both may be given together.",
                nameof(address));
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? hyperlinks = null;
            dynamic? created = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);
                hyperlinks = shape.Hyperlinks;
                created = hyperlinks.Add();

                if (!string.IsNullOrWhiteSpace(hyperlinkName))
                {
                    created.Name = hyperlinkName;
                }

                Apply(created, address, subAddress, description, newWindow);

                return Describe(ctx, pageIndex, shapeName, created);
            }
            finally
            {
                if (created != null) ComUtilities.Release(ref created!);
                if (hyperlinks != null) ComUtilities.Release(ref hyperlinks!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public HyperlinkResult Update(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName, string? address = null, string? subAddress = null, string? description = null, bool? newWindow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hyperlinkName);

        if (address is null && subAddress is null && description is null && newWindow is null)
        {
            throw new ArgumentException(
                "update needs at least one of address, sub_address, description or new_window. "
                + "Omitted values are left as they are, so a call with none of them would do nothing.",
                nameof(address));
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? hyperlink = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);
                hyperlink = GetHyperlink(shape, shapeName, hyperlinkName);

                Apply(hyperlink, address, subAddress, description, newWindow);

                return Describe(ctx, pageIndex, shapeName, hyperlink);
            }
            finally
            {
                if (hyperlink != null) ComUtilities.Release(ref hyperlink!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, int pageIndex, string shapeName, string hyperlinkName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hyperlinkName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? hyperlink = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);
                hyperlink = GetHyperlink(shape, shapeName, hyperlinkName);
                hyperlink.Delete();

                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted hyperlink '{hyperlinkName}' from shape '{shapeName}' on page {pageIndex}",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (hyperlink != null) ComUtilities.Release(ref hyperlink!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    private static void Apply(dynamic hyperlink, string? address, string? subAddress, string? description, bool? newWindow)
    {
        if (address is not null) hyperlink.Address = address;
        if (subAddress is not null) hyperlink.SubAddress = subAddress;
        if (description is not null) hyperlink.Description = description;
        if (newWindow is not null) hyperlink.NewWindow = newWindow.Value ? 1 : 0;
    }

    /// <summary>
    /// Resolves a hyperlink by row name, replacing Visio's bare "Invalid parameter".
    /// </summary>
    private static dynamic GetHyperlink(dynamic shape, string shapeName, string hyperlinkName)
    {
        dynamic hyperlinks = shape.Hyperlinks;
        try
        {
            try
            {
                return hyperlinks[hyperlinkName];
            }
            catch (Exception)
            {
                List<HyperlinkInfo> existing = ReadAll(shape);
                var available = existing.Select(h => h.Name).ToList();
                string detail = available.Count == 0
                    ? $"Shape '{shapeName}' has no hyperlinks. Use hyperlink(add) to create one."
                    : $"Shape '{shapeName}' has: {string.Join(", ", available)}.";

                throw new ArgumentException(
                    $"Hyperlink '{hyperlinkName}' not found. {detail}",
                    nameof(hyperlinkName));
            }
        }
        finally
        {
            ComUtilities.Release(ref hyperlinks!);
        }
    }

    private static List<HyperlinkInfo> ReadAll(dynamic shape)
    {
        dynamic? hyperlinks = null;
        try
        {
            hyperlinks = shape.Hyperlinks;
            int count = (int)hyperlinks.Count;
            var found = new List<HyperlinkInfo>(count);

            // Shape.Hyperlinks is 0-BASED, unlike Pages, Shapes and Masters. Starting at 1 throws
            // COMException "Invalid parameter" on a shape that has exactly one hyperlink.
            for (int i = 0; i < count; i++)
            {
                dynamic? hyperlink = null;
                try
                {
                    hyperlink = hyperlinks[i];
                    found.Add(Describe(hyperlink));
                }
                finally
                {
                    if (hyperlink != null) ComUtilities.Release(ref hyperlink!);
                }
            }

            return found;
        }
        finally
        {
            if (hyperlinks != null) ComUtilities.Release(ref hyperlinks!);
        }
    }

    private static HyperlinkResult Describe(VisioContext ctx, int pageIndex, string shapeName, dynamic hyperlink)
    {
        var info = Describe(hyperlink);
        info.PageIndex = pageIndex;
        info.ShapeName = shapeName;

        return new HyperlinkResult
        {
            Success = true,
            PageIndex = pageIndex,
            ShapeName = shapeName,
            Hyperlink = info,
            FilePath = ctx.DocumentPath
        };
    }

    private static HyperlinkInfo Describe(dynamic hyperlink)
    {
        string description = ComUtilities.SafeGetString(hyperlink, "Description");

        return new HyperlinkInfo
        {
            Name = ComUtilities.SafeGetString(hyperlink, "Name"),
            RowIndex = (int)hyperlink.Row,
            Address = (string)hyperlink.Address ?? string.Empty,
            SubAddress = Nullify((string)hyperlink.SubAddress),
            Description = Nullify(description),
            ExtraInfo = Nullify((string)hyperlink.ExtraInfo),
            Frame = Nullify((string)hyperlink.Frame),

            // Visio returns a VBA short, so a direct bool cast throws RuntimeBinderException.
            NewWindow = (short)hyperlink.NewWindow != 0
        };
    }

    private static string? Nullify(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
