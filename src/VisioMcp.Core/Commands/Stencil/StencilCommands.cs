using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Stencil;

public class StencilCommands : IStencilCommands
{
    public StencilMasterListResult ListMasters(IVisioBatch batch, string stencilPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stencilPath);

        return batch.Execute((ctx, ct) =>
        {
            dynamic? stencilDocument = null;
            dynamic? masters = null;
            try
            {
                stencilDocument = StencilDocumentHelper.OpenStencilDocument(ctx, stencilPath);
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

                StencilDocumentHelper.CloseStencilDocument(ref stencilDocument);
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
                stencilDocument = StencilDocumentHelper.OpenStencilDocument(ctx, stencilPath);
                masters = stencilDocument.Masters;
                master = StencilDocumentHelper.FindMasterByName(masters, masterName)
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

                StencilDocumentHelper.CloseStencilDocument(ref stencilDocument);
                ComUtilities.Release(ref page!);
            }
        });
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }

    private static StencilMasterInfo ReadMasterInfo(dynamic master, int index)
    {
        return new StencilMasterInfo
        {
            Name = StencilDocumentHelper.TryGetMasterName(master) ?? $"Master {index}",
            NameU = StencilDocumentHelper.TryGetMasterNameU(master)
        };
    }

    private static float ToPageCoordinate(float points) => points / 72f;
}
