using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VisioMcp.ComInterop.Session;

/// <summary>
/// Manages the active Office session for the MCP server and CLI.
/// Maps user-facing sessionId to internal IVisioBatch instance.
/// </summary>
/// <remarks>
/// <para><b>CRITICAL: Visio COM is single-instance for this host.</b> This manager intentionally
/// allows only one active automation session at a time to keep lifecycle handling predictable.
/// Therefore, only ONE session can be active at a time.</para>
/// <para><b>Concurrency Model:</b></para>
/// <list type="bullet">
/// <item><b>Only ONE session at a time:</b> the automation host is treated as single-session</item>
/// <item><b>Within-session operations are SERIAL:</b> Operations queue on one STA thread</item>
/// <item><b>Close before opening another:</b> Must close current session before opening a new file</item>
/// </list>
/// </remarks>
public sealed class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IVisioBatch> _activeSessions = new();
    private readonly ConcurrentDictionary<string, string> _activeFilePaths = new();
    private readonly ConcurrentDictionary<string, SessionTarget> _sessionTargets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _activeOperationCounts = new();
    private readonly ConcurrentDictionary<string, bool> _showPowerPointFlags = new();
    private readonly ConcurrentDictionary<string, SessionOrigin> _sessionOrigins = new();
    private readonly ConcurrentDictionary<string, DateTime> _sessionCreatedAt = new();
    private readonly Polly.ResiliencePipeline _sessionCreationPipeline = ResiliencePipelines.CreateSessionCreationPipeline();
    private readonly ILogger<SessionManager> _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new SessionManager with optional logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics</param>
    public SessionManager(ILogger<SessionManager>? logger = null)
    {
        _logger = logger ?? NullLogger<SessionManager>.Instance;
    }

    /// <summary>
    /// Creates a new session for the specified Visio file.
    /// </summary>
    /// <param name="filePath">Path to the Visio file to open</param>
    /// <param name="show">Whether to show the Visio window (default: false for background automation)</param>
    /// <param name="operationTimeout">Maximum time for any operation in this session (default: 5 minutes)</param>
    /// <param name="origin">Which client is creating this session (CLI or MCP)</param>
    /// <returns>Unique session ID for this session</returns>
    /// <exception cref="FileNotFoundException">File does not exist</exception>
    /// <exception cref="InvalidOperationException">Failed to create session, session already active, or file already open</exception>
    /// <remarks>
    /// <para><b>Single-session only:</b> Only one session can be active at a time.
    /// Close the current session before opening another file.</para>
    /// </remarks>
    public string CreateSession(string filePath, bool show = false, TimeSpan? operationTimeout = null, SessionOrigin origin = SessionOrigin.Unknown, string? pageName = null, int? pageIndex = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // CRITICAL: single-session lifecycle model — only one session at a time
        if (!_activeSessions.IsEmpty)
        {
            throw new InvalidOperationException("Only one session can be active at a time. Close the current session before opening another file.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Visio file not found: {filePath}. To create a new file, use the 'create' action instead of 'open'.", filePath);
        }

        var target = CreateSessionTarget(filePath, pageName, pageIndex);
        var normalizedPath = target.DocumentPath;

        // Check if file is already open in another session
        if (_activeFilePaths.ContainsKey(normalizedPath))
        {
            throw new InvalidOperationException($"File '{filePath}' is already open in another session.");
        }

        // Generate unique session ID
        string sessionId = Guid.NewGuid().ToString("N");

        IVisioBatch? batch = null;
        try
        {
            // Create batch session using Core API with retry for transient COM failures
            // (e.g., CO_E_SERVER_EXEC_FAILURE when system resources are constrained)
            batch = _sessionCreationPipeline.Execute(() => VisioSession.BeginBatch(show, operationTimeout, filePath));

            // Store in active sessions
            if (!_activeSessions.TryAdd(sessionId, batch))
            {
                throw new InvalidOperationException($"Session ID collision: {sessionId}");
            }

            // Track the file path
            if (!_activeFilePaths.TryAdd(normalizedPath, sessionId))
            {
                // Cleanup if file path tracking fails
                _activeSessions.TryRemove(sessionId, out _);
                throw new InvalidOperationException($"Failed to track file path for session: {sessionId}");
            }

            if (!_sessionTargets.TryAdd(sessionId, target))
            {
                _activeSessions.TryRemove(sessionId, out _);
                _activeFilePaths.TryRemove(normalizedPath, out _);
                throw new InvalidOperationException($"Failed to record session metadata for: {sessionId}");
            }

            // Initialize operation counter and show flag
            _activeOperationCounts[sessionId] = 0;
            _showPowerPointFlags[sessionId] = show;
            _sessionOrigins[sessionId] = origin;
            _sessionCreatedAt[sessionId] = DateTime.UtcNow;

            // Success - transfer ownership to dictionary
            var result = sessionId;
            batch = null;  // Prevent disposal in finally
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create session for '{filePath}': {ex.Message}", ex);
        }
        finally
        {
            // Dispose batch only if we didn't successfully add it to dictionary
            batch?.Dispose();
        }
    }

    /// <summary>
    /// Creates a new Visio file and opens a session for it in one operation.
    /// This is the preferred method for creating new documents with sessions.
    /// </summary>
    /// <param name="filePath">Path for the new Visio file (.vsdx, .vsdm, or .vsd)</param>
    /// <param name="show">Whether to show the Visio window (default: false)</param>
    /// <param name="operationTimeout">Maximum time for any operation in this session (default: 5 minutes)</param>
    /// <param name="origin">Which client is creating this session (CLI or MCP)</param>
    /// <returns>Unique session ID for this session</returns>
    /// <exception cref="InvalidOperationException">File already exists, or failed to create session</exception>
    /// <exception cref="DirectoryNotFoundException">Target directory does not exist</exception>
    /// <remarks>
    /// <para><b>Single application start:</b> This method starts Visio only once, creating the file and session together.</para>
    /// <para><b>File Format:</b> Determined by extension - .vsdm creates a macro-enabled document.</para>
    /// <para><b>Directory:</b> Target directory must exist - will not be created automatically.</para>
    /// </remarks>
    public string CreateSessionForNewFile(string filePath, bool show = false, TimeSpan? operationTimeout = null, SessionOrigin origin = SessionOrigin.Unknown, string? pageName = null, int? pageIndex = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // CRITICAL: single-session lifecycle model — only one session at a time
        if (!_activeSessions.IsEmpty)
        {
            throw new InvalidOperationException("Only one session can be active at a time. Close the current session before creating a new file.");
        }

        var target = CreateSessionTarget(filePath, pageName, pageIndex);
        var normalizedPath = target.DocumentPath;

        // Validate extension
        string extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        if (extension is not (".vsdx" or ".vsdm" or ".vsd"))
        {
            throw new ArgumentException($"Invalid file extension '{extension}'. Only .vsdx, .vsdm, and .vsd are supported.");
        }

        // Check if file already exists
        if (File.Exists(normalizedPath))
        {
            throw new InvalidOperationException($"File already exists: {normalizedPath}. Use CreateSession to open existing files.");
        }

        // Check if file is already open in another session
        if (_activeFilePaths.ContainsKey(normalizedPath))
        {
            throw new InvalidOperationException($"File '{filePath}' is already open in another session.");
        }

        // Generate unique session ID
        string sessionId = Guid.NewGuid().ToString("N");
        bool isMacroEnabled = extension == ".vsdm";

        VisioBatch? batch = null;
        try
        {
            // Create new Presentation and keep session open with retry for transient COM failures
            batch = _sessionCreationPipeline.Execute(() => VisioBatch.CreateNewPresentation(normalizedPath, isMacroEnabled, logger: null, show: show, operationTimeout: operationTimeout));

            // Store in active sessions
            if (!_activeSessions.TryAdd(sessionId, batch))
            {
                throw new InvalidOperationException($"Session ID collision: {sessionId}");
            }

            // Track the file path
            if (!_activeFilePaths.TryAdd(normalizedPath, sessionId))
            {
                _activeSessions.TryRemove(sessionId, out _);
                throw new InvalidOperationException($"Failed to track file path for session: {sessionId}");
            }

            if (!_sessionTargets.TryAdd(sessionId, target))
            {
                _activeSessions.TryRemove(sessionId, out _);
                _activeFilePaths.TryRemove(normalizedPath, out _);
                throw new InvalidOperationException($"Failed to record session metadata for: {sessionId}");
            }

            // Initialize operation counter and show flag
            _activeOperationCounts[sessionId] = 0;
            _showPowerPointFlags[sessionId] = show;
            _sessionOrigins[sessionId] = origin;
            _sessionCreatedAt[sessionId] = DateTime.UtcNow;

            // Success - transfer ownership to dictionary
            var result = sessionId;
            batch = null;  // Prevent disposal in finally
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create session for new file '{filePath}': {ex.Message}", ex);
        }
        finally
        {
            // Dispose batch only if we didn't successfully add it to dictionary
            batch?.Dispose();
        }
    }



    /// <summary>
    /// Gets an active session by ID.
    /// If the session exists but PowerPoint has died, it is automatically cleaned up and null is returned.
    /// </summary>
    /// <param name="sessionId">Session ID returned from CreateSession</param>
    /// <returns>IVisioBatch instance, or null if session not found or PowerPoint process is dead</returns>
    public IVisioBatch? GetSession(string sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        if (!_activeSessions.TryGetValue(sessionId, out var batch))
        {
            return null;
        }

        // Check if PowerPoint process is still alive
        if (!batch.IsVisioProcessAlive())
        {
            _logger?.LogWarning("Session {SessionId} has dead PowerPoint process, auto-cleaning up", sessionId);
            CleanupDeadSession(sessionId, batch);
            return null;
        }

        return batch;
    }

    /// <summary>
    /// Cleans up a session whose PowerPoint process has died.
    /// This removes all tracking data and disposes the batch (best effort).
    /// </summary>
    private void CleanupDeadSession(string sessionId, IVisioBatch batch)
    {
        // Remove from active sessions
        _activeSessions.TryRemove(sessionId, out _);

        // Remove file path metadata so it can be opened again
        if (_sessionTargets.TryRemove(sessionId, out var target))
        {
            _activeFilePaths.TryRemove(target.DocumentPath, out _);
        }
        else
        {
            var filePathEntry = _activeFilePaths.FirstOrDefault(kvp => kvp.Value == sessionId);
            if (!filePathEntry.Equals(default(KeyValuePair<string, string>)))
            {
                _activeFilePaths.TryRemove(filePathEntry.Key, out _);
            }
        }

        // Clean up operation tracking data
        _activeOperationCounts.TryRemove(sessionId, out _);
        _showPowerPointFlags.TryRemove(sessionId, out _);

        // Clean up session origin tracking data
        _sessionOrigins.TryRemove(sessionId, out _);
        _sessionCreatedAt.TryRemove(sessionId, out _);

        // Dispose the batch (best effort - process is already dead)
        try
        {
            batch.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error disposing dead session {SessionId} (expected - process is dead)", sessionId);
        }
    }

    /// <summary>
    /// Increments the active operation count for a session.
    /// Call this when starting an operation on the session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    public void BeginOperation(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        _activeOperationCounts.AddOrUpdate(sessionId, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Decrements the active operation count for a session.
    /// Call this when an operation completes (success or failure).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    public void EndOperation(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        _activeOperationCounts.AddOrUpdate(sessionId, 0, (_, count) => Math.Max(0, count - 1));
    }

    /// <summary>
    /// Gets the number of active operations for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Number of active operations, or 0 if session not found</returns>
    public int GetActiveOperationCount(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return 0;
        return _activeOperationCounts.TryGetValue(sessionId, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets whether PowerPoint is visible for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>True if PowerPoint is visible for this session</returns>
    public bool IsPowerPointVisible(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        return _showPowerPointFlags.TryGetValue(sessionId, out var visible) && visible;
    }

    /// <summary>
    /// Updates the visibility flag for a session.
    /// Called by window management commands when PowerPoint visibility changes mid-session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="visible">New visibility state</param>
    /// <returns>True if session was found and flag updated, false if session not found</returns>
    public bool SetPowerPointVisible(string sessionId, bool visible)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        if (!_activeSessions.ContainsKey(sessionId)) return false;
        _showPowerPointFlags[sessionId] = visible;
        return true;
    }

    /// <summary>
    /// Validates whether a session can be closed safely.
    /// Returns information about blocking conditions.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Validation result with details about any blocking conditions</returns>
    public CloseValidationResult ValidateClose(string sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new CloseValidationResult(false, false, 0, "Session ID is required");
        }

        if (!_activeSessions.ContainsKey(sessionId))
        {
            return new CloseValidationResult(false, false, 0, $"Session '{sessionId}' not found");
        }

        var activeOps = GetActiveOperationCount(sessionId);
        var isVisible = IsPowerPointVisible(sessionId);

        if (activeOps > 0)
        {
            return new CloseValidationResult(true, isVisible, activeOps,
                $"Cannot close: {activeOps} operation(s) still running. Wait for operations to complete before closing.");
        }

        return new CloseValidationResult(true, isVisible, 0, null);
    }

    /// <summary>
    /// Closes the specified session with optional save.
    /// If save is true, saves changes before closing to ensure atomic operation.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="save">Whether to save changes before closing (default: false)</param>
    /// <param name="force">Force close even if operations are running (default: false)</param>
    /// <returns>True if session was found and closed, false if session not found</returns>
    /// <exception cref="InvalidOperationException">Save operation failed or operations still running</exception>
    public bool CloseSession(string sessionId, bool save = false, bool force = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        // Check for running operations (unless force is true)
        if (!force)
        {
            var activeOps = GetActiveOperationCount(sessionId);
            if (activeOps > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot close session '{sessionId}': {activeOps} operation(s) still running. " +
                    "Wait for all operations to complete before closing, or use force=true to close anyway.");
            }
        }

        // Save first if requested (blocks until complete)
        if (save)
        {
            var batch = GetSession(sessionId);
            if (batch != null)
            {
                try
                {
                    batch.Save();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to save session '{sessionId}' before closing: {ex.Message}", ex);
                }
            }
        }

        // Then close
        return CloseSessionSync(sessionId);
    }

    private bool CloseSessionSync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        if (!_activeSessions.TryRemove(sessionId, out var batch))
        {
            return false;
        }

        // Remove file path metadata so it can be opened again
        if (_sessionTargets.TryRemove(sessionId, out var target))
        {
            _activeFilePaths.TryRemove(target.DocumentPath, out _);
        }
        else
        {
            var filePathEntry = _activeFilePaths.FirstOrDefault(kvp => kvp.Value == sessionId);
            if (!filePathEntry.Equals(default(KeyValuePair<string, string>)))
            {
                _activeFilePaths.TryRemove(filePathEntry.Key, out _);
            }
        }

        // Clean up operation tracking data
        _activeOperationCounts.TryRemove(sessionId, out _);
        _showPowerPointFlags.TryRemove(sessionId, out _);

        // Clean up session origin tracking data
        _sessionOrigins.TryRemove(sessionId, out _);
        _sessionCreatedAt.TryRemove(sessionId, out _);

        try
        {
            batch.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to close session '{sessionId}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the number of active sessions.
    /// Note: This count may include dead sessions. Use <see cref="GetActiveSessions"/> for accurate count.
    /// </summary>
    public int ActiveSessionCount => _activeSessions.Count;

    /// <summary>
    /// Checks if the PowerPoint process for a session is still alive.
    /// If the session exists but PowerPoint has died, it is automatically cleaned up.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>True if session exists and PowerPoint process is alive, false otherwise</returns>
    public bool IsSessionAlive(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        if (!_activeSessions.TryGetValue(sessionId, out var batch)) return false;

        if (batch.IsVisioProcessAlive())
        {
            return true;
        }

        // Auto-cleanup dead session
        _logger?.LogWarning("Session {SessionId} has dead PowerPoint process, auto-cleaning up during IsSessionAlive check", sessionId);
        CleanupDeadSession(sessionId, batch);
        return false;
    }

    /// <summary>
    /// Gets all active session IDs.
    /// Note: This property does not filter dead sessions. Use <see cref="GetActiveSessions"/> for filtered results.
    /// </summary>
    public IEnumerable<string> ActiveSessionIds => _activeSessions.Keys.ToList();

    /// <summary>
    /// Returns a snapshot of active sessions with associated Presentation paths.
    /// Dead sessions (where PowerPoint process has died) are automatically cleaned up and excluded.
    /// </summary>
    public IReadOnlyList<SessionDescriptor> GetActiveSessions()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var snapshot = new List<SessionDescriptor>(_sessionTargets.Count);
        var deadSessions = new List<(string sessionId, IVisioBatch batch)>();

        foreach (var kvp in _sessionTargets)
        {
            var sessionId = kvp.Key;
            var target = kvp.Value;

            // Check if session is still alive
            if (_activeSessions.TryGetValue(sessionId, out var batch))
            {
                if (batch.IsVisioProcessAlive())
                {
                    // Get origin and createdAt metadata (defaults for legacy sessions)
                    _sessionOrigins.TryGetValue(sessionId, out var origin);
                    _sessionCreatedAt.TryGetValue(sessionId, out var createdAt);

                    snapshot.Add(new SessionDescriptor(
                        sessionId,
                        target.DocumentPath,
                        origin,
                        createdAt == default ? null : createdAt,
                        target.PageName,
                        target.PageIndex));
                }
                else
                {
                    // Mark for cleanup (don't cleanup during iteration)
                    deadSessions.Add((sessionId, batch));
                }
            }
            // If not in _activeSessions but in _sessionTargets, skip (orphaned metadata)
        }

        // Clean up dead sessions after iteration
        foreach (var (sessionId, batch) in deadSessions)
        {
            _logger?.LogWarning("Session {SessionId} has dead PowerPoint process, auto-cleaning up during GetActiveSessions", sessionId);
            CleanupDeadSession(sessionId, batch);
        }

        return snapshot;
    }

    /// <summary>
    /// Attempts to get the Presentation path associated with a session ID.
    /// </summary>
    public bool TryGetFilePath(string sessionId, [NotNullWhen(true)] out string? filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            filePath = null;
            return false;
        }

        if (_sessionTargets.TryGetValue(sessionId, out var target))
        {
            filePath = target.DocumentPath;
            return true;
        }

        filePath = null;
        return false;
    }

    /// <summary>
    /// Disposes all active sessions, discarding unsaved changes unless callers saved explicitly first.
    /// </summary>
    /// <remarks>
    /// <para><b>CRITICAL:</b> Shutdown/dispose paths must never trigger interactive save prompts.
    /// Callers that want to persist changes must call <see cref="CloseSession(string, bool, bool)"/>
    /// with <c>save: true</c> or invoke an explicit save action before disposal.</para>
    /// <para><b>CRITICAL:</b> Sessions are disposed SEQUENTIALLY to avoid COM threading issues.</para>
    /// <para>Visio COM objects must be disposed on their STA threads. Parallel disposal causes deadlocks.</para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Close all active sessions SEQUENTIALLY to avoid COM threading issues
        // PowerPoint COM objects must be disposed on their STA threads, parallel disposal causes deadlocks
        var sessions = _activeSessions.Values.ToList();
        _activeSessions.Clear();
        _activeFilePaths.Clear();
        _sessionTargets.Clear();

        foreach (var session in sessions)
        {
            try
            {
                // Dispose sequentially - VisioBatch.Dispose() handles its own Visio cleanup
                // via VisioShutdownService with proper timeouts and retry logic while
                // discarding unsaved changes to avoid modal save prompts on shutdown.
                _logger.LogInformation("Disposing session for {Path} without implicit save", session.DocumentPath);
                session.Dispose();
            }
            catch (Exception)
            {
                // Best-effort cleanup — continue with remaining sessions
            }
        }
    }

    private static SessionTarget CreateSessionTarget(string filePath, string? pageName, int? pageIndex)
    {
        if (pageIndex is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "pageIndex must be greater than 0 when specified.");
        }

        var normalizedPageName = string.IsNullOrWhiteSpace(pageName) ? null : pageName.Trim();
        return new SessionTarget(Path.GetFullPath(filePath), normalizedPageName, pageIndex);
    }
}

