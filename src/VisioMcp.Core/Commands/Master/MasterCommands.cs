using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Master;

/// <summary>
/// Masters held inside the working document, backed by <c>Document.Masters</c> (#34).
/// </summary>
public class MasterCommands : IMasterCommands
{
    public MasterListResult List(IVisioBatch batch)
    {
        return batch.Execute((ctx, ct) =>
        {
            dynamic masters = ctx.Document.Masters;
            try
            {
                var found = new List<MasterInfo>();
                int count = (int)masters.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic? master = null;
                    try
                    {
                        master = masters[i];
                        found.Add(Describe(master));
                    }
                    finally
                    {
                        if (master != null) ComUtilities.Release(ref master!);
                    }
                }

                return new MasterListResult
                {
                    Success = true,
                    Masters = found,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                ComUtilities.Release(ref masters!);
            }
        });
    }

    public MasterDetailResult Read(IVisioBatch batch, string masterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? master = null;
            try
            {
                master = GetMaster(ctx, masterName);
                return new MasterDetailResult
                {
                    Success = true,
                    Master = Describe(master),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (master != null) ComUtilities.Release(ref master!);
            }
        });
    }

    public MasterDetailResult CreateFromShape(IVisioBatch batch, int pageIndex, string shapeName, string? masterName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? page = null;
            dynamic? shape = null;
            dynamic? created = null;
            try
            {
                page = ctx.Document.Pages[pageIndex];
                shape = page.Shapes.Item(shapeName);

                // Document.Drop copies the shape's definition into Masters. The coordinates place it
                // within the master's own drawing space, not on any page, so the shape on the page
                // is left exactly where it was.
                created = ctx.Document.Drop(shape, 0.0, 0.0);

                if (!string.IsNullOrWhiteSpace(masterName))
                {
                    created.Name = masterName;
                }

                return new MasterDetailResult
                {
                    Success = true,
                    Master = Describe(created),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (created != null) ComUtilities.Release(ref created!);
                if (shape != null) ComUtilities.Release(ref shape!);
                if (page != null) ComUtilities.Release(ref page!);
            }
        });
    }

    public MasterDetailResult Rename(IVisioBatch batch, string masterName, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? master = null;
            try
            {
                master = GetMaster(ctx, masterName);
                master.Name = newName;

                return new MasterDetailResult
                {
                    Success = true,
                    Master = Describe(master),
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (master != null) ComUtilities.Release(ref master!);
            }
        });
    }

    public OperationResult Delete(IVisioBatch batch, string masterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? master = null;
            try
            {
                master = GetMaster(ctx, masterName);
                string actual = ComUtilities.SafeGetString(master, "Name");
                master.Delete();

                return new OperationResult
                {
                    Success = true,
                    Action = "delete",
                    Message = $"Deleted master '{actual}'. Shapes already placed from it are unaffected.",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (master != null) ComUtilities.Release(ref master!);
            }
        });
    }

    public MasterInstanceListResult ListInstances(IVisioBatch batch, string masterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? master = null;
            dynamic? pages = null;
            try
            {
                // Resolve first, so an unknown name fails the way it does everywhere else rather
                // than quietly reporting zero instances.
                master = GetMaster(ctx, masterName);
                int targetId = (int)master.ID;
                string resolvedName = ComUtilities.SafeGetString(master, "Name");

                var instances = new List<MasterInstanceInfo>();
                pages = ctx.Document.Pages;
                int pageCount = (int)pages.Count;

                for (int p = 1; p <= pageCount; p++)
                {
                    dynamic? page = null;
                    try
                    {
                        page = pages[p];
                        string pageName = ComUtilities.SafeGetString(page, "Name");
                        int shapeCount = (int)page.Shapes.Count;

                        for (int s = 1; s <= shapeCount; s++)
                        {
                            dynamic? shape = null;
                            dynamic? shapeMaster = null;
                            try
                            {
                                shape = page.Shapes[s];
                                shapeMaster = shape.Master;
                                if (shapeMaster == null || (int)shapeMaster.ID != targetId)
                                {
                                    continue;
                                }

                                instances.Add(new MasterInstanceInfo
                                {
                                    PageIndex = p,
                                    PageName = pageName,
                                    ShapeId = (int)shape.ID,
                                    ShapeName = ComUtilities.SafeGetString(shape, "Name")
                                });
                            }
                            finally
                            {
                                if (shapeMaster != null) ComUtilities.Release(ref shapeMaster!);
                                if (shape != null) ComUtilities.Release(ref shape!);
                            }
                        }
                    }
                    finally
                    {
                        if (page != null) ComUtilities.Release(ref page!);
                    }
                }

                return new MasterInstanceListResult
                {
                    Success = true,
                    MasterName = resolvedName,
                    Instances = instances,
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (pages != null) ComUtilities.Release(ref pages!);
                if (master != null) ComUtilities.Release(ref master!);
            }
        });
    }

    /// <summary>
    /// Resolves a master by name, replacing Visio's bare "Object name not found".
    /// </summary>
    /// <remarks>
    /// A blank Visio document owns no masters at all, so "not found" is the expected answer far
    /// more often here than for a page or a shape. The message therefore says what the document
    /// does have, and how to get a master when it has none.
    /// </remarks>
    private static dynamic GetMaster(VisioContext ctx, string masterName)
    {
        dynamic masters = ctx.Document.Masters;
        try
        {
            try
            {
                return masters[masterName];
            }
            catch (Exception)
            {
                var available = new List<string>();
                int count = (int)masters.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic? master = null;
                    try
                    {
                        master = masters[i];
                        available.Add(ComUtilities.SafeGetString(master, "Name"));
                    }
                    finally
                    {
                        if (master != null) ComUtilities.Release(ref master!);
                    }
                }

                string detail = available.Count == 0
                    ? "This document has no masters. They appear when a stencil shape is dropped "
                      + "(stencil(drop-master)), or use master(create-from-shape) to promote a shape already on a page."
                    : $"This document has: {string.Join(", ", available)}.";

                throw new ArgumentException($"Master '{masterName}' not found. {detail}", nameof(masterName));
            }
        }
        finally
        {
            ComUtilities.Release(ref masters!);
        }
    }

    private static MasterInfo Describe(dynamic master)
    {
        dynamic? shapes = null;
        try
        {
            shapes = master.Shapes;
            string prompt = ComUtilities.SafeGetString(master, "Prompt");

            return new MasterInfo
            {
                Name = ComUtilities.SafeGetString(master, "Name"),
                UniversalName = ComUtilities.SafeGetString(master, "NameU"),
                Index = (int)master.Index,
                Id = (int)master.ID,
                UniqueId = ComUtilities.SafeGetString(master, "UniqueID"),
                ShapeCount = (int)shapes.Count,

                // Visio returns a VBA short here, so a direct bool cast throws RuntimeBinderException.
                Hidden = (short)master.Hidden != 0,
                Prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt
            };
        }
        finally
        {
            if (shapes != null) ComUtilities.Release(ref shapes!);
        }
    }
}
