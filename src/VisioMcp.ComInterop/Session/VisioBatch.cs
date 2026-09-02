using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Implementation of <see cref="IVisioBatch"/> that manages a single Visio instance on a dedicated STA thread.
/// </summary>
internal sealed class VisioBatch : IVisioBatch
{
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private readonly string _documentPath;
    private readonly string[] _allPresentationPaths;
    private readonly bool _showPowerPoint;
    private readonly bool _createNewFile;
    private readonly TimeSpan _operationTimeout;
    private readonly ILogger<VisioBatch> _logger;
    private readonly Channel<Func<Task>> _workQueue;
    private readonly Thread _staThread;
    private readonly CancellationTokenSource _shutdownCts;
    private int _disposed;
    private int? _VisioProcessId;
    private bool _operationTimedOut;

    private dynamic? _powerPoint;
    private dynamic? _presentation;
    private Dictionary<string, object>? _documents;
    private VisioContext? _context;

    public VisioBatch(string[] documentPaths, ILogger<VisioBatch>? logger = null, bool show = false, TimeSpan? operationTimeout = null)
        : this(documentPaths, logger, show, createNewFile: false, operationTimeout: operationTimeout)
    {
    }

    internal static VisioBatch CreateNewPresentation(string filePath, bool isMacroEnabled, ILogger<VisioBatch>? logger = null, bool show = false, TimeSpan? operationTimeout = null)
    {
        _ = isMacroEnabled;
        return new VisioBatch([filePath], logger, show, createNewFile: true, operationTimeout: operationTimeout);
    }

