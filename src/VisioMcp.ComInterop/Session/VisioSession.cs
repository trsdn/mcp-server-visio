using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Main entry point for Visio COM interop operations using the existing batch pattern.
/// </summary>
public static class VisioSession
{
    private static readonly SemaphoreSlim _createFileLock = new(1, 1);

    /// <summary>
    /// Opens one or more existing Visio documents in a shared batch.
    /// </summary>
    /// <param name="filePaths">The document paths to open.</param>
    /// <returns>A batch bound to the opened documents.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static IVisioBatch BeginBatch(params string[] filePaths)
        => BeginBatch(show: false, operationTimeout: null, filePaths);

    /// <summary>
    /// Opens one or more existing Visio documents in a shared batch with explicit visibility and timeout settings.
    /// </summary>
    /// <param name="show">Whether the Visio UI should be shown.</param>
    /// <param name="operationTimeout">Optional timeout override for queued operations.</param>
    /// <param name="filePaths">The document paths to open.</param>
    /// <returns>A batch bound to the opened documents.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static IVisioBatch BeginBatch(
        bool show,
        TimeSpan? operationTimeout,
        params string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            throw new ArgumentException("At least one file path is required", nameof(filePaths));

        string[] fullPaths = new string[filePaths.Length];
        for (int i = 0; i < filePaths.Length; i++)
        {
            string fullPath = Path.GetFullPath(filePaths[i]);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Visio file not found: {fullPath}. To create a new file, use the 'create' action instead of 'open'.", fullPath);
            }

            string extension = Path.GetExtension(fullPath).ToLowerInvariant();
            if (extension is not (".vsdx" or ".vsdm" or ".vsd"))
            {
                throw new ArgumentException($"Invalid file extension '{extension}'. Only Visio files (.vsdx, .vsdm, .vsd) are supported.");
            }

            fullPaths[i] = fullPath;
        }

        return new VisioBatch(fullPaths, logger: null, show: show, operationTimeout: operationTimeout);
    }

    /// <summary>
    /// Creates a new Visio document, reopens it through the standard batch pipeline, and executes the supplied operation.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="filePath">The path of the document to create.</param>
    /// <param name="isMacroEnabled">Whether the caller intends to create a macro-enabled file.</param>
    /// <param name="operation">The operation to execute after creation.</param>
    /// <param name="cancellationToken">Cancellation token for creation and execution.</param>
    /// <returns>The operation result.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static T CreateNew<T>(
        string filePath,
        bool isMacroEnabled,
        Func<VisioContext, CancellationToken, T> operation,
        CancellationToken cancellationToken = default)
    {
        if (!_createFileLock.Wait(TimeSpan.FromMinutes(2), cancellationToken))
        {
            throw new TimeoutException("Timed out waiting for file creation lock. Another CreateNew operation may be stuck.");
        }

        try
        {
            string fullPath = Path.GetFullPath(filePath);

            if (fullPath.Length > 218)
            {
                throw new PathTooLongException(
                    $"File path exceeds Visio's practical maximum length (~218 characters): {fullPath.Length} characters");
            }

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            CreateDocumentOnStaThread(fullPath, isMacroEnabled, cancellationToken);

            using var batch = BeginBatch(fullPath);
            return batch.Execute(operation, cancellationToken);
        }
        finally
        {
            _createFileLock.Release();
        }
    }

    private static void CreateDocumentOnStaThread(string fullPath, bool isMacroEnabled, CancellationToken cancellationToken)
    {
        _ = isMacroEnabled;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            dynamic? visioApp = null;
            dynamic? document = null;

            try
            {
                OleMessageFilter.Register();

                var visioType = Type.GetTypeFromProgID("Visio.InvisibleApp")
                    ?? Type.GetTypeFromProgID("Visio.Application");
                if (visioType == null)
                {
                    throw new InvalidOperationException("Visio is not installed or not properly registered.");
                }

#pragma warning disable IL2072
                visioApp = Activator.CreateInstance(visioType)!;
#pragma warning restore IL2072

                visioApp.Visible = false;
                visioApp.AlertResponse = 7;

                document = visioApp.Documents.Add("");
                document.SaveAs(fullPath);
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                try
                {
                    if (document != null)
                    {
                        try { document.Saved = true; } catch { }
                        document.Close();
                    }
                }
                catch { }

                if (visioApp != null)
                {
                    try { visioApp.Quit(); } catch { }
                    try { if (Marshal.IsComObject(visioApp)) Marshal.ReleaseComObject(visioApp); } catch { }
                }
                if (document != null)
                {
                    try { if (Marshal.IsComObject(document)) Marshal.ReleaseComObject(document); } catch { }
                }

                OleMessageFilter.Revoke();
            }
        })
        {
            IsBackground = true,
            Name = $"VisioCreate-{Path.GetFileName(fullPath)}"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!completion.Task.Wait(TimeSpan.FromSeconds(30), cancellationToken))
        {
            throw new TimeoutException($"File creation timed out for '{Path.GetFileName(fullPath)}'. Visio may be unresponsive.");
        }

        thread.Join(TimeSpan.FromSeconds(10));
    }
}
