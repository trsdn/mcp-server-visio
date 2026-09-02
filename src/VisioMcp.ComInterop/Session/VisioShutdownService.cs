using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Centralized service for document close and application quit operations.
/// </summary>
public static class VisioShutdownService
{
    private const int AlertResponseNo = 7;
    private static readonly ResiliencePipeline _quitPipeline = ResiliencePipelines.CreateVisioQuitPipeline();

    /// <summary>
    /// Saves the active document on the calling STA thread.
    /// </summary>
    public static void SaveDocumentWithTimeout(
        dynamic document,
        string? fileName = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;
        fileName ??= "unknown";

        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Saving document {FileName}", fileName);

        try
        {
            document.Save();
            logger.LogDebug("Document {FileName} saved successfully", fileName);
        }
        catch (COMException ex)
        {
            string errorMessage = ex.HResult switch
            {
                unchecked((int)0x800A03EC) =>
                    $"Cannot save '{fileName}'. The file may be read-only, locked by another process, or the path may not exist.",
                unchecked((int)0x800AC472) =>
                    $"Cannot save '{fileName}'. The file is locked for editing by another user or process.",
                _ => $"Failed to save document '{fileName}': {ex.Message}"
            };

            logger.LogError(ex, "Save failed for {FileName} (HResult: 0x{HResult:X8})", fileName, ex.HResult);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Closes a document and quits the application with resilient retry logic.
    /// </summary>
    public static void CloseAndQuit(
        dynamic? document,
        dynamic? visioApp,
        bool save,
        string? filePath = null,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        string fileName = string.IsNullOrEmpty(filePath) ? "unknown" : Path.GetFileName(filePath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (visioApp != null)
            {
                try { visioApp.AlertResponse = AlertResponseNo; } catch { }

                if (!save)
                {
                    try
                    {
                        visioApp.Visible = false;
                    }
                    catch
                    {
                    }
                }
            }

            if (save && document != null)
            {
                SaveDocumentWithTimeout(document, fileName, logger);
            }

            bool closedDocumentsFromApplication = false;

            if (visioApp != null)
            {
                try
                {
                    closedDocumentsFromApplication = CloseOpenDocuments(visioApp, !save, logger);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to close open documents for {FileName} via application collection", fileName);
                }
            }

            if (!closedDocumentsFromApplication && document != null)
            {
                try
                {
                    logger.LogDebug("Closing primary document {FileName} directly (save={Save})", fileName, save);
                    CloseDocument(document, !save, logger, fileName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to close primary document {FileName} directly", fileName);
                }
            }

            if (document != null)
            {
                TryReleaseComObject(document);
                document = null;
            }

            if (visioApp != null)
            {
                int attemptNumber = 0;
                Exception? lastException = null;
                using var quitTimeout = new CancellationTokenSource(ComInteropConstants.VisioQuitTimeout);

                try
                {
                    logger.LogDebug("Attempting to quit application for {FileName} with resilient retry ({Timeout} timeout)", fileName, ComInteropConstants.VisioQuitTimeout);

                    _quitPipeline.Execute(cancellationToken =>
                    {
                        attemptNumber++;
                        try
                        {
                            logger.LogDebug("Quit attempt {Attempt} for {FileName}", attemptNumber, fileName);
                            visioApp.Quit();
                            logger.LogDebug("Quit attempt {Attempt} succeeded for {FileName}", attemptNumber, fileName);
                        }
                        catch (COMException ex)
                        {
                            lastException = ex;
                            logger.LogWarning(ex,
                                "Quit attempt {Attempt} failed for {FileName} (HResult: 0x{HResult:X8})",
                                attemptNumber, fileName, ex.HResult);
                            throw;
                        }
                    }, quitTimeout.Token);

                    logger.LogInformation("Application quit succeeded for {FileName} after {Attempts} attempt(s) in {Elapsed}ms",
                        fileName, attemptNumber, stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) when (quitTimeout.Token.IsCancellationRequested)
                {
                    logger.LogError(
                        "Application quit TIMED OUT after {Timeout} for {FileName} (Attempts: {Attempts}). Visio is likely hung. Proceeding with forced COM cleanup.",
                        ComInteropConstants.VisioQuitTimeout, fileName, attemptNumber);
                    lastException = new TimeoutException($"Application.Quit() timed out after {ComInteropConstants.VisioQuitTimeout} for {fileName}");
                }
                catch (COMException ex) when (ex.HResult == ResiliencePipelines.RPC_E_CALL_FAILED)
                {
                    logger.LogError(ex,
                        "Application RPC connection FAILED (0x800706BE) for {FileName}. Proceeding with forced COM cleanup.",
                        fileName);
                    lastException = ex;
                }
                catch (COMException ex)
                {
                    logger.LogError(ex,
                        "Application quit failed for {FileName} after {Attempts} attempt(s) (HResult: 0x{HResult:X8}, Elapsed: {Elapsed}ms) - proceeding with COM cleanup",
                        fileName, attemptNumber, ex.HResult, stopwatch.ElapsedMilliseconds);
                    lastException = ex;
                }
                catch (MissingMemberException ex)
                {
                    logger.LogWarning(ex,
                        "Application COM proxy was disconnected while calling Quit for {FileName} - proceeding with COM cleanup",
                        fileName);
                    lastException = ex;
                }
                finally
                {
                    TryReleaseComObject(visioApp);
                    visioApp = null;
                }

                if (lastException != null)
                {
                    logger.LogWarning(
                        "Application quit unsuccessful for {FileName} (Elapsed: {Elapsed}s, Type: {ExceptionType}). COM cleanup completed. Process may leak if Visio remains hung.",
                        fileName, stopwatch.Elapsed.TotalSeconds, lastException.GetType().Name);
                }
            }
        }
        finally
        {
            logger.LogDebug("Application shutdown sequence completed for {FileName} in {Elapsed}ms",
                fileName, stopwatch.ElapsedMilliseconds);
        }
    }

    private static void TryReleaseComObject(object? comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    private static bool CloseOpenDocuments(dynamic visioApp, bool discardChanges, ILogger logger)
    {
        dynamic? documents = null;
        bool closedAnyDocuments = false;

        try
        {
            documents = visioApp.Documents;
            int documentCount = Convert.ToInt32(documents.Count);

            if (documentCount == 0)
            {
                return false;
            }

            try { visioApp.AlertResponse = AlertResponseNo; } catch { }

            for (int index = documentCount; index >= 1; index--)
            {
                dynamic? document = null;
                try
                {
                    document = documents.Item(index);
                    string documentName = TryGetDocumentName(document, index);
                    CloseDocument(document, discardChanges, logger, documentName);
                    closedAnyDocuments = true;
                }
                finally
                {
                    TryReleaseComObject(document);
                }
            }

            return closedAnyDocuments;
        }
        finally
        {
            TryReleaseComObject(documents);
        }
    }

    private static void CloseDocument(dynamic document, bool discardChanges, ILogger logger, string fileName)
    {
        logger.LogDebug("Closing document {FileName} (discardChanges={DiscardChanges})", fileName, discardChanges);

        if (discardChanges)
        {
            try { document.Saved = true; } catch { }
        }

        if (!TryInvokeCloseWithSaveFlag(document, !discardChanges, logger, fileName))
        {
            document.Close();
        }

        logger.LogDebug("Document {FileName} closed successfully", fileName);
    }

    private static bool TryInvokeCloseWithSaveFlag(dynamic document, bool saveChanges, ILogger logger, string fileName)
    {
        try
        {
            document.Close(saveChanges);
            logger.LogDebug("Closed document {FileName} using Close(saveChanges={SaveChanges})", fileName, saveChanges);
            return true;
        }
        catch (RuntimeBinderException)
        {
            return false;
        }
        catch (MissingMemberException)
        {
            return false;
        }
        catch (COMException ex) when (
            ex.HResult == unchecked((int)0x8002000E) || // DISP_E_BADPARAMCOUNT
            ex.HResult == unchecked((int)0x80020005))   // DISP_E_TYPEMISMATCH
        {
            logger.LogDebug("Close(saveChanges=...) not supported for {FileName}; falling back to Close()", fileName);
            return false;
        }
    }

    private static string TryGetDocumentName(dynamic document, int index)
    {
        try
        {
            return document.FullName?.ToString()
                ?? document.Name?.ToString()
                ?? $"document-{index}";
        }
        catch
        {
            return $"document-{index}";
        }
    }
}