    private VisioBatch(string[] documentPaths, ILogger<VisioBatch>? logger, bool show, bool createNewFile, TimeSpan? operationTimeout = null)
    {
        if (documentPaths == null || documentPaths.Length == 0)
            throw new ArgumentException("At least one document path is required", nameof(documentPaths));

        _allPresentationPaths = documentPaths;
        _documentPath = documentPaths[0];
        _showPowerPoint = show;
        _createNewFile = createNewFile;
        _operationTimeout = operationTimeout ?? ComInteropConstants.DefaultOperationTimeout;
        _logger = logger ?? NullLogger<VisioBatch>.Instance;
        _shutdownCts = new CancellationTokenSource();
        _workQueue = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _staThread = new Thread(() =>
        {
            try
            {
                OleMessageFilter.Register();

                Type? appType = Type.GetTypeFromProgID(_showPowerPoint ? "Visio.Application" : "Visio.InvisibleApp")
                    ?? Type.GetTypeFromProgID("Visio.Application");
                if (appType == null)
                {
                    throw new InvalidOperationException("Microsoft Visio is not installed on this system.");
                }

                dynamic tempPowerPoint = Activator.CreateInstance(appType)!;
                tempPowerPoint.Visible = _showPowerPoint;
                tempPowerPoint.AlertResponse = 7;

                CaptureProcessId(tempPowerPoint);

                var tempPresentations = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                dynamic? primaryPresentation = null;

                foreach (var path in _allPresentationPaths)
                {
                    string normalizedPath = Path.GetFullPath(path);
                    object pres;

                    if (_createNewFile)
                    {
                        string? directory = Path.GetDirectoryName(normalizedPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            throw new DirectoryNotFoundException($"Directory does not exist: '{directory}'. Create the directory first before creating Visio files.");
                        }

                        pres = tempPowerPoint.Documents.Add("");
                        ((dynamic)pres).SaveAs(normalizedPath);
                    }
                    else
                    {
                        FileAccessValidator.ValidateFileNotLocked(path);
                        try
                        {
                            pres = tempPowerPoint.Documents.Open(normalizedPath);
                        }
                        catch (COMException ex)
                        {
                            throw FileAccessValidator.CreateFileLockedError(path, ex);
                        }
                    }

                    tempPresentations[normalizedPath] = pres;
                    if (path == _documentPath)
                    {
                        primaryPresentation = pres;
                    }
                }

                _powerPoint = tempPowerPoint;
                _presentation = primaryPresentation;
                _documents = tempPresentations;
                _context = new VisioContext(_documentPath, _powerPoint, _presentation!);

                started.SetResult();

                while (true)
                {
                    try
                    {
                        if (!_workQueue.Reader.WaitToReadAsync(_shutdownCts.Token).AsTask().GetAwaiter().GetResult())
                        {
                            _logger.LogDebug("Channel completed, exiting message pump for {FileName}", Path.GetFileName(_documentPath));
                            break;
                        }

                        while (_workQueue.Reader.TryRead(out var work))
                        {
                            try
                            {
                                work().GetAwaiter().GetResult();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        while (_workQueue.Reader.TryRead(out var remainingWork))
                        {
                            try
                            {
                                remainingWork().GetAwaiter().GetResult();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        _logger.LogDebug("Shutdown requested, exiting message pump for {FileName}", Path.GetFileName(_documentPath));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                started.TrySetException(ex);
            }
            finally
            {
                _logger.LogDebug("STA thread cleanup starting for {FileName}", Path.GetFileName(_documentPath));
                VisioShutdownService.CloseAndQuit(_presentation, _powerPoint, false, _documentPath, _logger);

                _presentation = null;
                _powerPoint = null;
                _documents = null;
                _context = null;

                OleMessageFilter.Revoke();
                _logger.LogDebug("STA thread cleanup completed for {FileName}", Path.GetFileName(_documentPath));
            }
        })
        {
            IsBackground = true,
            Name = $"VisioBatch-{Path.GetFileName(_documentPath)}"
        };

        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        started.Task.GetAwaiter().GetResult();
    }

    public string DocumentPath => _documentPath;

    public ILogger Logger => _logger;

    public int? VisioProcessId => _VisioProcessId;

    public TimeSpan OperationTimeout => _operationTimeout;

    public bool IsVisioProcessAlive()
    {
        if (_disposed != 0) return false;
        if (!_VisioProcessId.HasValue) return false;

        try
        {
            using var proc = System.Diagnostics.Process.GetProcessById(_VisioProcessId.Value);
            return !proc.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public IReadOnlyDictionary<string, object> Documents
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, nameof(VisioBatch));
            return _documents ?? throw new InvalidOperationException("Documents not initialized");
        }
    }

    public object GetDocument(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(VisioBatch));

        if (_documents == null)
            throw new InvalidOperationException("Documents not initialized");

        string normalizedPath = Path.GetFullPath(filePath);
        if (_documents.TryGetValue(normalizedPath, out var document))
        {
            return document;
        }

        throw new KeyNotFoundException($"Document '{filePath}' is not open in this batch.");
    }

    public void Execute(Action<VisioContext, CancellationToken> operation, CancellationToken cancellationToken = default)
    {
        Execute((ctx, ct) =>
        {
            operation(ctx, ct);
            return 0;
        }, cancellationToken);
    }

    public T Execute<T>(Func<VisioContext, CancellationToken, T> operation, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(VisioBatch));

        if (_operationTimedOut)
        {
            throw new TimeoutException(
                $"A previous operation timed out or was cancelled for '{Path.GetFileName(_documentPath)}'. The Visio COM thread may be unresponsive. Please close this session and create a new one.");
        }

        if (!IsVisioProcessAlive())
        {
            _logger.LogError("Visio process is no longer running for document {FileName}", Path.GetFileName(_documentPath));
            throw new InvalidOperationException(
                $"Visio process is no longer running for document '{Path.GetFileName(_documentPath)}'. The application may have been closed manually or crashed. Please close this session and create a new one.");
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var writeTask = _workQueue.Writer.WriteAsync(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = ExecuteInUndoScope(operation, cancellationToken);
                    tcs.SetResult(result);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                return Task.CompletedTask;
            }, cancellationToken);

            if (writeTask.IsCompleted)
            {
                writeTask.GetAwaiter().GetResult();
            }
            else
            {
                writeTask.AsTask().GetAwaiter().GetResult();
            }
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(VisioBatch),
                $"Session for '{Path.GetFileName(_documentPath)}' was disposed while submitting an operation.");
        }

        try
        {
            if (cancellationToken.CanBeCanceled)
            {
                return tcs.Task.WaitAsync(cancellationToken).GetAwaiter().GetResult();
            }

            using var timeoutCts = new CancellationTokenSource(_operationTimeout);
            return tcs.Task.WaitAsync(timeoutCts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Operation timed out after {Timeout} for {FileName}", _operationTimeout, Path.GetFileName(_documentPath));
            _operationTimedOut = true;
            throw new TimeoutException(
                $"Visio operation timed out after {_operationTimeout.TotalSeconds} seconds for '{Path.GetFileName(_documentPath)}'. Visio may be unresponsive or the operation is taking longer than expected. Consider increasing timeoutSeconds when opening the session.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Operation cancelled or timed out for {FileName}", Path.GetFileName(_documentPath));
            _operationTimedOut = true;
            throw;
        }
    }

    /// <summary>
    /// Runs an operation inside a Visio undo scope so it is atomic and undoable in one step.
    /// </summary>
    /// <remarks>
    /// <para>Two properties, both verified against a live instance rather than assumed:</para>
    /// <para><b>Rollback.</b> <c>EndUndoScope(id, commit: false)</c> reverts the changes made inside
    /// the scope. Without it an operation that writes several cells and then fails leaves the
    /// document half-edited, with no way for the caller to know which writes landed.</para>
    /// <para><b>Grouping.</b> With <c>commit: true</c>, everything written inside the scope becomes
    /// a single entry in Visio's undo stack, so a command that writes five cells is one Ctrl+Z for
    /// a user watching in visible mode rather than five.</para>
    /// <para>Cost is roughly 1 ms per scope, negligible beside the COM calls it wraps. If Visio
    /// refuses to open a scope the operation still runs — losing atomicity is much better than
    /// refusing to work.</para>
    /// </remarks>
    private T ExecuteInUndoScope<T>(Func<VisioContext, CancellationToken, T> operation, CancellationToken cancellationToken)
    {
        var context = _context!;
        int scopeId = 0;
        bool scopeOpen = false;

        try
        {
            scopeId = Convert.ToInt32(context.Application.BeginUndoScope(UndoScopeName));
            scopeOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not open an undo scope; running the operation without one.");
        }

        try
        {
            var result = operation(context, cancellationToken);

            if (scopeOpen)
            {
                EndUndoScope(context, scopeId, commit: true);
            }

            return result;
        }
        catch
        {
            if (scopeOpen)
            {
                // Revert whatever the failed operation had already written. This must not throw:
                // masking the original exception would hide why the operation failed.
                EndUndoScope(context, scopeId, commit: false);
            }

            throw;
        }
    }

    private void EndUndoScope(VisioContext context, int scopeId, bool commit)
    {
        try
        {
            context.Application.EndUndoScope(scopeId, commit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to {Action} undo scope {ScopeId}.", commit ? "commit" : "cancel", scopeId);
        }
    }

    /// <summary>Name shown in Visio's Undo menu for an operation performed by this tool.</summary>
    private const string UndoScopeName = "VisioMcp operation";

    public void Save(CancellationToken cancellationToken = default)
    {
        Execute((ctx, ct) =>
        {
            VisioShutdownService.SaveDocumentWithTimeout(_presentation!, Path.GetFileName(_documentPath), _logger, ct);
            return 0;
        }, cancellationToken);
    }

    public void Dispose()
    {
        var callingThread = Environment.CurrentManagedThreadId;

        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            _logger.LogDebug("[Thread {CallingThread}] Dispose skipped - already disposed for {FileName}", callingThread, Path.GetFileName(_documentPath));
            return;
        }

        _logger.LogDebug("[Thread {CallingThread}] Dispose starting for {FileName}", callingThread, Path.GetFileName(_documentPath));
        _shutdownCts.Cancel();
        _workQueue.Writer.Complete();
        _logger.LogDebug("[Thread {CallingThread}] Waiting for STA thread (Id={STAThread}) to exit for {FileName}", callingThread, _staThread?.ManagedThreadId ?? -1, Path.GetFileName(_documentPath));

        if (_operationTimedOut && _VisioProcessId.HasValue && _staThread.IsAlive)
        {
            _logger.LogWarning(
                "[Thread {CallingThread}] Operation timed out - force-killing Visio process {ProcessId} before waiting for STA thread for {FileName}",
                callingThread, _VisioProcessId.Value, Path.GetFileName(_documentPath));
            TryKillProcess(_VisioProcessId.Value, callingThread, "pre-emptive, before STA join");
        }

        if (_staThread.IsAlive)
        {
            var joinTimeout = _operationTimedOut ? TimeSpan.FromSeconds(10) : ComInteropConstants.StaThreadJoinTimeout;
            if (!_staThread.Join(joinTimeout))
            {
                _logger.LogError(
                    "[Thread {CallingThread}] STA thread (Id={STAThread}) did NOT exit within {Timeout} for {FileName}. Attempting force-kill.",
                    callingThread, _staThread.ManagedThreadId, joinTimeout, Path.GetFileName(_documentPath));

                if (_VisioProcessId.HasValue)
                {
                    TryKillProcess(_VisioProcessId.Value, callingThread, Path.GetFileName(_documentPath));
                    _staThread.Join(TimeSpan.FromSeconds(5));
                }
                else
                {
                    _logger.LogError("[Thread {CallingThread}] No Visio process ID captured - cannot force-kill. Process will leak.", callingThread);
                }
            }
        }

        if (_VisioProcessId.HasValue)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(_VisioProcessId.Value);
                if (!process.HasExited && !process.WaitForExit(5000))
                {
                    _logger.LogWarning(
                        "[Thread {CallingThread}] Visio process {ProcessId} did not exit within 5s for {FileName}. Force-killing lingering process.",
                        callingThread, _VisioProcessId.Value, Path.GetFileName(_documentPath));
                    process.Kill();
                    process.WaitForExit(3000);
                }
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        _shutdownCts.Dispose();
    }

    private void CaptureProcessId(dynamic app)
    {
        try
        {
            int hwnd = 0;
            try
            {
                hwnd = Convert.ToInt32(app.WindowHandle32);
            }
            catch
            {
            }

            if (hwnd != 0)
            {
                _ = GetWindowThreadProcessId(new IntPtr(hwnd), out uint processId);
                if (processId != 0)
                {
                    _VisioProcessId = (int)processId;
                    _logger.LogDebug("Captured Visio process ID via HWND: {ProcessId}", _VisioProcessId);
                    return;
                }
            }

            _VisioProcessId = TryFindNewestVisioProcessId();
            if (_VisioProcessId.HasValue)
            {
                _logger.LogDebug("Captured Visio process ID via process scan: {ProcessId}", _VisioProcessId);
            }
            else
            {
                _logger.LogWarning("Could not determine Visio process ID. Force-kill will be disabled for this session.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture Visio process ID. Force-kill will not be available.");
        }
    }

    private static int? TryFindNewestVisioProcessId()
    {
        try
        {
            return System.Diagnostics.Process.GetProcessesByName("VISIO")
                .OrderByDescending(p => p.StartTime)
                .Select(p =>
                {
                    try
                    {
                        return (int?)p.Id;
                    }
                    finally
                    {
                        p.Dispose();
                    }
                })
                .FirstOrDefault(id => id.HasValue);
        }
        catch
        {
            return null;
        }
    }

    private void TryKillProcess(int processId, int callingThread, string reason)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                _logger.LogWarning(
                    "[Thread {CallingThread}] Force-killing Visio process {ProcessId} for {Reason}",
                    callingThread, processId, reason);
                process.Kill();
                process.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
            _logger.LogDebug("[Thread {CallingThread}] Visio process {ProcessId} already exited", callingThread, processId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Thread {CallingThread}] Failed to force-kill Visio process {ProcessId}",
                callingThread, processId);
        }
    }

    private static void TryReleaseComObject(object? comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }
}