/// <summary>
/// Represents a snapshot of an active PowerPoint session managed by <see cref="SessionManager"/>.
/// </summary>
/// <param name="SessionId">Public session identifier shared with clients.</param>
/// <param name="FilePath">Normalized Presentation path associated with the session.</param>
/// <param name="Origin">Which client created this session (CLI or MCP).</param>
/// <param name="CreatedAt">When the session was created.</param>
public sealed record SessionDescriptor(
    string SessionId,
    string FilePath,
    SessionOrigin Origin = SessionOrigin.Unknown,
    DateTime? CreatedAt = null,
    string? PageName = null,
    int? PageIndex = null)
{
    /// <summary>
    /// Visio-friendly alias for <see cref="FilePath"/>.
    /// </summary>
    public string DocumentPath => FilePath;
}

/// <summary>
/// Stores the document-level target associated with a session.
/// This keeps session identity Visio-friendly without breaking the existing file-based callers.
/// </summary>
public sealed record SessionTarget(
    string DocumentPath,
    string? PageName = null,
    int? PageIndex = null)
{
    /// <summary>
    /// Backward-compatible alias for <see cref="DocumentPath"/>.
    /// </summary>
    public string FilePath => DocumentPath;
}

/// <summary>
/// Indicates which client created a session.
/// </summary>
public enum SessionOrigin
{
    /// <summary>Session origin is unknown (legacy sessions).</summary>
    Unknown = 0,

    /// <summary>Session was created via the CLI.</summary>
    CLI = 1,

    /// <summary>Session was created via the MCP Server.</summary>
    MCP = 2
}

/// <summary>
/// Result of validating whether a session can be closed.
/// </summary>
/// <param name="SessionExists">Whether the session was found.</param>
/// <param name="IsPowerPointVisible">Whether PowerPoint is visible (show=true).</param>
/// <param name="ActiveOperationCount">Number of operations currently running.</param>
/// <param name="BlockingReason">Reason why close is blocked, or null if close is allowed.</param>
public sealed record CloseValidationResult(
    bool SessionExists,
    bool IsPowerPointVisible,
    int ActiveOperationCount,
    string? BlockingReason)
{
    /// <summary>
    /// Whether the session can be closed (no blocking conditions).
    /// </summary>
    public bool CanClose => SessionExists && ActiveOperationCount == 0;
}



