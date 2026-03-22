using VisioMcp.ComInterop;
using VisioMcp.ComInterop.Session;
using VisioMcp.Core.Models;

namespace VisioMcp.Core.Commands.Export;

public class ExportCommands : IExportCommands
{
    public ExportResult ToPdf(IVisioBatch batch, string destinationPath, int? fromPage, int? toPage)
    {
        return ExportFixedFormat(batch, destinationPath, fromPage, toPage, fixedFormat: 1, formatName: "PDF");
    }

    public ExportResult ToXps(IVisioBatch batch, string destinationPath, int? fromPage, int? toPage)
    {
        return ExportFixedFormat(batch, destinationPath, fromPage, toPage, fixedFormat: 2, formatName: "XPS");
    }

    public ExportResult PageExport(IVisioBatch batch, int pageIndex, string destinationPath)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageIndex);

        var fullPath = PrepareOutputPath(destinationPath);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(fullPath)))
        {
            throw new ArgumentException("destinationPath must include a file extension such as .png or .svg.", nameof(destinationPath));
        }

        return batch.Execute((ctx, ct) =>
        {
            dynamic page = GetPage(ctx, pageIndex);
            try
            {
                page.Export(fullPath);

                return new ExportResult
                {
                    Success = true,
                    FilePath = ctx.DocumentPath,
                    OutputPath = fullPath,
                    Format = Path.GetExtension(fullPath).TrimStart('.').ToUpperInvariant()
                };
            }
            finally
            {
                ComUtilities.Release(ref page!);
            }
        });
    }

    public OperationResult Print(IVisioBatch batch, int copies, int? fromPage, int? toPage)
    {
        ValidatePageRange(fromPage, toPage);

        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;
            int printRange = fromPage.HasValue || toPage.HasValue ? 1 : 0; // visPrintFromTo / visPrintAll
            int from = fromPage ?? 1;
            int to = toPage ?? -1;
            int copyCount = copies > 0 ? copies : 1;

            document.PrintOut(
                printRange,
                printRange == 1 ? from : Type.Missing,
                printRange == 1 ? to : Type.Missing,
                false,
                Type.Missing,
                false,
                Type.Missing,
                copyCount,
                false,
                false);

            return new OperationResult
            {
                Success = true,
                Action = "print",
                Message = printRange == 1
                    ? $"Printed {copyCount} copy(ies) for pages {from}-{to}"
                    : $"Printed {copyCount} copy(ies) for all foreground pages",
                FilePath = ctx.DocumentPath
            };
        });
    }

    public ExportResult SaveCopy(IVisioBatch batch, string destinationPath)
    {
        var fullPath = PrepareOutputPath(destinationPath);

        return batch.Execute((ctx, ct) =>
        {
            if (string.Equals(fullPath, ctx.DocumentPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("destinationPath must differ from the active document path.");
            }

            dynamic document = ctx.Document;
            document.Save();
            System.IO.File.Copy(ctx.DocumentPath, fullPath, overwrite: true);

            return new ExportResult
            {
                Success = true,
                FilePath = ctx.DocumentPath,
                OutputPath = fullPath,
                Format = Path.GetExtension(fullPath).TrimStart('.').ToUpperInvariant()
            };
        });
    }

    private static ExportResult ExportFixedFormat(
        IVisioBatch batch,
        string destinationPath,
        int? fromPage,
        int? toPage,
        int fixedFormat,
        string formatName)
    {
        ValidatePageRange(fromPage, toPage);
        var fullPath = PrepareOutputPath(destinationPath);

        return batch.Execute((ctx, ct) =>
        {
            dynamic document = ctx.Document;
            int printRange = fromPage.HasValue || toPage.HasValue ? 1 : 0; // visPrintFromTo / visPrintAll

            document.ExportAsFixedFormat(
                fixedFormat,
                fullPath,
                1,
                printRange,
                printRange == 1 ? fromPage ?? 1 : Type.Missing,
                printRange == 1 ? toPage ?? -1 : Type.Missing,
                false,
                true,
                true,
                true,
                false,
                Type.Missing);

            return new ExportResult
            {
                Success = true,
                FilePath = ctx.DocumentPath,
                OutputPath = fullPath,
                Format = formatName
            };
        });
    }

    private static string PrepareOutputPath(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string fullPath = Path.GetFullPath(destinationPath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    private static void ValidatePageRange(int? fromPage, int? toPage)
    {
        if (fromPage.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fromPage.Value);
        }

        if (toPage.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toPage.Value);
        }

        if (fromPage.HasValue != toPage.HasValue)
        {
            throw new ArgumentException("fromPage and toPage must either both be provided or both be omitted.");
        }

        if (fromPage.HasValue && toPage.HasValue && fromPage.Value > toPage.Value)
        {
            throw new ArgumentException("fromPage must be less than or equal to toPage.");
        }
    }

    private static dynamic GetPage(VisioContext ctx, int pageIndex)
    {
        return ((dynamic)ctx.Document).Pages.Item(pageIndex);
    }
}
