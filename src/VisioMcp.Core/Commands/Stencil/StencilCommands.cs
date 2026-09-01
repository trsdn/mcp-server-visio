using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Stencil;

public class StencilCommands : IStencilCommands
{
    private const int VisOpenReadOnly = 0x2;
    private const int VisOpenDontList = 0x8;
    private const int VisOpenHidden = 0x40;
    private const int VisOpenNoWorkspace = 0x100;
    private const int StencilOpenFlags = VisOpenReadOnly | VisOpenDontList | VisOpenHidden | VisOpenNoWorkspace;

    public StencilMasterListResult ListMasters(IVisioBatch batch, string stencilPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stencilPath);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? stencilDocument = null;
            dynamic? masters = null;
            try
            {
                stencilDocument = OpenStencilDocument(ctx, stencilPath);
                masters = stencilDocument.Masters;

                var result = new StencilMasterListResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    StencilPath = stencilPath
                };

                int count = Convert.ToInt32(masters.Count);
                for (int i = 1; i <= count; i++)
                {
                    dynamic? master = null;
                    try
                    {
                        master = masters.Item(i);
                        result.Masters.Add(ReadMasterInfo(master, i));
                    }
                    finally
                    {
                        if (master != null)
                        {
                            ComUtilities.Release(ref master!);
                        }
                    }
                }

                return result;
            }
            finally
            {
                if (masters != null)
                {
                    ComUtilities.Release(ref masters!);
                }

                CloseStencilDocument(ref stencilDocument);
            }
        });
    }

    public OperationResult DropMaster(IVisioBatch batch, int pageIndex, string stencilPath, string masterName, float xPosition, float yPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stencilPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            dynamic? stencilDocument = null;
            dynamic? masters = null;
            dynamic? master = null;
            dynamic? shape = null;
            try
            {
                stencilDocument = OpenStencilDocument(ctx, stencilPath);
                masters = stencilDocument.Masters;
                master = FindMasterByName(masters, masterName)
                    ?? throw new InvalidOperationException($"Master '{masterName}' was not found in stencil '{stencilPath}'.");

                shape = page.Drop(master, ToPageCoordinate(xPosition), ToPageCoordinate(yPosition));
                string shapeName = shape.Name?.ToString() ?? string.Empty;

                return new OperationResult
                {
                    Success = true,
                    Action = "drop-master",
                    Message = $"Dropped master '{masterName}' onto page {pageIndex} as shape '{shapeName}'",
                    FilePath = ctx.DocumentPath
                };
            }
            finally
            {
                if (shape != null)
                {
                    ComUtilities.Release(ref shape!);
                }

                if (master != null)
                {
                    ComUtilities.Release(ref master!);
                }

                if (masters != null)
                {
                    ComUtilities.Release(ref masters!);
                }

                CloseStencilDocument(ref stencilDocument);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic OpenStencilDocument(VisioContext ctx, string stencilPath)
    {
        if (!System.IO.File.Exists(stencilPath))
        {
            throw new FileNotFoundException($"Stencil file was not found: {stencilPath}", stencilPath);
        }

        return ctx.Application.Documents.OpenEx(stencilPath, StencilOpenFlags);
    }

    private static void CloseStencilDocument(ref dynamic? stencilDocument)
    {
        if (stencilDocument == null)
        {
            return;
        }

        try
        {
            stencilDocument.Saved = true;
        }
        catch
        {
        }

        try
        {
            stencilDocument.Close();
        }
        catch
        {
        }

        ComUtilities.Release(ref stencilDocument!);
    }

    private static dynamic? FindMasterByName(dynamic masters, string masterName)
    {
        int count = Convert.ToInt32(masters.Count);
        for (int i = 1; i <= count; i++)
        {
            dynamic? master = null;
            try
            {
                master = masters.Item(i);
                string? name = TryGetMasterName(master);
                string? nameU = TryGetMasterNameU(master);
                if (string.Equals(name, masterName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nameU, masterName, StringComparison.OrdinalIgnoreCase))
                {
                    return master;
                }
            }
            catch
            {
                if (master != null)
                {
                    ComUtilities.Release(ref master!);
                }

                throw;
            }

            if (master != null)
            {
                ComUtilities.Release(ref master!);
            }
        }

        return null;
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ctx.Document.Pages.Item(pageIndex);
    }

    private static StencilMasterInfo ReadMasterInfo(dynamic master, int index)
    {
        return new StencilMasterInfo
        {
            Name = TryGetMasterName(master) ?? $"Master {index}",
            NameU = TryGetMasterNameU(master)
        };
    }

    private static string? TryGetMasterName(dynamic master)
    {
        try
        {
            return master.Name?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetMasterNameU(dynamic master)
    {
        try
        {
            return master.NameU?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static float ToPageCoordinate(float points) => points / 72f;
}
