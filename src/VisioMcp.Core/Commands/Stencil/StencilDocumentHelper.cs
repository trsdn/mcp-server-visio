using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;

namespace VisioMcp.Core.Commands.Stencil;

internal static class StencilDocumentHelper
{
    private const int VisOpenReadOnly = 0x2;
    private const int VisOpenDontList = 0x8;
    private const int VisOpenHidden = 0x40;
    private const int VisOpenNoWorkspace = 0x100;
    private const int StencilOpenFlags = VisOpenReadOnly | VisOpenDontList | VisOpenHidden | VisOpenNoWorkspace;

    internal static dynamic OpenStencilDocument(VisioContext ctx, string stencilPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stencilPath);

        dynamic? documents = null;
        try
        {
            documents = ((dynamic)ctx.Application).Documents;

            if (System.IO.File.Exists(stencilPath))
            {
                return documents.OpenEx(stencilPath, StencilOpenFlags);
            }

            try
            {
                return documents.OpenEx(stencilPath, StencilOpenFlags);
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException(
                    $"Stencil '{stencilPath}' was not found. Pass a full path, or an installed stencil "
                    + "name such as 'BASFLO_M.VSSX' (Basic Flowchart) or 'BASIC_U.VSSX' (Basic Shapes). "
                    + "Not every stencil ships with every Visio edition.",
                    stencilPath,
                    ex);
            }
        }
        finally
        {
            if (documents != null)
            {
                ComUtilities.Release(ref documents!);
            }
        }
    }

    internal static dynamic OpenBuiltInStencilDocument(VisioContext ctx, int stencilType, int measurementSystem)
    {
        string stencilPath = ((dynamic)ctx.Application).GetBuiltInStencilFile(stencilType, measurementSystem);
        return OpenStencilDocument(ctx, stencilPath);
    }

    internal static void CloseStencilDocument(ref dynamic? stencilDocument)
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
            // Best-effort cleanup: a stencil can already be closing during session teardown.
        }

        try
        {
            stencilDocument.Close();
        }
        catch
        {
            // Best-effort cleanup: releasing the COM reference below is still required.
        }

        ComUtilities.Release(ref stencilDocument!);
    }

    internal static dynamic? FindMasterByName(dynamic masters, string masterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterName);

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

    internal static string? TryGetMasterName(dynamic master)
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

    internal static string? TryGetMasterNameU(dynamic master)
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
}
